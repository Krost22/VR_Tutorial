#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KrostGames.TTS.Engine
{
    /// <summary>
    /// Keeps one hidden Windows PowerShell process alive and talks to it line by line through stdin/stdout,
    /// so a request only pays for the synthesis itself (tens of milliseconds) instead of a process start.
    /// Requests run one at a time on a background thread. A watchdog restarts the process if it stops answering.
    /// </summary>
    internal sealed class TTSWorker : IDisposable
    {
        abstract class Job
        {
            public string Command;
            public string Args;
            public int TimeoutMs;
            public abstract bool IsDone { get; }
            public abstract void Complete(string payload);
            public abstract void Fail(Exception error);
        }

        sealed class Job<T> : Job
        {
            public Func<string, T> Parse;
            public readonly TaskCompletionSource<T> Result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            public override bool IsDone => Result.Task.IsCompleted;

            // Runs on the worker thread, so heavy parsing (base64 + WAV decoding) stays off Unity's main thread.
            public override void Complete(string payload)
            {
                try { Result.TrySetResult(Parse(payload)); }
                catch (Exception e) { Fail(e); }
            }

            public override void Fail(Exception error) =>
                Result.TrySetException(error as TTSException ?? new TTSException(error.Message, error));
        }

        sealed class Disposable : IDisposable
        {
            readonly Action _onDispose;
            public Disposable(Action onDispose) { _onDispose = onDispose; }
            public void Dispose() => _onDispose();
        }

        const int StartTimeoutMs = 30000;

        readonly BlockingCollection<Job> _jobs = new BlockingCollection<Job>();
        readonly Thread _thread;
        readonly object _processLock = new object();
        readonly StringBuilder _stderr = new StringBuilder();
        Process _process;
        StreamWriter _stdin;
        StreamReader _stdout;
        int _nextId;
        int _watchedId;
        volatile bool _timedOut;
        volatile bool _disposed;

        public TTSWorker()
        {
            _thread = new Thread(Run) { IsBackground = true, Name = "LocalTTS worker" };
            _thread.Start();
        }

        public Task<T> Send<T>(string command, string args, int timeoutMs, Func<string, T> parse)
        {
            var job = new Job<T> { Command = command, Args = args, TimeoutMs = timeoutMs, Parse = parse };
            try { _jobs.Add(job); }
            catch (InvalidOperationException) { job.Fail(new TTSException("The speech engine was shut down.")); }
            return job.Result.Task;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _jobs.CompleteAdding();
            KillCurrentProcess();     // unblocks a pending read
            _thread.Join(2000);
            ReleaseProcess();
            while (_jobs.TryTake(out Job job)) job.Fail(new TTSException("The speech engine was shut down."));
        }

        void Run()
        {
            try
            {
                foreach (Job job in _jobs.GetConsumingEnumerable())
                {
                    if (job.IsDone) continue;
                    if (_disposed) { job.Fail(new TTSException("The speech engine was shut down.")); continue; }
                    try { job.Complete(Execute(job)); }
                    catch (Exception e) { job.Fail(e); }
                }
            }
            catch (Exception)
            {
                // The queue was torn down during shutdown.
            }
            finally
            {
                ReleaseProcess();
            }
        }

        string Execute(Job job)
        {
            EnsureStarted();
            int id = ++_nextId;
            string idText = id.ToString(CultureInfo.InvariantCulture);
            string ok = "OK\t" + idText + "\t";
            string err = "ERR\t" + idText + "\t";

            using (Watch(id, job.TimeoutMs))
            {
                try
                {
                    _stdin.Write(job.Command);
                    _stdin.Write('\t');
                    _stdin.Write(idText);
                    if (!string.IsNullOrEmpty(job.Args))
                    {
                        _stdin.Write('\t');
                        _stdin.Write(job.Args);
                    }
                    _stdin.Write('\n');
                    _stdin.Flush();

                    while (true)
                    {
                        string line = _stdout.ReadLine();
                        if (line == null) throw ProcessLost(null);
                        if (line.StartsWith(ok, StringComparison.Ordinal)) return line.Substring(ok.Length);
                        if (line.StartsWith(err, StringComparison.Ordinal)) throw new TTSException(DecodeText(line.Substring(err.Length)));
                        // Anything else is stray output: ignore it.
                    }
                }
                catch (IOException e) { throw ProcessLost(e); }
                catch (ObjectDisposedException e) { throw ProcessLost(e); }
                catch (InvalidOperationException e) { throw ProcessLost(e); }
            }
        }

        void EnsureStarted()
        {
            if (IsAlive()) return;
            ReleaseProcess();
            if (_disposed) throw new TTSException("The speech engine was shut down.");

            var info = new ProcessStartInfo
            {
                FileName = PowerShellPath(),
                Arguments = "-NoLogo -NoProfile -NonInteractive -EncodedCommand " + TTSWorkerScript.Encoded,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.ASCII,
                StandardErrorEncoding = Encoding.UTF8
            };

            Process process;
            try { process = Process.Start(info); }
            catch (Exception e) { throw new TTSException("Could not start Windows PowerShell: " + e.Message, e); }
            if (process == null) throw new TTSException("Could not start Windows PowerShell.");

            lock (_stderr) _stderr.Length = 0;
            process.ErrorDataReceived += OnStderr;
            process.BeginErrorReadLine();
            _stdin = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false));
            _stdout = process.StandardOutput;
            lock (_processLock) _process = process;

            using (Watch(-1, StartTimeoutMs))
            {
                while (true)
                {
                    string line;
                    try { line = _stdout.ReadLine(); }
                    catch (Exception e) { throw ProcessLost(e); }
                    if (line == null) throw ProcessLost(null);
                    if (line == "READY") return;
                    if (line.StartsWith("FATAL\t", StringComparison.Ordinal))
                    {
                        string message = DecodeText(line.Substring(6));
                        ReleaseProcess();
                        throw new TTSException(message);
                    }
                }
            }
        }

        IDisposable Watch(int id, int timeoutMs)
        {
            _timedOut = false;
            Volatile.Write(ref _watchedId, id);
            var timer = new Timer(_ =>
            {
                if (Volatile.Read(ref _watchedId) != id) return;
                _timedOut = true;
                KillCurrentProcess();
            }, null, timeoutMs, Timeout.Infinite);

            return new Disposable(() =>
            {
                Volatile.Write(ref _watchedId, 0);
                timer.Dispose();
            });
        }

        TTSException ProcessLost(Exception inner)
        {
            ReleaseProcess();
            if (_disposed) return new TTSException("The speech engine was shut down.");
            if (_timedOut) return new TTSException("The speech engine did not answer in time and will restart on the next request.");
            return new TTSException("The speech engine stopped unexpectedly." + StderrTail(), inner);
        }

        bool IsAlive()
        {
            lock (_processLock)
            {
                try { return _process != null && !_process.HasExited; }
                catch (Exception) { return false; }
            }
        }

        // Any thread: kills the process so a blocked read returns.
        void KillCurrentProcess()
        {
            lock (_processLock)
            {
                try { if (_process != null && !_process.HasExited) _process.Kill(); }
                catch (Exception) { /* already gone */ }
            }
        }

        // Worker thread (or Dispose): kills and frees the process.
        void ReleaseProcess()
        {
            Process process;
            lock (_processLock)
            {
                process = _process;
                _process = null;
            }
            if (process == null) return;
            try { process.ErrorDataReceived -= OnStderr; } catch (Exception) { }
            try { if (!process.HasExited) process.Kill(); } catch (Exception) { }
            try { process.Dispose(); } catch (Exception) { }
        }

        void OnStderr(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            lock (_stderr)
            {
                _stderr.AppendLine(e.Data);
                if (_stderr.Length > 4000) _stderr.Remove(0, _stderr.Length - 4000);
            }
        }

        string StderrTail()
        {
            lock (_stderr)
            {
                string text = _stderr.ToString().Trim();
                return text.Length == 0 ? "" : "\n" + text;
            }
        }

        static string DecodeText(string base64)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(base64)); }
            catch (FormatException) { return base64; }
        }

        static string PowerShellPath()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
                if (File.Exists(path)) return path;
            }
            catch (Exception) { /* fall back to PATH */ }
            return "powershell.exe";
        }
    }
}
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Text;

namespace KrostGames.TTS.Engine
{
    /// <summary>
    /// The Windows PowerShell 5.1 program run by <see cref="TTSWorker"/>. It loads both Windows speech engines once
    /// (SAPI 5 through System.Speech and OneCore through Windows.Media.SpeechSynthesis) and then answers one request
    /// per line on stdin. Every text field travels as base64, so quotes, accents or symbols never need escaping.
    ///
    /// Requests:  VOICES  id
    ///            SPEAK   id  engine(S|W)  voiceId(b64)  culture  rate  pitch  volume  ssml(0|1)  text(b64)
    ///            QUIT
    /// Answers:   READY | FATAL message(b64) | OK id payload | ERR id message(b64)
    /// </summary>
    internal static class TTSWorkerScript
    {
        const string Source = @"
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$T = [char]9
$out = [Console]::Out
$in = [Console]::In
function Send([string]$msg) { $out.Write($msg); $out.Write([char]10); $out.Flush() }
function Enc([string]$s) { [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($s)) }
function Dec([string]$s) { [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($s)) }

$sapi = $null
try { Add-Type -AssemblyName System.Speech; $sapi = New-Object System.Speech.Synthesis.SpeechSynthesizer } catch { $sapi = $null }

$rt = $null; $rtVoices = @(); $asTask = $null
try {
    Add-Type -AssemblyName System.Runtime.WindowsRuntime
    $null = [Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows.Media.SpeechSynthesis, ContentType = WindowsRuntime]
    $generic = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' } | Select-Object -First 1
    $asTask = $generic.MakeGenericMethod([Windows.Media.SpeechSynthesis.SpeechSynthesisStream])
    $rt = New-Object Windows.Media.SpeechSynthesis.SpeechSynthesizer
    $rtVoices = @([Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices)
} catch { $rt = $null; $rtVoices = @() }

if ($sapi -eq $null -and $rt -eq $null) { Send ('FATAL' + $T + (Enc 'The Windows speech engines (SAPI 5 and OneCore) could not be loaded.')); exit 1 }
Send 'READY'

function Get-VoiceList {
    $list = New-Object 'System.Collections.Generic.List[string]'
    if ($sapi -ne $null) {
        foreach ($iv in $sapi.GetInstalledVoices()) {
            if (-not $iv.Enabled) { continue }
            $vi = $iv.VoiceInfo
            $list.Add('S|' + $vi.Name + '|' + $vi.Name + '|' + $vi.Culture.Name + '|' + $vi.Gender)
        }
    }
    foreach ($v in $rtVoices) { $list.Add('W|' + $v.Id + '|' + $v.DisplayName + '|' + $v.Language + '|' + $v.Gender) }
    return ($list.ToArray() -join [string][char]10)
}

function Invoke-Sapi([string]$voice, [string]$culture, [int]$rate, [int]$pitch, [int]$volume, [bool]$ssml, [string]$text) {
    if ($voice) { $sapi.SelectVoice($voice) }
    $sapi.Rate = [Math]::Max(-10, [Math]::Min(10, $rate))
    $sapi.Volume = [Math]::Max(0, [Math]::Min(100, $volume))
    $ms = New-Object System.IO.MemoryStream
    $sapi.SetOutputToWaveStream($ms)
    try {
        if ($ssml) { $sapi.SpeakSsml($text) }
        elseif ($pitch -ne 0) {
            $pct = [Math]::Max(-10, [Math]::Min(10, $pitch)) * 6
            if ($pct -gt 0) { $sign = '+' } else { $sign = '' }
            $esc = [Security.SecurityElement]::Escape($text)
            $sapi.SpeakSsml(""<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='$culture'><prosody pitch='$sign$pct%'>$esc</prosody></speak>"")
        }
        else { $sapi.Speak($text) }
    } finally { $sapi.SetOutputToNull() }
    $script:audio = $ms.ToArray()
}

function Invoke-OneCore([string]$voiceId, [int]$rate, [int]$pitch, [int]$volume, [bool]$ssml, [string]$text) {
    $voice = $null
    foreach ($v in $rtVoices) { if ($v.Id -eq $voiceId) { $voice = $v; break } }
    if ($voice -eq $null) { throw ('Voice not found: ' + $voiceId) }
    $rt.Voice = $voice
    try {
        $rt.Options.SpeakingRate = [Math]::Max(0.5, [Math]::Min(6.0, [Math]::Pow(3.0, $rate / 10.0)))
        $rt.Options.AudioPitch = [Math]::Max(0.0, [Math]::Min(2.0, 1.0 + $pitch * 0.06))
        $rt.Options.AudioVolume = [Math]::Max(0.0, [Math]::Min(1.0, $volume / 100.0))
    } catch { }
    if ($ssml) { $op = $rt.SynthesizeSsmlToStreamAsync($text) } else { $op = $rt.SynthesizeTextToStreamAsync($text) }
    $task = $asTask.Invoke($null, @($op))
    $null = $task.Wait(-1)
    $stream = $task.Result
    $src = [System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($stream)
    $ms = New-Object System.IO.MemoryStream
    $src.CopyTo($ms)
    $src.Dispose()
    $stream.Dispose()
    $script:audio = $ms.ToArray()
}

while ($true) {
    $line = $in.ReadLine()
    if ($line -eq $null) { break }
    $line = $line.Trim([char]0xFEFF, [char]13, [char]32)
    if ($line.Length -eq 0) { continue }
    $p = $line.Split($T)
    $cmd = $p[0]
    if ($cmd -eq 'QUIT') { break }
    $id = '0'
    if ($p.Length -gt 1) { $id = $p[1] }
    try {
        if ($cmd -eq 'VOICES') { Send ('OK' + $T + $id + $T + (Enc (Get-VoiceList))) }
        elseif ($cmd -eq 'SPEAK') {
            $engine = $p[2]; $voice = Dec $p[3]; $culture = $p[4]
            $rate = [int]$p[5]; $pitch = [int]$p[6]; $volume = [int]$p[7]
            $ssml = ($p[8] -eq '1'); $text = Dec $p[9]
            $script:audio = $null
            if ($engine -eq 'W') {
                if ($rt -eq $null) { throw 'The OneCore speech engine is not available.' }
                Invoke-OneCore $voice $rate $pitch $volume $ssml $text
            } else {
                if ($sapi -eq $null) { throw 'The SAPI speech engine is not available.' }
                Invoke-Sapi $voice $culture $rate $pitch $volume $ssml $text
            }
            Send ('OK' + $T + $id + $T + [Convert]::ToBase64String($script:audio))
        }
        elseif ($cmd -eq 'PING') { Send ('OK' + $T + $id + $T + 'PONG') }
        else { Send ('ERR' + $T + $id + $T + (Enc ('Unknown command: ' + $cmd))) }
    } catch {
        $msg = $_.Exception.Message
        if ($_.Exception.InnerException -ne $null) { $msg = $_.Exception.InnerException.Message }
        Send ('ERR' + $T + $id + $T + (Enc $msg))
    }
}
if ($sapi -ne $null) { $sapi.Dispose() }
";

        static string _encoded;

        /// <summary>The script as PowerShell -EncodedCommand expects it (UTF-16LE, base64).</summary>
        public static string Encoded => _encoded ?? (_encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(Source)));
    }
}
#endif

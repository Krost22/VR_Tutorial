string OUT = "C:/Users/eduar/AppData/Local/Temp/claude/C--Users-eduar-OneDrive-Documentos-Github-VR-Tutorial/9a572df9-bdb3-4a3b-84fc-8a5bada7436d/scratchpad/rec/audio/";
var user = KrostGames.TTS.EditorTools.TTSUserSettings.instance.voice;
var es = new KrostGames.TTS.TTSVoiceOptions(user.voiceName, user.language, user.rate, user.pitch, user.volume);
var speaker = UnityEngine.GameObject.Find("English guide").GetComponent<KrostGames.TTS.TTSSpeaker>();
var jobs = new (string file, string text, KrostGames.TTS.TTSVoiceOptions voice)[] {
    ("A0", "Welcome to the training room. Grab the red cube to begin.", new KrostGames.TTS.TTSVoiceOptions("Microsoft Zira Desktop", KrostGames.TTS.TTSLanguage.English, 0, 0, 100)),
    ("A1", "¡Bien hecho! Ahora presiona el botón verde.", es),
    ("A2", "Checkpoint reached. Well done!", speaker.Voice),
};
System.Func<System.Threading.Tasks.Task> run = async () =>
{
    var log = new System.Text.StringBuilder("es voice=" + es.voiceName + " pitch=" + es.pitch + " speaker voice=" + speaker.Voice.voiceName + " pitch=" + speaker.Voice.pitch + "\n");
    foreach (var j in jobs)
    {
        var d = await KrostGames.TTS.LocalTTS.SynthesizeDataAsync(j.text, j.voice);
        System.IO.File.WriteAllBytes(OUT + j.file + ".wav", KrostGames.TTS.WavUtility.Encode(d.Samples, d.Channels, d.SampleRate));
        log.Append(j.file + " " + d.Length.ToString("F3") + "s " + d.SampleRate + "Hz\n");
    }
    System.IO.File.WriteAllText(OUT + "resynth.txt", log.ToString());
};
_ = run();
return "started";

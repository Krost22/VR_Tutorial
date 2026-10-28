using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRTutorial.GameFeel.EditorTools
{
    /// <summary>
    /// Sintetiza los sonidos del kit de game feel. Menú: Tools/Game Feel/Regenerar sonidos.
    ///
    /// Buenas prácticas aplicadas (ver Docs/CAMBIOS_GameFeel.md, "Sonidos rehechos"):
    /// - Capas: cada sonido se arma con un transitorio (el "clic" que da el momento exacto), un cuerpo
    ///   (el tono o golpe que dice qué es) y una cola (el aire o la resonancia que lo asienta en la sala).
    /// - Sonoridad igualada por categoría con una medida tipo LUFS (ponderación K simplificada, ventanas
    ///   de 400 ms) en lugar de normalizar por pico, para que todos suenen parejos entre sí.
    /// - Techo de pico a -1 dBFS con limitador suave (deja margen al sumar varios sonidos).
    /// - Altavoces del Quest: casi no reproducen por debajo de ~150 Hz. Los golpes graves llevan armónicos
    ///   (saturación suave) para que se oigan; se filtra lo que está por debajo de 30 Hz, que solo gasta margen.
    /// - Errores suaves, nunca estridentes (comodidad en VR); premios más brillantes y algo más fuertes.
    /// - Sin clics: fundido de entrada de 1 ms y de salida de 20 ms; los bucles se cierran sin costura.
    /// - Variantes: los sonidos más repetidos tienen 3 tomas (banco FeelSoundBank) para que no suenen robóticos.
    /// - Importación: mono; cortos con Decompress On Load (sin costo al reproducir), largos comprimidos en memoria.
    /// Los nombres de archivo no cambian, así que las escenas siguen apuntando a los mismos clips.
    /// </summary>
    public static class GameFeelAudioBuilder
    {
        const int Rate = 44100;
        const string BankPath = GameFeelAssetBuilder.Root + "/Resources/FeelSoundBank.asset";
        const float PeakCeiling = 0.891f; // -1 dBFS

        // Sonoridad objetivo (LUFS de corto plazo, máximo) por tipo de sonido.
        enum Cat { Ui, Air, Impact, Reward, BigReward, Negative, Loop, Ambience }
        static float Target(Cat c) => c switch
        {
            Cat.Ui => -17f,
            Cat.Air => -19f,
            Cat.Impact => -15f,
            Cat.Reward => -15f,
            Cat.BigReward => -13f,
            Cat.Negative => -19f,
            Cat.Loop => -22f,
            Cat.Ambience => -28f, // fondo: se siente más de lo que se oye
            _ => -18f
        };

        [MenuItem("Tools/Game Feel/Regenerar sonidos")]
        public static void BuildMenu() => Debug.Log("[GameFeel] Sonidos:\n" + BuildAll());

        public static string BuildAll()
        {
            Directory.CreateDirectory(GameFeelAssetBuilder.AudioDir);
            var report = new List<string>();
            var variants = new Dictionary<string, List<string>>();

            void Make(string name, Cat cat, Func<int, float[]> synth, int takes = 1, bool loop = false)
            {
                var names = new List<string>();
                for (int v = 0; v < takes; v++)
                {
                    string file = v == 0 ? name : name + "_v" + (v + 1);
                    var b = synth(v);
                    report.Add(file + ": " + Master(b, Target(cat), loop, out b));
                    WriteWav(file, b);
                    names.Add(file);
                }
                if (takes > 1) variants[name] = names;
            }

            Make("feel_pop", Cat.Ui, Pop, 3);
            Make("feel_chime", Cat.Reward, v => Chime());
            Make("feel_success", Cat.Reward, v => Success());
            Make("feel_fanfare", Cat.BigReward, v => Fanfare());
            Make("feel_powerdown", Cat.Impact, v => PowerDown());
            Make("feel_error", Cat.Negative, Error, 2);
            Make("feel_whoosh", Cat.Air, Whoosh, 3);
            Make("feel_thud", Cat.Impact, Thud, 3);
            Make("feel_step", Cat.Ui, Step, 3);          // más suave que un golpe: se repite mucho
            Make("feel_lock", Cat.Impact, v => Lock());
            Make("feel_charge", Cat.Loop, v => Charge(), 1, true);
            Make("feel_rumble", Cat.Impact, v => Rumble());
            Make("feel_swish", Cat.Air, Swish, 3);
            Make("feel_gaze_hum", Cat.Loop, v => GazeHum(), 1, true);
            // Mazmorra
            Make("feel_activate", Cat.Ui, v => Activate());
            Make("feel_portal_hum", Cat.Loop, v => PortalHum(), 1, true);
            Make("feel_room_tone", Cat.Ambience, v => RoomTone(), 1, true);
            Make("feel_torch_crackle", Cat.Loop, v => TorchCrackle(), 1, true);

            AssetDatabase.Refresh();
            ConfigureImport();
            BuildBank(variants);
            AssetDatabase.SaveAssets();
            return string.Join("\n", report);
        }

        /// <summary>Solo el zumbido de la fase de mirada (lo usa GazePhaseBuilder).</summary>
        public static AudioClip BuildGazeHum()
        {
            var b = GazeHum();
            Master(b, Target(Cat.Loop), true, out b);
            WriteWav("feel_gaze_hum", b);
            string path = GameFeelAssetBuilder.AudioDir + "/feel_gaze_hum.wav";
            AssetDatabase.ImportAsset(path);
            ConfigureImport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ================================================================ Sonidos

        static readonly float[] TakePitch = { 1f, 0.94f, 1.07f };
        static float Pitch(int v) => TakePitch[v % TakePitch.Length];

        // Aparición / confirmación de interfaz: clic + "blip" que sube + destello agudo.
        static float[] Pop(int v)
        {
            float p = Pitch(v);
            var b = Buf(0.28f);
            Noise(b, 0f, 0.006f, 0.35f, k => 1f - k, new Biquad(Biquad.HP(2500f)), 100 + v);              // transitorio
            Osc(b, 0f, 0.075f, k => Mathf.Lerp(420f, 980f, Mathf.Sqrt(k)) * p, k => Env(k, 0.03f, 5f), 0.8f, new[] { 1f, 0.22f, 0.08f }); // cuerpo
            Bell(b, 0.018f, 1320f * p, 0.16f, 22f);                                                          // cola
            return b;
        }

        // Una nota de logro (se sube de tono por escala desde el código): campana con golpe de mazo.
        static float[] Chime()
        {
            var b = Buf(1.5f);
            Noise(b, 0f, 0.004f, 0.18f, k => 1f - k, new Biquad(Biquad.HP(4000f)), 7);
            Bell(b, 0f, 880f, 0.55f, 3.2f);
            Bell(b, 0f, 880f * 1.003f, 0.2f, 3.6f); // leve batido: brillo vivo
            return b;
        }

        // Arpegio mayor ascendente (Do-Mi-Sol-Do) + brillo final.
        static float[] Success()
        {
            var b = Buf(1.8f);
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < notes.Length; i++)
            {
                float t = i * 0.085f;
                Noise(b, t, 0.004f, 0.12f, k => 1f - k, new Biquad(Biquad.HP(4000f)), 20 + i);
                Bell(b, t, notes[i], i == notes.Length - 1 ? 0.5f : 0.36f, 3f);
            }
            Noise(b, 0.3f, 0.9f, 0.05f, k => Mathf.Sin(Mathf.PI * Mathf.Min(1f, k * 3f)) * (1f - k), new Biquad(Biquad.HP(6500f)), 29);
            return b;
        }

        // Gran premio: golpe de timbal, acorde de metales que se abre, arpegio y platillo.
        static float[] Fanfare()
        {
            var b = Buf(2.8f);
            // Timbal: caída de tono con armónicos para que se oiga en altavoces pequeños.
            var drum = Buf(0.6f);
            Osc(drum, 0f, 0.55f, k => Mathf.Lerp(130f, 92f, Mathf.Sqrt(k)), k => Env(k, 0.01f, 7f), 0.9f, new[] { 1f, 0.5f, 0.25f });
            Noise(drum, 0f, 0.02f, 0.4f, k => 1f - k, new Biquad(Biquad.LP(3000f)), 31);
            SoftClip(drum, 1.8f);
            Mix(b, drum, 0f, 0.7f);
            // Metales: suma de armónicos tipo sierra, brillantes al ataque y más oscuros después (como un filtro que se cierra).
            float[] chord = { 261.63f, 329.63f, 392f, 523.25f };
            for (int c = 0; c < chord.Length; c++)
                Brass(b, 0.05f + c * 0.015f, 2.5f, chord[c], 0.16f, 40 + c);
            float[] arp = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f, 1568f };
            for (int i = 0; i < arp.Length; i++) Bell(b, 0.15f + i * 0.075f, arp[i], 0.2f, 2.6f);
            // Platillo: aire agudo que crece y se apaga.
            Noise(b, 0.05f, 2.4f, 0.08f, k => Mathf.Min(1f, k * 8f) * Mathf.Pow(1f - k, 1.5f), new Biquad(Biquad.HP(6000f)), 51);
            return b;
        }

        // Láser que se apaga: barrido descendente zumbón, chisporroteo y "clonc" final.
        static float[] PowerDown()
        {
            var b = Buf(0.85f);
            var lp = new Biquad(Biquad.LP(6000f));
            Osc(b, 0f, 0.7f, k => Mathf.Lerp(880f, 70f, 1f - Mathf.Pow(1f - k, 2.2f)), k => Mathf.Pow(1f - k, 1.2f) * Mathf.Min(1f, k * 60f), 0.5f, new[] { 1f, 0.45f, 0.3f, 0.15f }, lp, k => Mathf.Lerp(6000f, 500f, k));
            var rng = new System.Random(3);
            for (int i = 0; i < 14; i++)
            {
                float t = (float)(rng.NextDouble() * 0.45);
                Noise(b, t, 0.003f, 0.25f * (1f - t / 0.45f), k => 1f - k, new Biquad(Biquad.BP(3200f, 1.5f)), 60 + i);
            }
            var thunk = Buf(0.2f);
            Osc(thunk, 0f, 0.18f, k => Mathf.Lerp(110f, 55f, k), k => Env(k, 0.02f, 9f), 0.6f, new[] { 1f, 0.4f, 0.2f });
            SoftClip(thunk, 2f);
            Mix(b, thunk, 0.6f, 0.5f);
            return b;
        }

        // Cubo equivocado: "bonc-bonc" descendente, suave y de madera. Avisa sin castigar.
        static float[] Error(int v)
        {
            float p = v == 0 ? 1f : 0.95f;
            var b = Buf(0.45f);
            float[] notes = { 329.63f * p, 261.63f * p };
            for (int i = 0; i < 2; i++)
            {
                float t = i * 0.13f;
                Noise(b, t, 0.006f, 0.25f, k => 1f - k, new Biquad(Biquad.BP(1200f, 1.2f)), 70 + i + v * 5);
                Osc(b, t, 0.2f, k => notes[i], k => Env(k, 0.025f, 9f), 0.6f, new[] { 1f, 0.15f, 0.3f, 0f, 0.08f });
            }
            Filter(b, new Biquad(Biquad.LP(2200f)));
            return b;
        }

        // Objeto que vuela: ruido rosa por un filtro de banda que sube y baja, con aleteo leve.
        static float[] Whoosh(int v)
        {
            float p = Pitch(v);
            var b = Buf(0.62f);
            var rng = new System.Random(80 + v);
            var bp = new Biquad(Biquad.BP(600f, 1.1f));
            float pink = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float k = i / (float)b.Length;
                float center = (300f + 1500f * Mathf.Sin(Mathf.PI * Mathf.Pow(k, 0.8f))) * p;
                if (i % 32 == 0) bp.Set(Biquad.BP(center, 1.1f));
                pink = 0.97f * pink + 0.03f * (float)(rng.NextDouble() * 2 - 1) * 6f;
                float flutter = 1f - 0.18f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (14f + v * 2f) * i / Rate));
                b[i] = bp.Process(pink) * Mathf.Pow(Mathf.Sin(Mathf.PI * k), 1.6f) * flutter;
            }
            Noise(b, 0.15f, 0.35f, 0.04f, k => Mathf.Sin(Mathf.PI * k), new Biquad(Biquad.HP(5000f)), 90 + v);
            return b;
        }

        // Golpe: transitorio + cuerpo que cae de tono (con armónicos) + nudillo de madera + cola de sala.
        static float[] Thud(int v)
        {
            float p = Pitch(v);
            var b = Buf(0.45f);
            Noise(b, 0f, 0.008f, 0.6f, k => 1f - k, new Biquad(Biquad.LP(3500f)), 110 + v);
            var body = Buf(0.35f);
            Osc(body, 0f, 0.32f, k => Mathf.Lerp(160f, 55f, Mathf.Sqrt(k)) * p, k => Env(k, 0.01f, 9f), 1f, new[] { 1f, 0.3f });
            SoftClip(body, 2.4f); // armónicos: el golpe se oye en los altavoces del visor
            Mix(b, body, 0f, 0.75f);
            Osc(b, 0f, 0.08f, k => 320f * p, k => Env(k, 0.01f, 12f), 0.25f, new[] { 1f });
            Noise(b, 0.01f, 0.35f, 0.12f, k => Mathf.Exp(-k * 6f), new Biquad(Biquad.LP(700f)), 120 + v);
            return b;
        }

        // Paso / aterrizaje: como el golpe pero corto, más agudo y con roce.
        static float[] Step(int v)
        {
            float p = Pitch(v);
            var b = Buf(0.26f);
            Noise(b, 0f, 0.012f, 0.35f, k => 1f - k, new Biquad(Biquad.BP(2500f, 0.8f)), 130 + v);
            var body = Buf(0.2f);
            Osc(body, 0f, 0.18f, k => Mathf.Lerp(210f, 110f, Mathf.Sqrt(k)) * p, k => Env(k, 0.01f, 10f), 1f, new[] { 1f, 0.25f });
            SoftClip(body, 2f);
            Mix(b, body, 0.002f, 0.6f);
            return b;
        }

        // Pilar que se fija: doble clic metálico + golpe + campanita de confirmación.
        static float[] Lock()
        {
            var b = Buf(1.1f);
            Noise(b, 0f, 0.004f, 0.5f, k => 1f - k, new Biquad(Biquad.BP(3200f, 2f)), 141);
            Noise(b, 0.045f, 0.004f, 0.4f, k => 1f - k, new Biquad(Biquad.BP(2600f, 2f)), 142);
            Tone(b, 0.045f, 520f, 0.22f, 20f);                  // resonancia metálica (parciales inarmónicos)
            Tone(b, 0.045f, 1310f, 0.16f, 26f);
            Tone(b, 0.045f, 2470f, 0.1f, 32f);
            var body = Buf(0.25f);
            Osc(body, 0f, 0.22f, k => Mathf.Lerp(150f, 70f, Mathf.Sqrt(k)), k => Env(k, 0.01f, 8f), 1f, new[] { 1f, 0.3f });
            SoftClip(body, 2.2f);
            Mix(b, body, 0.045f, 0.6f);
            Bell(b, 0.09f, 1046.5f, 0.22f, 3.8f);
            return b;
        }

        // Bucle de carga del pilar (1 s exacto): acorde cálido con trémolo y chispa suave. El código le sube el tono.
        static float[] Charge()
        {
            var b = Buf(1f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float trem = 0.78f + 0.22f * Mathf.Sin(2f * Mathf.PI * 4f * t); // 4 ciclos en 1 s: cierra el bucle
                float s = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.45f
                          + Mathf.Sin(2f * Mathf.PI * 221f * t) * 0.2f   // 1 Hz de batido: 1 ciclo por bucle
                          + Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.28f
                          + Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.14f
                          + Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.07f;
                b[i] = s * trem * 0.5f;
            }
            var air = LoopNoise(1f, 0.04f, new Biquad(Biquad.BP(2400f, 0.9f)), 150);
            for (int i = 0; i < b.Length; i++) b[i] += air[i];
            return b;
        }

        // Pared que se hunde: grave de roca (con armónicos), roce de piedra y crujidos.
        static float[] Rumble()
        {
            var b = Buf(1.8f);
            var rng = new System.Random(21);
            float brown = 0f;
            var lp = new Biquad(Biquad.LP(220f));
            var grindBp = new Biquad(Biquad.BP(750f, 0.9f));
            for (int i = 0; i < b.Length; i++)
            {
                float k = i / (float)b.Length;
                float white = (float)(rng.NextDouble() * 2 - 1);
                brown = Mathf.Clamp(brown * 0.996f + white * 0.06f, -1f, 1f);
                float low = lp.Process(brown) * 3.5f;
                float am = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * (9f + 3f * Mathf.Sin(k * 7f)) * i / Rate));
                float grind = grindBp.Process(white) * 0.35f * am; // roce: se oye aunque el altavoz no tenga graves
                float env = Mathf.Min(1f, k / 0.06f) * Mathf.Min(1f, (1f - k) / 0.3f);
                b[i] = (low + grind) * env;
            }
            for (int i = 0; i < 9; i++)
            {
                float t = 0.05f + (float)(rng.NextDouble() * 1.3);
                Noise(b, t, 0.02f, 0.3f, k => Mathf.Exp(-k * 5f), new Biquad(Biquad.BP(1500f + (float)rng.NextDouble() * 1000f, 1.2f)), 160 + i);
            }
            var sub = Buf(1.7f);
            Osc(sub, 0f, 1.65f, k => Mathf.Lerp(58f, 48f, k), k => Mathf.Min(1f, k / 0.08f) * (1f - k), 0.5f, new[] { 1f, 0.35f, 0.2f });
            SoftClip(sub, 1.6f);
            Mix(b, sub, 0f, 0.5f);
            return b;
        }

        // Movimiento rápido de algo pequeño (aro, luz): barrido de aire corto con chispa.
        static float[] Swish(int v)
        {
            float p = Pitch(v);
            var b = Buf(0.42f);
            var rng = new System.Random(170 + v);
            var bp = new Biquad(Biquad.BP(1000f, 1.4f));
            for (int i = 0; i < b.Length; i++)
            {
                float k = i / (float)b.Length;
                if (i % 32 == 0) bp.Set(Biquad.BP(Mathf.Lerp(900f, 5200f, Mathf.Sqrt(k)) * p, 1.4f));
                float env = k < 0.08f ? k / 0.08f : Mathf.Pow(1f - (k - 0.08f) / 0.92f, 2f);
                b[i] = bp.Process((float)(rng.NextDouble() * 2 - 1)) * env;
            }
            Bell(b, 0.05f, 1567.98f * p, 0.1f, 7f);
            return b;
        }

        // Zumbido de la luz que hay que mirar (bucle de 2 s): quinta suave que "respira" y aire brillante.
        static float[] GazeHum()
        {
            var b = Buf(2f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float breathe = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 1f * t); // 2 ciclos en el bucle
                float s = 0.5f * Mathf.Sin(2f * Mathf.PI * 220f * t)
                          + 0.28f * Mathf.Sin(2f * Mathf.PI * 330f * t)
                          + 0.12f * Mathf.Sin(2f * Mathf.PI * 440.5f * t) // 0,5 Hz de batido: 1 ciclo por bucle
                          + 0.06f * Mathf.Sin(2f * Mathf.PI * 660f * t);
                b[i] = s * breathe * 0.5f;
            }
            var air = LoopNoise(2f, 0.025f, new Biquad(Biquad.BP(3200f, 0.8f)), 180);
            for (int i = 0; i < b.Length; i++) b[i] += air[i];
            return b;
        }

        // ---------------------------------------------------------------- Mazmorra

        // Pilar que se activa al poner su runa: energía que sube y campanita. Reemplaza la "explosión" vieja.
        static float[] Activate()
        {
            var b = Buf(1.1f);
            var rng = new System.Random(200);
            var bp = new Biquad(Biquad.BP(400f, 1.3f));
            int n = (int)(0.55f * Rate);
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                if (i % 32 == 0) bp.Set(Biquad.BP(Mathf.Lerp(350f, 3200f, k * k), 1.3f));
                b[i] += bp.Process((float)(rng.NextDouble() * 2 - 1)) * Mathf.Pow(k, 1.5f) * 0.9f;
            }
            Osc(b, 0f, 0.55f, k => Mathf.Lerp(220f, 660f, k * k), k => Mathf.Pow(k, 2f), 0.18f, new[] { 1f, 0.3f });
            var thump = Buf(0.25f);
            Osc(thump, 0f, 0.22f, k => Mathf.Lerp(140f, 70f, Mathf.Sqrt(k)), k => Env(k, 0.02f, 8f), 0.8f, new[] { 1f, 0.3f });
            SoftClip(thump, 2f);
            Mix(b, thump, 0.52f, 0.4f);
            Bell(b, 0.53f, 1318.5f, 0.3f, 3.5f);
            Bell(b, 0.53f, 1975.5f, 0.12f, 4.5f);
            return b;
        }

        // Portal final (bucle de 2 s): grave místico con brillo que gira y aire.
        static float[] PortalHum()
        {
            var b = Buf(2f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float swirl = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 1.5f * t); // 3 ciclos en el bucle
                float s = 0.45f * Mathf.Sin(2f * Mathf.PI * 110f * t)
                          + 0.3f * Mathf.Sin(2f * Mathf.PI * 165f * t)
                          + 0.25f * Mathf.Sin(2f * Mathf.PI * 220f * t)          // armónico audible en altavoces pequeños
                          + 0.08f * swirl * Mathf.Sin(2f * Mathf.PI * 880f * t)
                          + 0.05f * (1f - swirl) * Mathf.Sin(2f * Mathf.PI * 1320f * t);
                b[i] = s * 0.5f;
            }
            var air = LoopNoise(2f, 0.05f, new Biquad(Biquad.BP(2600f, 0.7f)), 210);
            for (int i = 0; i < b.Length; i++) b[i] += air[i];
            return b;
        }

        // Tono de sala de la mazmorra (bucle de 8 s): aire grave, corriente lejana y alguna gota.
        static float[] RoomTone()
        {
            float seconds = 8f;
            int n = (int)(seconds * Rate), fade = Rate / 2;
            var raw = new float[n + fade];
            var rng = new System.Random(220);
            var lp = new Biquad(Biquad.LP(260f));
            var wind = new Biquad(Biquad.BP(520f, 0.6f));
            float brown = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                float white = (float)(rng.NextDouble() * 2 - 1);
                brown = Mathf.Clamp(brown * 0.997f + white * 0.05f, -1f, 1f);
                float gust = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * i / (float)n * 2f) * Mathf.Sin(2f * Mathf.PI * i / (float)n * 3f);
                raw[i] = lp.Process(brown) * 2.5f + wind.Process(white) * 0.12f * gust;
            }
            // Gotas lejanas: 3 por bucle, suaves y con algo de eco (no deben llamar la atención).
            float[] drops = { 1.3f, 4.1f, 6.6f };
            foreach (var d in drops)
            {
                float f = 1700f + (float)rng.NextDouble() * 900f;
                var drop = new float[Rate / 2];
                Osc(drop, 0f, 0.06f, k => f * (1f + 0.6f * k), k => Env(k, 0.05f, 10f), 0.25f, new[] { 1f });
                Mix(raw, drop, d, 1f);
                Mix(raw, drop, d + 0.11f, 0.3f);
            }
            return CloseLoop(raw, n, fade);
        }

        // Crepitar de antorcha (bucle de 4 s): rugido suave de llama y chasquidos al azar.
        static float[] TorchCrackle()
        {
            float seconds = 4f;
            int n = (int)(seconds * Rate), fade = Rate / 4;
            var raw = new float[n + fade];
            var rng = new System.Random(230);
            var roar = new Biquad(Biquad.LP(500f));
            float pink = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                pink = 0.98f * pink + 0.02f * (float)(rng.NextDouble() * 2 - 1) * 5f;
                float flicker = 0.7f + 0.3f * Mathf.PerlinNoise(i / (float)Rate * 3f, 0.5f);
                raw[i] = roar.Process(pink) * 0.5f * flicker;
            }
            // Chasquidos: unos 18 por segundo, de distinto brillo y fuerza.
            int clicks = (int)(seconds * 18f);
            for (int c = 0; c < clicks; c++)
            {
                float t = (float)rng.NextDouble() * (seconds + fade / (float)Rate - 0.01f);
                float amp = Mathf.Pow((float)rng.NextDouble(), 2f) * 0.6f + 0.05f;
                float f = 1800f + (float)rng.NextDouble() * 3500f;
                Noise(raw, t, 0.001f + (float)rng.NextDouble() * 0.004f, amp, k => 1f - k, new Biquad(Biquad.BP(f, 1.5f)), 240 + c);
            }
            return CloseLoop(raw, n, fade);
        }

        /// <summary>Cierra un bucle: funde los últimos <paramref name="fade"/> samples (sobrantes) con el principio.</summary>
        static float[] CloseLoop(float[] raw, int n, int fade)
        {
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = raw[i];
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                o[i] = raw[i] * Mathf.Sin(k * Mathf.PI * 0.5f) + raw[n + i] * Mathf.Cos(k * Mathf.PI * 0.5f);
            }
            return o;
        }

        // ================================================================ Bloques de síntesis

        static float[] Buf(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];

        /// <summary>Envolvente: ataque lineal (fracción de k) y caída exponencial.</summary>
        static float Env(float k, float attack, float decay) => k < attack ? k / attack : Mathf.Exp(-(k - attack) * decay);

        /// <summary>
        /// Oscilador con frecuencia y volumen variables y suma de armónicos (amps[n] = armónico n+1).
        /// Opcional: filtro con corte que cambia en el tiempo.
        /// </summary>
        static void Osc(float[] b, float start, float dur, Func<float, float> freq, Func<float, float> env, float amp, float[] amps,
                        Biquad filter = null, Func<float, float> cutoff = null)
        {
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            double phase = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float k = i / (float)n;
                phase += 2 * Math.PI * freq(k) / Rate;
                float s = 0f;
                for (int h = 0; h < amps.Length; h++) if (amps[h] != 0f) s += amps[h] * (float)Math.Sin(phase * (h + 1));
                if (filter != null)
                {
                    if (cutoff != null && i % 32 == 0) filter.Set(Biquad.LP(cutoff(k)));
                    s = filter.Process(s);
                }
                b[s0 + i] += amp * env(k) * s;
            }
        }

        static void Tone(float[] b, float start, float freq, float amp, float decay)
        {
            int s0 = (int)(start * Rate);
            for (int i = s0; i < b.Length; i++)
            {
                float t = (i - s0) / (float)Rate;
                float env = t < 0.002f ? t / 0.002f : Mathf.Exp(-(t - 0.002f) * decay);
                if (env < 0.0003f && t > 0.01f) break;
                b[i] += amp * env * Mathf.Sin(2f * Mathf.PI * freq * t);
            }
        }

        /// <summary>Campana: parciales ligeramente inarmónicos; los agudos se apagan antes (como un metal real).</summary>
        static void Bell(float[] b, float start, float f, float amp, float decay)
        {
            Tone(b, start, f, amp, decay);
            Tone(b, start, f * 2.01f, amp * 0.42f, decay * 1.6f);
            Tone(b, start, f * 2.99f, amp * 0.2f, decay * 2.2f);
            Tone(b, start, f * 4.17f, amp * 0.1f, decay * 3f);
            Tone(b, start, f * 5.43f, amp * 0.05f, decay * 4f);
        }

        /// <summary>Nota de metales: armónicos 1/n que se oscurecen tras el ataque, vibrato tardío.</summary>
        static void Brass(float[] b, float start, float dur, float f, float amp, int seed)
        {
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            double phase = 0;
            var rng = new System.Random(seed);
            float drift = (float)(rng.NextDouble() - 0.5) * 0.004f; // cada voz un pelín desafinada: más "orquesta"
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float t = i / (float)Rate, k = i / (float)n;
                float vib = 1f + Mathf.Clamp01((t - 0.35f) * 3f) * 0.004f * Mathf.Sin(2f * Mathf.PI * 5.5f * t);
                phase += 2 * Math.PI * f * (1f + drift) * vib / Rate;
                float bright = Mathf.Lerp(0.12f, 0.45f, Mathf.Clamp01(t / 0.4f));
                float s = 0f;
                for (int h = 1; h <= 10; h++) s += Mathf.Exp(-(h - 1) * bright) / h * (float)Math.Sin(phase * h);
                float env = Mathf.Min(1f, t / 0.04f) * (k > 0.65f ? Mathf.Pow(1f - (k - 0.65f) / 0.35f, 1.5f) : 1f);
                b[s0 + i] += amp * env * s;
            }
        }

        static void Noise(float[] b, float start, float dur, float amp, Func<float, float> env, Biquad filter, int seed)
        {
            var rng = new System.Random(seed);
            int s0 = (int)(start * Rate), n = Mathf.Max(1, (int)(dur * Rate));
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float white = (float)(rng.NextDouble() * 2 - 1);
                b[s0 + i] += amp * env(i / (float)n) * filter.Process(white);
            }
        }

        /// <summary>Ruido filtrado que se repite sin costura: se genera de más y se funde el final con el principio.</summary>
        static float[] LoopNoise(float seconds, float amp, Biquad filter, int seed)
        {
            int n = (int)(seconds * Rate), fade = Rate / 10;
            var raw = new float[n + fade];
            var rng = new System.Random(seed);
            for (int i = 0; i < raw.Length; i++) raw[i] = amp * filter.Process((float)(rng.NextDouble() * 2 - 1));
            var outBuf = new float[n];
            for (int i = 0; i < n; i++) outBuf[i] = raw[i];
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade; // fundido de igual potencia
                outBuf[i] = raw[i] * Mathf.Sin(k * Mathf.PI * 0.5f) + raw[n + i] * Mathf.Cos(k * Mathf.PI * 0.5f);
            }
            return outBuf;
        }

        static void Mix(float[] dst, float[] src, float start, float gain)
        {
            int s0 = (int)(start * Rate);
            for (int i = 0; i < src.Length && s0 + i < dst.Length; i++) dst[s0 + i] += src[i] * gain;
        }

        static void Filter(float[] b, Biquad f)
        {
            for (int i = 0; i < b.Length; i++) b[i] = f.Process(b[i]);
        }

        /// <summary>Saturación suave (tanh): añade armónicos 2.º-3.º a los graves para altavoces pequeños.</summary>
        static void SoftClip(float[] b, float drive)
        {
            float norm = 1f / (float)Math.Tanh(drive);
            for (int i = 0; i < b.Length; i++) b[i] = (float)Math.Tanh(b[i] * drive) * norm;
        }

        // ================================================================ Masterización

        /// <summary>
        /// Limpia graves inaudibles, iguala la sonoridad al objetivo, limita picos a -1 dBFS, recorta el
        /// silencio final y pone fundidos. Devuelve el informe "x,xx s, -nn LUFS, pico -n dBFS".
        /// </summary>
        static string Master(float[] input, float targetLufs, bool loop, out float[] output)
        {
            var b = (float[])input.Clone();

            // 1) Paso alto 30 Hz (2 etapas) y paso bajo 16 kHz. En bucles se filtra dos veces seguidas y se
            //    queda la segunda pasada: el estado del filtro ya es periódico y no aparece un salto al repetir.
            var hp1 = new Biquad(Biquad.HP(30f)); var hp2 = new Biquad(Biquad.HP(30f)); var lp = new Biquad(Biquad.LP(16000f));
            int passes = loop ? 2 : 1;
            for (int pass = 0; pass < passes; pass++)
                for (int i = 0; i < b.Length; i++)
                {
                    float y = lp.Process(hp2.Process(hp1.Process(input[i] + 0f)));
                    if (pass == passes - 1) b[i] = y;
                }

            // 2) Sonoridad (máximo de corto plazo) y ganancia hacia el objetivo.
            float lufs = ShortTermLoudness(b);
            float gain = Mathf.Pow(10f, (targetLufs - lufs) / 20f);
            for (int i = 0; i < b.Length; i++) b[i] *= gain;

            // 3) Limitador suave por encima de -4 dBFS y techo duro de -1 dBFS.
            const float knee = 0.63f;
            for (int i = 0; i < b.Length; i++)
            {
                float a = Mathf.Abs(b[i]);
                if (a > knee) b[i] = Mathf.Sign(b[i]) * (knee + (PeakCeiling - knee) * (float)Math.Tanh((a - knee) / (PeakCeiling - knee)));
            }
            float peak = b.Max(x => Mathf.Abs(x));
            if (peak > PeakCeiling) { float g = PeakCeiling / peak; for (int i = 0; i < b.Length; i++) b[i] *= g; peak = PeakCeiling; }

            if (!loop)
            {
                // 4) Recortar la cola por debajo de -60 dBFS (menos memoria) y fundidos contra clics.
                int end = b.Length - 1;
                while (end > Rate / 20 && Mathf.Abs(b[end]) < 0.001f) end--;
                int len = Mathf.Min(b.Length, end + Rate / 50);
                Array.Resize(ref b, len);
                int fin = Mathf.Min(len, Rate / 1000);
                for (int i = 0; i < fin; i++) b[i] *= i / (float)fin;
                int fout = Mathf.Min(len, Rate / 50);
                for (int i = 0; i < fout; i++) b[len - 1 - i] *= i / (float)fout;
            }

            output = b;
            float finalLufs = ShortTermLoudness(b);
            return $"{b.Length / (float)Rate:0.00} s, {finalLufs:0.0} LUFS, pico {20f * Mathf.Log10(Mathf.Max(peak, 1e-6f)):0.0} dBFS";
        }

        /// <summary>
        /// Sonoridad tipo LUFS (BS.1770 simplificada): ponderación K (estante agudo +4 dB y paso alto 38 Hz),
        /// ventanas de 400 ms con salto de 100 ms; se devuelve la ventana más fuerte.
        /// </summary>
        static float ShortTermLoudness(float[] b)
        {
            var shelf = new Biquad(Biquad.HighShelf(1680f, 4f));
            var hp = new Biquad(Biquad.HP(38f));
            var sq = new double[b.Length];
            for (int i = 0; i < b.Length; i++) { float y = hp.Process(shelf.Process(b[i])); sq[i] = y * y; }
            int win = Mathf.Min(b.Length, (int)(0.4f * Rate)), hop = Mathf.Max(1, (int)(0.1f * Rate));
            double sum = 0; for (int i = 0; i < win; i++) sum += sq[i];
            double best = sum / win;
            for (int start = hop; start + win <= b.Length; start += hop)
            {
                sum = 0; for (int i = start; i < start + win; i++) sum += sq[i];
                best = Math.Max(best, sum / win);
            }
            return -0.691f + 10f * (float)Math.Log10(Math.Max(best, 1e-12));
        }

        // ================================================================ Archivos

        static void WriteWav(string name, float[] samples)
        {
            string path = Path.Combine(GameFeelAssetBuilder.AudioDir, name + ".wav");
            using (var stream = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(stream))
            {
                int dataBytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + dataBytes);
                w.Write(new[] { 'W', 'A', 'V', 'E' }); w.Write(new[] { 'f', 'm', 't', ' ' });
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(dataBytes);
                var rng = new System.Random(1);
                foreach (var s in samples)
                {
                    // Dither TPDF de 1 bit: evita distorsión en las colas muy suaves al pasar a 16 bits.
                    float d = (float)(rng.NextDouble() - rng.NextDouble()) / 32767f;
                    w.Write((short)Mathf.Clamp(Mathf.RoundToInt((s + d) * 32767f), -32768, 32767));
                }
            }
        }

        static void ConfigureImport()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { GameFeelAssetBuilder.AudioDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                bool longClip = clip != null && clip.length > 1.6f;
                importer.forceToMono = true;              // el visor espacializa: estéreo no aporta y gasta el doble
                importer.loadInBackground = longClip;
                var s = importer.defaultSampleSettings;
                // Cortos: se descomprimen al cargar (cero costo al reproducir, son pocos KB).
                // Largos: comprimidos en memoria (Meta recomienda no descomprimir al cargar lo que no es corto).
                s.loadType = longClip ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
                s.preloadAudioData = true;                // sin tirón la primera vez que suena
                s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = s;
                importer.SaveAndReimport();
            }
        }

        static void BuildBank(Dictionary<string, List<string>> variants)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BankPath));
            var bank = AssetDatabase.LoadAssetAtPath<FeelSoundBank>(BankPath);
            if (bank == null) { bank = ScriptableObject.CreateInstance<FeelSoundBank>(); AssetDatabase.CreateAsset(bank, BankPath); }
            bank.entries.Clear();
            foreach (var kv in variants)
            {
                AudioClip Load(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>(GameFeelAssetBuilder.AudioDir + "/" + n + ".wav");
                bank.entries.Add(new FeelSoundBank.Entry
                {
                    clip = Load(kv.Value[0]),
                    variants = kv.Value.Skip(1).Select(Load).Where(c => c != null).ToArray(),
                    volumeJitter = 0.1f
                });
            }
            EditorUtility.SetDirty(bank);
        }

        // ================================================================ Filtro biquad (RBJ)

        sealed class Biquad
        {
            float b0, b1, b2, a1, a2, z1, z2;
            public Biquad(float[] c) => Set(c);
            public void Set(float[] c) { b0 = c[0]; b1 = c[1]; b2 = c[2]; a1 = c[3]; a2 = c[4]; }
            public float Process(float x)
            {
                float y = b0 * x + z1;
                z1 = b1 * x - a1 * y + z2;
                z2 = b2 * x - a2 * y;
                return y;
            }

            static float[] Norm(double b0, double b1, double b2, double a0, double a1, double a2)
                => new[] { (float)(b0 / a0), (float)(b1 / a0), (float)(b2 / a0), (float)(a1 / a0), (float)(a2 / a0) };

            public static float[] LP(float f, float q = 0.7071f)
            {
                double w = 2 * Math.PI * Mathf.Min(f, Rate * 0.45f) / Rate, c = Math.Cos(w), a = Math.Sin(w) / (2 * q);
                return Norm((1 - c) / 2, 1 - c, (1 - c) / 2, 1 + a, -2 * c, 1 - a);
            }
            public static float[] HP(float f, float q = 0.7071f)
            {
                double w = 2 * Math.PI * f / Rate, c = Math.Cos(w), a = Math.Sin(w) / (2 * q);
                return Norm((1 + c) / 2, -(1 + c), (1 + c) / 2, 1 + a, -2 * c, 1 - a);
            }
            public static float[] BP(float f, float q)
            {
                double w = 2 * Math.PI * Mathf.Min(f, Rate * 0.45f) / Rate, c = Math.Cos(w), a = Math.Sin(w) / (2 * q);
                return Norm(a, 0, -a, 1 + a, -2 * c, 1 - a);
            }
            public static float[] HighShelf(float f, float gainDb)
            {
                double A = Math.Pow(10, gainDb / 40), w = 2 * Math.PI * f / Rate, c = Math.Cos(w), s = Math.Sin(w);
                double alpha = s / 2 * Math.Sqrt(2), sq = 2 * Math.Sqrt(A) * alpha;
                return Norm(A * ((A + 1) + (A - 1) * c + sq), -2 * A * ((A - 1) + (A + 1) * c), A * ((A + 1) + (A - 1) * c - sq),
                            (A + 1) - (A - 1) * c + sq, 2 * ((A - 1) - (A + 1) * c), (A + 1) - (A - 1) * c - sq);
            }
        }
    }
}

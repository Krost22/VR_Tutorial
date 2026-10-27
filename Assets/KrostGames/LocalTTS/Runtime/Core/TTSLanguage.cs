using System;

namespace KrostGames.TTS
{
    /// <summary>
    /// Language used to pick a voice when no voice name is set, or when the named voice is not installed.
    /// Any installed Windows voice can still be chosen by name, whatever its language.
    /// </summary>
    public enum TTSLanguage
    {
        English = 0,
        Spanish = 1,
        Portuguese = 2,
        French = 3,
        German = 4,
        Italian = 5,
        Japanese = 6,
        Chinese = 7,
        Korean = 8,
        Russian = 9
    }

    public static class TTSLanguageExtensions
    {
        static readonly string[] Prefixes = { "en", "es", "pt", "fr", "de", "it", "ja", "zh", "ko", "ru" };

        static readonly string[] Samples =
        {
            "Hello! This is Local TTS, speaking in real time.",
            "¡Hola! Soy Local TTS y hablo en tiempo real.",
            "Olá! Eu sou o Local TTS e falo em tempo real.",
            "Bonjour ! Je suis Local TTS et je parle en temps réel.",
            "Hallo! Ich bin Local TTS und spreche in Echtzeit.",
            "Ciao! Sono Local TTS e parlo in tempo reale.",
            "こんにちは。Local TTS がリアルタイムで話しています。",
            "你好！我是 Local TTS，正在实时说话。",
            "안녕하세요! Local TTS가 실시간으로 말하고 있어요.",
            "Привет! Это Local TTS, я говорю в реальном времени."
        };

        /// <summary>Two-letter ISO code ("en", "es"...), matched against voice cultures such as "es-MX".</summary>
        public static string CulturePrefix(this TTSLanguage language)
        {
            int i = (int)language;
            return i >= 0 && i < Prefixes.Length ? Prefixes[i] : "en";
        }

        /// <summary>Upper-case code used for output folders and the {lang} naming token ("EN", "ES"...).</summary>
        public static string Code(this TTSLanguage language) => language.CulturePrefix().ToUpperInvariant();

        /// <summary>A short sentence in this language, handy to test a voice.</summary>
        public static string SampleText(this TTSLanguage language)
        {
            int i = (int)language;
            return i >= 0 && i < Samples.Length ? Samples[i] : Samples[0];
        }

        /// <summary>Finds the language of a culture name such as "pt-BR". False for languages outside the enum.</summary>
        public static bool TryFromCulture(string culture, out TTSLanguage language)
        {
            language = TTSLanguage.English;
            if (string.IsNullOrEmpty(culture) || culture.Length < 2) return false;
            for (int i = 0; i < Prefixes.Length; i++)
            {
                if (culture.StartsWith(Prefixes[i], StringComparison.OrdinalIgnoreCase))
                {
                    language = (TTSLanguage)i;
                    return true;
                }
            }
            return false;
        }
    }
}

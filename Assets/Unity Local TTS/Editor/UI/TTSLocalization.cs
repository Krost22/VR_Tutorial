#if UNITY_EDITOR_WIN
namespace KrostGames.LocalTTS.Editor.UI
{
    public enum UILanguage { English, Spanish, Portuguese }

    public static class TTSLocalization
    {
        public static UILanguage Current = UILanguage.English;

        private static readonly string[][] Strings =
        {
            // English
            new[] {
                "Language", "Text Input", "Text to Speak:", "File Name (optional)",
                "Leave empty to auto-generate from text. No extension needed.",
                "Voice Settings", "Scan Installed Voices", "Refresh Voices",
                "Live Preview (Listen without generating)", "Stop",
                "Use Naming Template", "Generate Audio", "Generating...",
                "Auto-assign to selected GameObject",
                "Last Generated Clip", "Duration:", "Channels:", "Sample Rate:",
                "Ping in Project", "Preview (Editor Audio)",
                "Batch Generation", "Generate All Lines", "History",
                "Clear History", "Utilities", "Generated clips:", "Open Folder",
                "Clear All", "No voices found for", "Install a voice pack:",
                "UI Language", "Stop All Audio"
            },
            // Spanish
            new[] {
                "Idioma", "Entrada de Texto", "Texto a Hablar:", "Nombre de Archivo (opcional)",
                "Dejar vacío para auto-generar desde el texto. Sin extensión.",
                "Configuración de Voz", "Escanear Voces Instaladas", "Actualizar Voces",
                "Vista Previa en Vivo (Escuchar sin generar)", "Detener",
                "Usar Plantilla de Nombre", "Generar Audio", "Generando...",
                "Auto-asignar al GameObject seleccionado",
                "Último Clip Generado", "Duración:", "Canales:", "Frecuencia:",
                "Localizar en Proyecto", "Vista Previa (Audio Editor)",
                "Generación por Lotes", "Generar Todas las Líneas", "Historial",
                "Limpiar Historial", "Utilidades", "Clips generados:", "Abrir Carpeta",
                "Limpiar Todo", "No se encontraron voces para", "Instale un paquete de voz:",
                "Idioma de la UI", "Detener Todo el Audio"
            },
            // Portuguese
            new[] {
                "Idioma", "Entrada de Texto", "Texto para Falar:", "Nome do Arquivo (opcional)",
                "Deixe vazio para gerar automaticamente. Sem extensão.",
                "Configurações de Voz", "Escanear Vozes Instaladas", "Atualizar Vozes",
                "Pré-visualização Ao Vivo (Ouvir sem gerar)", "Parar",
                "Usar Modelo de Nome", "Gerar Áudio", "Gerando...",
                "Auto-atribuir ao GameObject selecionado",
                "Último Clip Gerado", "Duração:", "Canais:", "Taxa de Amostragem:",
                "Localizar no Projeto", "Pré-visualização (Áudio Editor)",
                "Geração em Lote", "Gerar Todas as Linhas", "Histórico",
                "Limpar Histórico", "Utilidades", "Clips gerados:", "Abrir Pasta",
                "Limpar Tudo", "Nenhuma voz encontrada para", "Instale um pacote de voz:",
                "Idioma da UI", "Parar Todo o Áudio"
            }
        };

        // Indices
        public const int Language = 0, TextInput = 1, TextToSpeak = 2, FileName = 3,
            FileNameHint = 4, VoiceSettings = 5, ScanVoices = 6, RefreshVoices = 7,
            LivePreview = 8, Stop = 9, UseNamingTemplate = 10, GenerateAudio = 11,
            Generating = 12, AutoAssign = 13, LastClip = 14, Duration = 15,
            Channels = 16, SampleRate = 17, PingProject = 18, PreviewAudio = 19,
            BatchGen = 20, GenAllLines = 21, History = 22, ClearHistory = 23,
            Utilities = 24, GenClips = 25, OpenFolder = 26, ClearAll = 27,
            NoVoices = 28, InstallVoice = 29, UILang = 30, StopAllAudio = 31;

        public static string Get(int index) => Strings[(int)Current][index];

        public static readonly string[] UILanguageLabels = { "🇺🇸 EN", "🇪🇸 ES", "🇧🇷 PT" };
    }
}
#endif

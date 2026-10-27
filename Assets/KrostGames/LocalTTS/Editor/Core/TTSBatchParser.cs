using System;
using System.Collections.Generic;
using System.Text;

namespace KrostGames.TTS.EditorTools
{
    /// <summary>Reads batch files: plain text (one clip per line) or CSV (text, optional file name).</summary>
    public static class TTSBatchParser
    {
        /// <summary>
        /// .txt: every non-empty line is one clip; lines starting with # or // are skipped.
        /// .csv: "text,file name" per row (file name optional). Quoted fields may contain commas, quotes ("") and line breaks.
        /// The separator can be a comma or a semicolon (Excel uses ";" in many locales). A "text,filename" header row is skipped.
        /// </summary>
        public static List<WindowsTTSEngine.BatchEntry> Parse(string content, bool csv)
        {
            var entries = new List<WindowsTTSEngine.BatchEntry>();
            if (string.IsNullOrEmpty(content)) return entries;
            content = content.TrimStart('﻿');

            if (!csv)
            {
                foreach (string raw in content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || IsComment(line)) continue;
                    entries.Add(new WindowsTTSEngine.BatchEntry(line));
                }
                return entries;
            }

            char separator = DetectSeparator(content);
            bool first = true;
            foreach (List<string> record in ReadCsv(content, separator))
            {
                string text = record.Count > 0 ? record[0].Trim() : "";
                string file = record.Count > 1 ? record[1].Trim() : null;
                bool header = first && IsHeader(text, file);
                first = false;
                if (header || text.Length == 0 || IsComment(text)) continue;
                entries.Add(new WindowsTTSEngine.BatchEntry(text, string.IsNullOrEmpty(file) ? null : file));
            }
            return entries;
        }

        static bool IsComment(string line) => line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal);

        static bool IsHeader(string text, string file)
        {
            string t = text.ToLowerInvariant();
            bool textHeader = t == "text" || t == "texto";
            string f = (file ?? "").ToLowerInvariant().Replace(" ", "");
            return textHeader && (file == null || f == "filename" || f == "file" || f == "name" || f == "archivo" || f == "nombre" || f == "nomedoarquivo" || f == "arquivo");
        }

        static char DetectSeparator(string content)
        {
            bool quoted = false;
            int commas = 0, semicolons = 0;
            foreach (char c in content)
            {
                if (c == '"') quoted = !quoted;
                else if (!quoted && (c == '\n' || c == '\r') && commas + semicolons > 0) break;
                else if (!quoted && c == ',') commas++;
                else if (!quoted && c == ';') semicolons++;
            }
            return semicolons > 0 && commas == 0 ? ';' : ',';
        }

        static IEnumerable<List<string>> ReadCsv(string content, char separator)
        {
            var record = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < content.Length && content[i + 1] == '"') { field.Append('"'); i++; }
                        else quoted = false;
                    }
                    else field.Append(c);
                    continue;
                }

                if (c == '"' && field.ToString().Trim().Length == 0) { field.Clear(); quoted = true; }
                else if (c == separator) { record.Add(field.ToString()); field.Clear(); }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n') i++;
                    record.Add(field.ToString());
                    field.Clear();
                    if (record.Count > 1 || record[0].Trim().Length > 0) yield return record;
                    record = new List<string>();
                }
                else field.Append(c);
            }

            record.Add(field.ToString());
            if (record.Count > 1 || record[0].Trim().Length > 0) yield return record;
        }
    }
}

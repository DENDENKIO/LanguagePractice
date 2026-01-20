using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LanguagePractice.Services
{
    public static class ImportTextNormalizer
    {
        // 全角ASCII（！〜～）と全角スペースを半角へ。日本語はそのまま。
        public static string ToHalfWidthAscii(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (c == '\u3000') { sb.Append(' '); continue; } // 全角スペース
                if (c >= '\uFF01' && c <= '\uFF5E') { sb.Append((char)(c - 0xFEE0)); continue; } // 全角ASCII
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static string RemoveInvisible(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s
                .Replace("\u200B", "") // zero-width space
                .Replace("\u200C", "")
                .Replace("\u200D", "")
                .Replace("\uFEFF", "");
        }

        public static string NormalizeNewlines(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        public static string TrimLineEnds(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var lines = s.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].TrimEnd();
            return string.Join("\n", lines);
        }

        // 空行が多すぎるのを抑える（3連続以上→2連続に）
        public static string CollapseBlankLines(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = NormalizeNewlines(s);
            return Regex.Replace(s, @"\n{3,}", "\n\n");
        }

        // KEY： の “：” を ":" に寄せ、KEY と ":" の間の空白も整える（キー検出安定化）
        public static string NormalizeKeyLines(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = NormalizeNewlines(s);

            // 行頭の KEY：/KEY :/KEY: を KEY: に統一（キーは大文字/数字/_）
            return Regex.Replace(
                s,
                @"(?m)^(?<k>[A-Z0-9_]+)\s*[：:]\s*",
                m => $"{m.Groups["k"].Value}: "
            );
        }

        public static string NormalizeAll(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = ToHalfWidthAscii(s);
            s = RemoveInvisible(s);
            s = NormalizeNewlines(s);
            s = TrimLineEnds(s);
            s = CollapseBlankLines(s);
            return s.Trim();
        }

        // ---- @<KEY>@ ... @</KEY>@ 形式 ----
        public static string WrapWithImportTags(Dictionary<string, string> keyValues)
        {
            var sb = new StringBuilder();
            foreach (var kv in keyValues)
            {
                var key = kv.Key?.Trim() ?? "";
                if (key.Length == 0) continue;

                var value = kv.Value ?? "";
                value = NormalizeAll(value);

                sb.Append("@<").Append(key).Append(">@\n");
                sb.Append(value);
                if (!value.EndsWith("\n")) sb.Append("\n");
                sb.Append("@</").Append(key).Append(">@\n\n");
            }
            return sb.ToString().TrimEnd();
        }

        public static Dictionary<string, string> ParseImportTags(string taggedText)
        {
            var dict = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(taggedText)) return dict;

            taggedText = NormalizeAll(taggedText);

            // @<KEY>@ ... @</KEY>@  (KEYは英数字と_)
            var rx = new Regex(@"@<(?<k>[A-Z0-9_]+)>@\s*(?<v>[\s\S]*?)\s*@</\k<k>>@", RegexOptions.Singleline);
            foreach (Match m in rx.Matches(taggedText))
            {
                var k = m.Groups["k"].Value.Trim();
                var v = m.Groups["v"].Value;
                v = NormalizeAll(v);

                if (!dict.ContainsKey(k)) dict[k] = v;
                else dict[k] += "\n" + v;
            }

            return dict;
        }

        // マーカー内の「KEY:」セクションを、空白に強い形で分割
        public static Dictionary<string, string> SplitKeySections(string markerContent)
        {
            var dict = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(markerContent)) return dict;

            var s = NormalizeAll(markerContent);
            s = NormalizeKeyLines(s);

            var lines = s.Split('\n');
            string? currentKey = null;
            var buf = new StringBuilder();

            void Flush()
            {
                if (currentKey == null) return;
                var value = buf.ToString().Trim('\n').Trim();
                value = NormalizeAll(value);
                dict[currentKey] = value;
                buf.Clear();
            }

            var keyLineRx = new Regex(@"^(?<k>[A-Z0-9_]+):\s*(?<rest>.*)$");

            foreach (var line in lines)
            {
                var m = keyLineRx.Match(line);
                if (m.Success)
                {
                    // 新しいキー
                    Flush();
                    currentKey = m.Groups["k"].Value.Trim();
                    var rest = m.Groups["rest"].Value;
                    if (!string.IsNullOrEmpty(rest))
                        buf.AppendLine(rest);
                }
                else
                {
                    // 継続行
                    buf.AppendLine(line);
                }
            }

            Flush();
            return dict;
        }
    }
}

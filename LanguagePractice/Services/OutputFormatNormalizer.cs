using LanguagePractice.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LanguagePractice.Services
{
    /// <summary>
    /// AI出力の表記ゆれを吸収し、DBに保存する文字列を統一フォーマットへ正規化する。
    /// 目的：後々のパース/検索/UI表示で崩れにくくする。
    /// </summary>
    public static class OutputFormatNormalizer
    {
        public static string NormalizeText(string s)
        {
            return ImportTextNormalizer.NormalizeAll(s ?? "");
        }

        public static string NormalizeCommaList(string s)
        {
            s = NormalizeText(s);
            if (string.IsNullOrWhiteSpace(s)) return "";

            // 全角カンマ等を半角に寄せる
            s = s.Replace("、", ",").Replace("，", ",").Replace("／", "/");

            var parts = s.Split(',')
                         .Select(x => x.Trim())
                         .Where(x => x.Length > 0)
                         .Distinct()
                         .ToList();

            return string.Join(", ", parts);
        }

        /// <summary>
        /// FIXブロック（複数行）を見やすい統一フォーマットへ。
        /// 入力例：
        /// FIX：
        /// PLACE：xxx
        /// TIME：yyy
        /// - WEATHER: zzz
        ///
        /// 出力（統一）：
        /// - PLACE: xxx
        /// - TIME: yyy
        /// - WEATHER: zzz
        /// </summary>
        public static string NormalizeFixConditions(string fixRaw)
        {
            fixRaw = NormalizeText(fixRaw);
            if (string.IsNullOrWhiteSpace(fixRaw)) return "";

            // 行ごとに処理（先頭の "FIX:" が混ざっていても削る）
            var lines = fixRaw.Split('\n')
                              .Select(l => l.Trim())
                              .Where(l => l.Length > 0)
                              .ToList();

            // 先頭が "FIX:" だけの行なら除去
            if (lines.Count > 0 && Regex.IsMatch(lines[0], @"^FIX\s*[:：]?\s*$", RegexOptions.IgnoreCase))
                lines.RemoveAt(0);

            // KEY抽出（ハイフン/記号ありの行も許容）
            // - PLACE: xxx
            // PLACE：xxx
            // ・PLACE: xxx
            var rx = new Regex(@"^(?:[-•*・]\s*)?(?<k>PLACE|TIME|WEATHER|LIGHT|SOUND|OBJECTS)\s*[：:]\s*(?<v>.*)$",
                               RegexOptions.IgnoreCase);

            // 固定順序
            string[] order = { "PLACE", "TIME", "WEATHER", "LIGHT", "SOUND", "OBJECTS" };
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var extra = new List<string>();

            foreach (var line in lines)
            {
                var m = rx.Match(line);
                if (m.Success)
                {
                    var k = m.Groups["k"].Value.ToUpperInvariant();
                    var v = m.Groups["v"].Value.Trim();
                    v = NormalizeText(v);

                    if (!string.IsNullOrWhiteSpace(v))
                        map[k] = v;
                    else if (!map.ContainsKey(k))
                        map[k] = "";
                }
                else
                {
                    extra.Add(line);
                }
            }

            var sb = new StringBuilder();

            foreach (var k in order)
            {
                if (map.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                {
                    sb.Append("- ").Append(k).Append(": ").Append(v).Append('\n');
                }
            }

            // 読めなかった行があれば NOTE に退避（情報ロスト防止）
            if (extra.Count > 0)
            {
                string note = NormalizeText(string.Join(" / ", extra));
                if (!string.IsNullOrWhiteSpace(note))
                {
                    sb.Append("- NOTE: ").Append(note).Append('\n');
                }
            }

            return sb.ToString().TrimEnd();
        }

        public static Topic NormalizeTopic(Topic t)
        {
            t.Title = NormalizeText(t.Title);
            t.Emotion = NormalizeCommaList(t.Emotion);
            t.Scene = NormalizeText(t.Scene).ToUpperInvariant();
            t.Tags = NormalizeCommaList(t.Tags);
            t.FixConditions = NormalizeFixConditions(t.FixConditions);
            t.CreatedAt = NormalizeText(t.CreatedAt);
            return t;
        }

        public static Work NormalizeWork(Work w)
        {
            w.Kind = NormalizeText(w.Kind);
            w.Title = NormalizeText(w.Title);
            w.BodyText = NormalizeText(w.BodyText);
            w.WriterName = NormalizeText(w.WriterName);
            w.ReaderNote = NormalizeText(w.ReaderNote);
            w.ToneLabel = NormalizeText(w.ToneLabel);
            w.CreatedAt = NormalizeText(w.CreatedAt);
            return w;
        }

        public static StudyCard NormalizeStudyCard(StudyCard c)
        {
            c.Focus = NormalizeText(c.Focus);
            c.Level = NormalizeText(c.Level).ToUpperInvariant();
            c.BestExpressionsRaw = NormalizeText(c.BestExpressionsRaw);
            c.MetaphorChainsRaw = NormalizeText(c.MetaphorChainsRaw);
            c.DoNextRaw = NormalizeText(c.DoNextRaw);
            c.Tags = NormalizeCommaList(c.Tags);
            c.FullParsedContent = NormalizeText(c.FullParsedContent);
            c.CreatedAt = NormalizeText(c.CreatedAt);
            return c;
        }

        public static Observation NormalizeObservation(Observation o)
        {
            o.ImageUrl = NormalizeText(o.ImageUrl);
            o.Motif = NormalizeText(o.Motif);
            o.VisualRaw = NormalizeText(o.VisualRaw);
            o.SoundRaw = NormalizeText(o.SoundRaw);
            o.MetaphorsRaw = NormalizeText(o.MetaphorsRaw);
            o.CoreCandidatesRaw = NormalizeText(o.CoreCandidatesRaw);
            o.FullContent = NormalizeText(o.FullContent);
            o.CreatedAt = NormalizeText(o.CreatedAt);
            return o;
        }
    }
}

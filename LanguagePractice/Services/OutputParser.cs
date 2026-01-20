using LanguagePractice.Helpers;
using LanguagePractice.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LanguagePractice.Services
{
    public partial class OutputParser
    {
        // ==========================================
        // 汎用ヘルパー: マーカー位置検索
        // ==========================================
        private static int FindMarkerIndex(string text, string marker)
        {
            return text.LastIndexOf(marker, StringComparison.Ordinal);
        }

        // ==========================================
        // 汎用ヘルパー: マーカー範囲抽出
        // ==========================================
        private static string ExtractMarkerRange(string rawOutput, string beginMarker, string endMarker)
        {
            int start = FindMarkerIndex(rawOutput, beginMarker);
            int end = FindMarkerIndex(rawOutput, endMarker);

            if (start == -1 || end == -1 || start >= end)
                return "";

            int contentStart = start + beginMarker.Length;
            int length = end - contentStart;
            return rawOutput.Substring(contentStart, length).Trim();
        }

        // ==========================================
        // 改行崩れに強い KEY:VALUE パース（キー位置スライス）
        // ==========================================
        private static Dictionary<string, string> ParseKeyValueBlock(string block)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(block)) return result;

            string s = ImportTextNormalizer.NormalizeAll(block);

            var rx = new Regex(@"(?<k>[A-Z][A-Z0-9_]{1,40})\s*[：:]\s*", RegexOptions.IgnoreCase);
            var matches = rx.Matches(s);
            if (matches.Count == 0) return result;

            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                string key = m.Groups["k"].Value.Trim().ToUpperInvariant();

                int valueStart = m.Index + m.Length;
                int valueEnd = (i + 1 < matches.Count) ? matches[i + 1].Index : s.Length;
                if (valueEnd < valueStart) continue;

                string value = s.Substring(valueStart, valueEnd - valueStart).Trim();

                if (result.ContainsKey(key))
                    result[key] += "\n" + value;
                else
                    result[key] = value;
            }

            FixShiftedFields(result);
            return result;
        }

        // 「READERにTOPICが飲まれる」等の軽補正
        private static void FixShiftedFields(Dictionary<string, string> dict)
        {
            if (dict.TryGetValue("READER", out var readerVal))
            {
                var mTopic = Regex.Match(readerVal, @"^\s*TOPIC\s*[：:]\s*(?<v>[\s\S]+)$", RegexOptions.IgnoreCase);
                if (mTopic.Success)
                {
                    if (!dict.ContainsKey("TOPIC") || string.IsNullOrWhiteSpace(dict.GetValueOrDefault("TOPIC", "")))
                        dict["TOPIC"] = mTopic.Groups["v"].Value.Trim();

                    dict["READER"] = "";
                }
            }
        }

        // ==========================================
        // TOPICブロックから FIX セクションを丸ごと抽出
        // ==========================================
        private static string ExtractFixSection(string topicBlock)
        {
            if (string.IsNullOrWhiteSpace(topicBlock)) return "";

            string s = ImportTextNormalizer.NormalizeAll(topicBlock);

            // FIX行の開始（FIX: / FIX：）
            var m = Regex.Match(s, @"(?im)^\s*FIX\s*[：:]\s*(?<rest>.*)$");
            if (!m.Success) return "";

            string rest = (m.Groups["rest"].Value ?? "").Trim();

            // FIX行の次行から
            int nl = s.IndexOf('\n', m.Index);
            int afterLineStart = (nl == -1) ? (m.Index + m.Length) : (nl + 1);

            string tail = afterLineStart <= s.Length ? s.Substring(afterLineStart) : "";

            // 終端（@@@TOPIC_END@@@ まで）
            int end = tail.Length;
            int end1 = IndexOfRegex(tail, @"(?im)^\s*@@@TOPIC_END@@@\s*$");
            if (end1 != -1) end = Math.Min(end, end1);

            string body = (end > 0 ? tail.Substring(0, end) : "").Trim();

            // FIX: が同一行で内容を持つ形式も吸収
            if (!string.IsNullOrWhiteSpace(rest))
            {
                if (string.IsNullOrWhiteSpace(body)) return rest;
                return (rest + "\n" + body).Trim();
            }

            return body;
        }

        private static int IndexOfRegex(string s, string pattern)
        {
            var m = Regex.Match(s, pattern);
            return m.Success ? m.Index : -1;
        }

        // ==========================================
        // 旧互換用: 辞書版Parse（単一エンティティ用）
        // ==========================================
        public static Dictionary<string, string> Parse(string rawOutput, OperationKind opKind)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(rawOutput)) return result;

            string beginMarker = LpConstants.MarkerBegin[opKind];
            string endMarker = LpConstants.MarkerEnd[opKind];

            string content = ExtractMarkerRange(rawOutput, beginMarker, endMarker);
            if (string.IsNullOrEmpty(content)) return result;

            return ParseKeyValueBlock(content);
        }

        // ==========================================
        // TEXT_GEN → Work リスト解析
        // ==========================================
        public static List<Work> ParseWorks(string rawOutput, long runLogId = 0)
        {
            var list = new List<Work>();
            if (string.IsNullOrWhiteSpace(rawOutput)) return list;

            string packBegin = LpConstants.MarkerBegin[OperationKind.TEXT_GEN];
            string packEnd = LpConstants.MarkerEnd[OperationKind.TEXT_GEN];

            string targetArea = ExtractMarkerRange(rawOutput, packBegin, packEnd);
            if (string.IsNullOrEmpty(targetArea))
                targetArea = rawOutput;

            var blocks = Regex.Split(targetArea, @"@@@\s*WORK\s*\|\s*\d+\s*@@@", RegexOptions.IgnoreCase);

            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;
                if (!Regex.IsMatch(block, @"(TEXT|GIKO_TEXT|REVISED_TEXT)\s*[：:]", RegexOptions.IgnoreCase))
                    continue;

                var dict = ParseKeyValueBlock(block);
                if (dict.Count == 0) continue;

                string body =
                    dict.GetValueOrDefault("TEXT", "") +
                    dict.GetValueOrDefault("GIKO_TEXT", "") +
                    dict.GetValueOrDefault("REVISED_TEXT", "");

                var work = new Work
                {
                    RunLogId = runLogId,
                    Kind = WorkKind.TEXT_GEN.ToString(),
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Title = dict.GetValueOrDefault("TOPIC", "無題"),
                    BodyText = body,
                    WriterName = dict.GetValueOrDefault("WRITER", ""),
                    ReaderNote = dict.GetValueOrDefault("READER", ""),
                    ToneLabel = dict.GetValueOrDefault("TONE", "なし")
                };

                work = OutputFormatNormalizer.NormalizeWork(work);

                if (!string.IsNullOrWhiteSpace(work.BodyText))
                    list.Add(work);
            }

            // 区切りがない場合は1件として扱う
            if (list.Count == 0)
            {
                var dict = ParseKeyValueBlock(targetArea);
                string body =
                    dict.GetValueOrDefault("TEXT", "") +
                    dict.GetValueOrDefault("GIKO_TEXT", "") +
                    dict.GetValueOrDefault("REVISED_TEXT", "");

                if (dict.Count > 0 && !string.IsNullOrWhiteSpace(body))
                {
                    var w = new Work
                    {
                        RunLogId = runLogId,
                        Kind = WorkKind.TEXT_GEN.ToString(),
                        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Title = dict.GetValueOrDefault("TOPIC", "無題"),
                        BodyText = body,
                        WriterName = dict.GetValueOrDefault("WRITER", ""),
                        ReaderNote = dict.GetValueOrDefault("READER", ""),
                        ToneLabel = dict.GetValueOrDefault("TONE", "なし")
                    };
                    list.Add(OutputFormatNormalizer.NormalizeWork(w));
                }
            }

            return list;
        }

        // ==========================================
        // STUDY_CARD → StudyCard リスト解析
        // ==========================================
        public static List<StudyCard> ParseStudyCards(string rawOutput, long? sourceWorkId = null)
        {
            var list = new List<StudyCard>();
            if (string.IsNullOrWhiteSpace(rawOutput)) return list;

            string packBegin = LpConstants.MarkerBegin[OperationKind.STUDY_CARD];
            string packEnd = LpConstants.MarkerEnd[OperationKind.STUDY_CARD];

            string targetArea = ExtractMarkerRange(rawOutput, packBegin, packEnd);
            if (string.IsNullOrEmpty(targetArea))
                targetArea = rawOutput;

            var blocks = Regex.Split(targetArea, @"@@@\s*CARD\s*\|\s*\d+\s*@@@", RegexOptions.IgnoreCase);

            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;
                if (!Regex.IsMatch(block, @"FOCUS\s*[：:]", RegexOptions.IgnoreCase))
                    continue;

                var dict = ParseKeyValueBlock(block);
                if (dict.Count == 0) continue;

                var card = new StudyCard
                {
                    SourceWorkId = sourceWorkId,
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Focus = dict.GetValueOrDefault("FOCUS", "解析結果参照"),
                    Level = dict.GetValueOrDefault("LEVEL", "NORMAL"),
                    BestExpressionsRaw = dict.GetValueOrDefault("BEST_EXPRESSIONS", ""),
                    MetaphorChainsRaw = dict.GetValueOrDefault("METAPHOR_CHAINS", ""),
                    DoNextRaw = dict.GetValueOrDefault("DO_NEXT", ""),
                    Tags = dict.GetValueOrDefault("TAGS", ""),
                    FullParsedContent = string.Join("\n\n", dict.Select(kv => $"{kv.Key}: {kv.Value}"))
                };

                list.Add(OutputFormatNormalizer.NormalizeStudyCard(card));
            }

            return list;
        }

        // ==========================================
        // Persona解析
        // ==========================================
        public static List<Persona> ParsePersonas(string rawOutput)
        {
            var list = new List<Persona>();
            if (string.IsNullOrWhiteSpace(rawOutput)) return list;

            string packBegin = LpConstants.MarkerBegin[OperationKind.PERSONA_GEN];
            string packEnd = LpConstants.MarkerEnd[OperationKind.PERSONA_GEN];

            string targetArea = ExtractMarkerRange(rawOutput, packBegin, packEnd);
            if (string.IsNullOrEmpty(targetArea))
                targetArea = rawOutput;

            var blocks = Regex.Split(targetArea, @"@@@\s*PERSONA\s*\|\s*\d+\s*@@@", RegexOptions.IgnoreCase);

            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;
                if (!Regex.IsMatch(block, @"NAME\s*[：:]", RegexOptions.IgnoreCase))
                    continue;

                var dict = ParseKeyValueBlock(block);
                if (dict.Count == 0) continue;

                var p = new Persona
                {
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    VerificationStatus = VerificationStatus.UNVERIFIED.ToString(),
                    Name = OutputFormatNormalizer.NormalizeText(dict.GetValueOrDefault("NAME", "")),
                    Location = OutputFormatNormalizer.NormalizeText(dict.GetValueOrDefault("LOCATION", "")),
                    Bio = OutputFormatNormalizer.NormalizeText(dict.GetValueOrDefault("BIO", "")),
                    Style = OutputFormatNormalizer.NormalizeText(dict.GetValueOrDefault("STYLE", "")),
                    Tags = OutputFormatNormalizer.NormalizeCommaList(dict.GetValueOrDefault("TAGS", ""))
                };

                if (!string.IsNullOrWhiteSpace(p.Name))
                    list.Add(p);
            }

            return list;
        }

        // ==========================================
        // ★Topic解析（FIXは範囲抽出 → 正規化して保存）
        // ==========================================
        public static List<Topic> ParseTopics(string rawOutput)
        {
            var list = new List<Topic>();
            if (string.IsNullOrWhiteSpace(rawOutput)) return list;

            string packBegin = LpConstants.MarkerBegin[OperationKind.TOPIC_GEN];
            string packEnd = LpConstants.MarkerEnd[OperationKind.TOPIC_GEN];

            string targetArea = ExtractMarkerRange(rawOutput, packBegin, packEnd);
            if (string.IsNullOrEmpty(targetArea))
                targetArea = rawOutput;

            var blocks = Regex.Split(targetArea, @"@@@\s*TOPIC\s*\|\s*\d+\s*@@@", RegexOptions.IgnoreCase);

            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;
                if (!Regex.IsMatch(block, @"TITLE\s*[：:]", RegexOptions.IgnoreCase))
                    continue;

                var dict = ParseKeyValueBlock(block);
                if (dict.Count == 0) continue;

                string fixRaw = ExtractFixSection(block);
                if (string.IsNullOrWhiteSpace(fixRaw))
                    fixRaw = dict.GetValueOrDefault("FIX", "");

                var t = new Topic
                {
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Title = dict.GetValueOrDefault("TITLE", ""),
                    Emotion = dict.GetValueOrDefault("EMOTION", ""),
                    Scene = dict.GetValueOrDefault("SCENE", ""),
                    Tags = dict.GetValueOrDefault("TAGS", ""),
                    FixConditions = fixRaw
                };

                list.Add(OutputFormatNormalizer.NormalizeTopic(t));
            }

            return list;
        }

        // ==========================================
        // 旧互換: 辞書からWork生成（RunPanelで使用）
        // ==========================================
        public static Work CreateWorkFromDict(Dictionary<string, string> dict, long runLogId)
        {
            var w = new Work
            {
                RunLogId = runLogId,
                Kind = WorkKind.TEXT_GEN.ToString(),
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Title = dict.GetValueOrDefault("TOPIC", "無題"),
                BodyText = dict.GetValueOrDefault("TEXT", "") +
                           dict.GetValueOrDefault("GIKO_TEXT", "") +
                           dict.GetValueOrDefault("REVISED_TEXT", ""),
                WriterName = dict.GetValueOrDefault("WRITER", ""),
                ReaderNote = dict.GetValueOrDefault("READER", ""),
                ToneLabel = dict.GetValueOrDefault("TONE", "なし")
            };
            return OutputFormatNormalizer.NormalizeWork(w);
        }

        // ==========================================
        // 旧互換: 辞書からStudyCard生成
        // ==========================================
        public static StudyCard CreateStudyCardFromDict(Dictionary<string, string> dict)
        {
            var c = new StudyCard
            {
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Focus = dict.GetValueOrDefault("FOCUS", "解析結果参照"),
                Level = dict.GetValueOrDefault("LEVEL", "NORMAL"),
                BestExpressionsRaw = dict.GetValueOrDefault("BEST_EXPRESSIONS", ""),
                MetaphorChainsRaw = dict.GetValueOrDefault("METAPHOR_CHAINS", ""),
                DoNextRaw = dict.GetValueOrDefault("DO_NEXT", ""),
                Tags = dict.GetValueOrDefault("TAGS", ""),
                FullParsedContent = string.Join("\n\n", dict.Select(kv => $"{kv.Key}: {kv.Value}"))
            };
            return OutputFormatNormalizer.NormalizeStudyCard(c);
        }

        // ==========================================
        // Observation解析（RunPanelで使用）
        // ==========================================
        public static Observation CreateObservationFromDict(Dictionary<string, string> dict)
        {
            var o = new Observation
            {
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Motif = dict.GetValueOrDefault("IMAGE_MOTIF", ""),
                VisualRaw = dict.GetValueOrDefault("VISUAL", ""),
                SoundRaw = dict.GetValueOrDefault("SOUND", ""),
                MetaphorsRaw = dict.GetValueOrDefault("METAPHORS", ""),
                CoreCandidatesRaw = dict.GetValueOrDefault("CORE_SENTENCE_CANDIDATES", ""),
                FullContent = string.Join("\n\n", dict.Select(kv => $"{kv.Key}: {kv.Value}"))
            };
            return OutputFormatNormalizer.NormalizeObservation(o);
        }

        // ==========================================
        // PersonaVerification解析（あなたの既存仕様を維持）
        // ==========================================
        public static PersonaVerification ParseVerificationResult(string rawOutput, long personaId, string e1, string e2, string e3)
        {
            var result = new PersonaVerification
            {
                PersonaId = personaId,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Evidence1 = e1,
                Evidence2 = e2,
                Evidence3 = e3,
                ResultJson = rawOutput,
                OverallVerdict = "UNCLEAR",
                RevisedBioDraft = ""
            };

            string beginMarker = LpConstants.MarkerBegin[OperationKind.PERSONA_VERIFY_ASSIST];
            string endMarker = LpConstants.MarkerEnd[OperationKind.PERSONA_VERIFY_ASSIST];

            string content = ExtractMarkerRange(rawOutput, beginMarker, endMarker);
            if (!string.IsNullOrEmpty(content))
            {
                var bioMatch = Regex.Match(content, @"REVISED_PERSONA_DRAFT\s*[：:]\s*BIO\s*[：:]\s*([\s\S]*?)(?=STATUS_RECOMMENDATION|$)", RegexOptions.IgnoreCase);
                if (bioMatch.Success)
                    result.RevisedBioDraft = bioMatch.Groups[1].Value.Trim();

                var statusMatch = Regex.Match(content, @"SUGGESTED_STATUS\s*[：:]\s*([A-Z_]+)", RegexOptions.IgnoreCase);
                if (statusMatch.Success)
                    result.OverallVerdict = statusMatch.Groups[1].Value.Trim();
            }

            return result;
        }
    }
}

using LanguagePractice.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LanguagePractice.Services
{
    /// <summary>
    /// MindsetLab エクスポートサービス
    /// 仕様書2 第4.5章準拠
    /// </summary>
    public class MindsetExportService
    {
        private readonly MindsetDatabaseService _db;

        private static readonly string ExportFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LanguagePractice", "MindsetLabExports");

        public MindsetExportService(MindsetDatabaseService db)
        {
            _db = db;

            if (!Directory.Exists(ExportFolder))
            {
                Directory.CreateDirectory(ExportFolder);
            }
        }

        /// <summary>
        /// 日単位のセッションをエクスポート
        /// </summary>
        public string ExportDay(int dayId)
        {
            var day = _db.GetDay(dayId);
            if (day == null) throw new InvalidOperationException("Day not found");

            var entries = _db.GetEntriesByDay(dayId);
            var review = _db.GetReviewByDay(dayId);

            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  MindsetLab Daily Report - {day.DateKey}");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();

            // 重点マインドセット
            sb.AppendLine("【今日の重点マインドセット】");
            var focusList = day.GetFocusMindsetList();
            foreach (var f in focusList)
            {
                sb.AppendLine($"  {f}. {MindsetDefinitions.GetMindsetName(f)}");
            }
            sb.AppendLine();

            // 儀式
            if (!string.IsNullOrEmpty(day.StartRitual))
            {
                sb.AppendLine("【開始儀式】");
                sb.AppendLine($"  {day.StartRitual}");
                sb.AppendLine();
            }

            // ドリル記録
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine("  【ドリル記録】");
            sb.AppendLine("───────────────────────────────────────────────────────────────");

            var groupedEntries = new Dictionary<int, List<MsEntry>>();
            foreach (var entry in entries)
            {
                int mindsetId = GetMindsetIdFromEntryType(entry.EntryType);
                if (!groupedEntries.ContainsKey(mindsetId))
                    groupedEntries[mindsetId] = new List<MsEntry>();
                groupedEntries[mindsetId].Add(entry);
            }

            foreach (var kvp in groupedEntries)
            {
                var mindsetName = MindsetDefinitions.GetMindsetName(kvp.Key);
                sb.AppendLine();
                sb.AppendLine($"▼ Mindset {kvp.Key}: {mindsetName}");
                sb.AppendLine();

                foreach (var entry in kvp.Value)
                {
                    var drillTitle = GetDrillTitle(entry.EntryType);
                    sb.AppendLine($"  [{entry.EntryType}] {drillTitle}");
                    sb.AppendLine($"  {entry.BodyText}");
                    sb.AppendLine();
                }
            }

            // レビュー
            if (review != null)
            {
                sb.AppendLine("───────────────────────────────────────────────────────────────");
                sb.AppendLine("  【AIレビュー】");
                sb.AppendLine("───────────────────────────────────────────────────────────────");
                sb.AppendLine();
                sb.AppendLine($"  総合スコア: {review.TotalScore}/100");
                sb.AppendLine();

                if (!string.IsNullOrEmpty(review.SubscoresJson))
                {
                    sb.AppendLine("  【詳細スコア】");
                    sb.AppendLine($"    {review.SubscoresJson}");
                    sb.AppendLine();
                }

                if (!string.IsNullOrEmpty(review.Strengths))
                {
                    sb.AppendLine("  【強み】");
                    sb.AppendLine($"    {review.Strengths}");
                    sb.AppendLine();
                }

                if (!string.IsNullOrEmpty(review.Weaknesses))
                {
                    sb.AppendLine("  【弱み・改善点】");
                    sb.AppendLine($"    {review.Weaknesses}");
                    sb.AppendLine();
                }

                if (!string.IsNullOrEmpty(review.NextDayPlan))
                {
                    sb.AppendLine("  【明日の課題】");
                    sb.AppendLine($"    {review.NextDayPlan}");
                    sb.AppendLine();
                }

                if (!string.IsNullOrEmpty(review.CoreLink))
                {
                    sb.AppendLine("  【核候補（PoetryLab接続用）】");
                    sb.AppendLine($"    {review.CoreLink}");
                    sb.AppendLine();
                }
            }

            // 終了儀式
            if (!string.IsNullOrEmpty(day.EndRitual))
            {
                sb.AppendLine("【終了儀式】");
                sb.AppendLine($"  {day.EndRitual}");
                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            // ファイル保存
            var fileName = $"MindsetLab_{day.DateKey.Replace("-", "")}.txt";
            var filePath = Path.Combine(ExportFolder, fileName);
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            // ログ記録
            _db.CreateExportLog(dayId, filePath);

            return filePath;
        }

        private int GetMindsetIdFromEntryType(string entryType)
        {
            if (entryType.StartsWith("A")) return 1;
            if (entryType.StartsWith("B")) return 2;
            if (entryType.StartsWith("C")) return 3;
            if (entryType.StartsWith("D")) return 4;
            if (entryType.StartsWith("E")) return 5;
            if (entryType.StartsWith("F")) return 6;
            return 0;
        }

        private string GetDrillTitle(string entryType)
        {
            foreach (var m in MindsetDefinitions.All.Values)
            {
                foreach (var d in m.Drills)
                {
                    if (d.EntryType == entryType) return d.Title;
                }
            }
            return entryType;
        }
    }
}

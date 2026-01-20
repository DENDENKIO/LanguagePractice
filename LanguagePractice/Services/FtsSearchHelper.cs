using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Dapper;

namespace LanguagePractice.Services
{
    public static class FtsSearchHelper
    {
        /// <summary>
        /// - 全角ASCII→半角ASCII（ImportTextNormalizer）
        /// - 不可視除去
        /// - trim
        /// - 英語は大小無視のため lower 化
        /// </summary>
        public static string NormalizeQuery(string s)
        {
            s ??= "";
            s = ImportTextNormalizer.ToHalfWidthAscii(s);
            s = ImportTextNormalizer.RemoveInvisible(s);
            s = s.Trim();
            s = s.ToLowerInvariant();
            return s;
        }

        /// <summary>
        /// "a@b@c" => ["a","b","c"]（空は除外）
        /// </summary>
        public static List<string> SplitAtForAnd(string normalized)
        {
            return (normalized ?? "")
                .Split('@', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();
        }

        /// <summary>
        /// @区切りを AND で組む。各トークンは phrase 扱いでクォート。
        /// 例: ["red","coffee"] => "\"red\" AND \"coffee\""
        /// </summary>
        public static string BuildAndMatchQuery(IEnumerable<string> terms)
        {
            var qs = terms
                .Select(EscapeAsPhrase)
                .Where(x => x.Length > 0)
                .ToList();

            return string.Join(" AND ", qs);
        }

        /// <summary>
        /// FTS5 phrase: "..." （内部の " は ""）
        /// </summary>
        private static string EscapeAsPhrase(string term)
        {
            term ??= "";
            term = term.Replace("\"", "\"\"");
            return $"\"{term}\"";
        }

        public static bool HasFtsTable(SqliteConnection conn, string ftsTable)
        {
            return conn.ExecuteScalar<long>(
                "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=@Name;",
                new { Name = ftsTable }
            ) > 0;
        }
    }
}

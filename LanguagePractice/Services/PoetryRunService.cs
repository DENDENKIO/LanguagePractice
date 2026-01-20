using LanguagePractice.Models;
using System.Collections.Generic;

namespace LanguagePractice.Services
{
    /// <summary>
    /// PoetryLab Run管理サービス
    /// </summary>
    public class PoetryRunService
    {
        private readonly PoetryDatabaseService _db;

        public PoetryRunService(PoetryDatabaseService db)
        {
            _db = db;
        }

        /// <summary>
        /// 新規Run作成
        /// </summary>
        public PlRun Create(int projectId, string routeName = "標準Run")
        {
            var id = _db.CreateRun(projectId, routeName);
            return _db.GetRun(id)!;
        }

        /// <summary>
        /// Run取得
        /// </summary>
        public PlRun? Get(int id)
        {
            return _db.GetRun(id);
        }

        /// <summary>
        /// プロジェクトのRun一覧取得
        /// </summary>
        public List<PlRun> GetByProject(int projectId)
        {
            return _db.GetRunsByProject(projectId);
        }

        /// <summary>
        /// Run完了
        /// </summary>
        public void Complete(int id)
        {
            _db.UpdateRunStatus(id, "SUCCESS");
        }

        /// <summary>
        /// Run失敗
        /// </summary>
        public void Fail(int id)
        {
            _db.UpdateRunStatus(id, "FAILED");
        }

        /// <summary>
        /// Runキャンセル
        /// </summary>
        public void Cancel(int id)
        {
            _db.UpdateRunStatus(id, "CANCELLED");
        }

        /// <summary>
        /// Runの成果物一覧取得
        /// </summary>
        public List<PlTextAsset> GetAssets(int runId)
        {
            return _db.GetTextAssetsByRun(runId);
        }

        /// <summary>
        /// 特定タイプの成果物取得
        /// </summary>
        public PlTextAsset? GetAsset(int runId, string assetType)
        {
            return _db.GetTextAssetByType(runId, assetType);
        }

        /// <summary>
        /// RunのIssue一覧取得
        /// </summary>
        public List<PlIssue> GetIssues(int runId, string[]? severities = null)
        {
            return _db.GetIssuesByRun(runId, severities);
        }

        /// <summary>
        /// Runの比較結果取得
        /// </summary>
        public PlCompare? GetCompare(int runId)
        {
            return _db.GetCompareByRun(runId);
        }

        /// <summary>
        /// RunのStepLog一覧取得
        /// </summary>
        public List<PlAiStepLog> GetStepLogs(int runId)
        {
            return _db.GetAiStepLogsByRun(runId);
        }
    }
}

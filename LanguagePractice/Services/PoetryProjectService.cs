using LanguagePractice.Models;
using System.Collections.Generic;

namespace LanguagePractice.Services
{
    /// <summary>
    /// PoetryLab プロジェクト管理サービス
    /// </summary>
    public class PoetryProjectService
    {
        private readonly PoetryDatabaseService _db;

        public PoetryProjectService(PoetryDatabaseService db)
        {
            _db = db;
        }

        /// <summary>
        /// 新規プロジェクト作成
        /// </summary>
        public PlProject Create(string title, string styleType = "KOU")
        {
            var id = _db.CreateProject(title, styleType);
            return _db.GetProject(id)!;
        }

        /// <summary>
        /// プロジェクト取得
        /// </summary>
        public PlProject? Get(int id)
        {
            return _db.GetProject(id);
        }

        /// <summary>
        /// 全プロジェクト取得（更新日時降順）
        /// </summary>
        public List<PlProject> GetAll()
        {
            return _db.GetAllProjects();
        }

        /// <summary>
        /// プロジェクト更新
        /// </summary>
        public void Update(int id, string title, string styleType)
        {
            _db.UpdateProject(id, title, styleType);
        }

        /// <summary>
        /// プロジェクト削除（関連データも削除）
        /// </summary>
        public void Delete(int id)
        {
            _db.DeleteProject(id);
        }
    }
}

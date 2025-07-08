using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Utils;

namespace Kuantech.Core.Database
{
    public class KtDatabaseManager : SubManager
    {
        public List<KtDatabase> Databases;
        
        private Dictionary<string, KtDatabase> _databases = new Dictionary<string, KtDatabase>();

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            _databases = new Dictionary<string, KtDatabase>();
            foreach (var db in Databases)
            {
                await db.Initialize();
                _databases[db.GetDbName()] = db;
            }
        }

        #region Getters
        public static KtDatabase GetDatabase(string database)
        {
            var ctx = GetContext<KtDatabaseManager>();
            if (ctx == null) return null;
            if (database == null || ctx._databases.IsNullOrEmpty() || !ctx._databases.ContainsKey(database)) return null;
            return ctx._databases[database];
        }
        
        public bool GetData<T>(string dbName, string table, string row, string column, out T result)
        {
            result = default;
            KtDatabase db = GetDatabase(dbName);
            if (db == null) return false;
            return db.GetValue(table, row, column, out result);
        }
        #endregion
    }
}
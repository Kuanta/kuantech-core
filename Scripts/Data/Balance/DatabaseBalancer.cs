using Kuantech.Core.Database;

namespace Kuantech.Core.Data
{
    public class DatabaseBalancer : Balancer
    {
        public string DatabaseName;
        public string TableName;

        public override void Balance()
        {
            KtDatabase db = KtDatabaseManager.GetDatabase(DatabaseName);
            if(db == null) return;
            BalanceFromDb(db, TableName);
        }

        protected virtual void BalanceFromDb(KtDatabase db, string tableName)
        {
            
        }
    }
}
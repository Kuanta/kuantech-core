using System.Collections.Generic;
using Kuantech.Core.Data;

namespace Kuantech.Core.Database
{
    public class ActorBlueprintBalancer : DatabaseBalancer
    {
        public List<ActorBlueprint> Blueprints = new List<ActorBlueprint>();
        protected override void BalanceFromDb(KtDatabase db, string tableName)
        {
            DataTable dt = db.GetDataTable(tableName);
            if (dt == null) return;
            foreach (var bp in Blueprints)
            {
                bp.UpdateFromDatabaseTable(dt);
            }
        }
    }
}
using System.Collections.Generic;

namespace Kuantech.Core.Database
{
    public class ActorBlueprintBalancer : Balancer
    {
        public List<ActorBlueprint> Blueprints = new List<ActorBlueprint>();
        public override void Balance(KtDatabase db, string tableName)
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
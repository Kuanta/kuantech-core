using Kuantech.TowerDefense;

namespace Kuantech.Core.Database
{
    public class SpawnablesBalancer : Balancer
    {
        public SpawnablesCollection Collection;
        public string SpawnWeightColumnName = "SpawnWeight";
        public string MinDifficultyColumnName = "MinDifficulty";
        public string MaxDifficultyColumnName = "MaxDifficulty";
        public override void Balance(KtDatabase db, string tableName)
        {
            DataTable dt = db.GetDataTable(tableName);
            if (dt == null) return;
            foreach (var spawnable in Collection.Spawnables)
            {
                string Id = spawnable.ActorBlueprint.GetId();
                var row = dt.GetRow(Id);
                if(row == null) continue;
                spawnable.MinDifficultyLevel = row.GetIntValue(MinDifficultyColumnName, spawnable.MinDifficultyLevel);
                spawnable.MaxDifficultyLevel = row.GetIntValue(MaxDifficultyColumnName, spawnable.MaxDifficultyLevel);
                spawnable.SpawnWeight = row.GetFloatValue(SpawnWeightColumnName, spawnable.SpawnWeight);
            }
        }
    }
}
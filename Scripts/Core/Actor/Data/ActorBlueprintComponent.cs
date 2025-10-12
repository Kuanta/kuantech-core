using System;
using Kuantech.Core.Database;

namespace Kuantech.Core
{
    [Serializable]
    public abstract class ActorBlueprintComponent
    {
        public abstract void OnActorCreated(ActorBlueprint blueprint, Actor actor);

        public virtual void UpdateFromDatabaseRow(DataTable.RowData rowData)
        {
            
        }
    }
}
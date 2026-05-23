
using Kuantech.Core.Database.Attributes;
using Kuantech.Core.Database;
using Kuantech.Rpg.Managers;
using Kuantech.Core;

namespace Kuantech.Rpg
{
    public class Affix
    {
        [KtDatabaseVariable("AffixName")] public string AffixName { get; private set; }
        [KtDatabaseVariable("Weight")] public float Weight { get; private set; }

        public virtual void ReadFromRow(DataTable.RowData row)
        {
            DataTable.SetVariablesFromRow(this, row);
        }    

        public virtual void ApplyAffixToActor(Actor actor)
        {
            
        }

        public virtual void RemoveAffixFromActor(Actor actor)
        {

        }
    }
}
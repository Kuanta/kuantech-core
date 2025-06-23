using Cysharp.Threading.Tasks;
using Kuantech.Core.Data;
using Kuantech.Core.Database;

namespace Kuantech.Utils
{
    /// <summary>
    /// A config soruce that reads from a DataTable.
    /// </summary>
    public class DataCollectionConfigSource : ConfigSource
    {
        public DataCollection DataTable;
        
        public override async UniTask Initialize(ConfigManager configManager)
        {
            await base.Initialize(configManager);

            if (DataTable == null) return;

            foreach (var entry in DataTable.Entries)
            {
                ConfigDataDictionary[entry.Id] = entry.Data;
            }
        }
    }
}
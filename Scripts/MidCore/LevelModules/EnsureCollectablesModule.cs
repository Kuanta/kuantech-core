using System.Collections.Generic;
using Kuantech.Core;

namespace Kuantech.Midcore
{
    /// <summary>
    /// Ensures given collectables are unlocked
    /// </summary>
    public class EnsureCollectablesModule : LevelModule
    {
        public List<CollectableAsset> Collectables;
        
        public override void Initialize()
        {
            base.Initialize();
            foreach (var collectable in Collectables)
            {
                if (!ProgressionManager.IsProgressibleUnlocked(collectable))
                {
                    ProgressionManager.UnlockProgressible(collectable);
                }
            }
        } 
    }
}
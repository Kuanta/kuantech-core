using Kuantech.Core;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class WavePhase : LevelPhase
    {
        public override string Key => "WavePhase";
        
        public override void OnEnter(Level level)
        {
            base.OnEnter(level);
            WaveHandlerModule whm = ParentLevel.GetLevelModule<WaveHandlerModule>();
            if (whm == null)
            {
                Debug.LogError("Current level doesn't have wave handler module");
                return;
            }
        }
    }
}
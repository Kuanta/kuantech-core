using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// Spawns one or more enemies (a boss, an elite pack, a surge) through the wave handler's spawn
    /// chokepoint. Because it calls SpawnEnemy directly — not the budgeted batch loop — it ignores the
    /// concurrent cap AND lands in the alive set, so wave completion automatically waits for these enemies
    /// to die. That is what gates the win on the boss with no extra bookkeeping.
    /// </summary>
    [Serializable]
    public class SpawnEnemyAction : WaveEventAction
    {
        // Id, not a direct ActorBlueprint reference -- wave events round-trip through JSON (see
        // ArenaData.WaveEvents), and neither JsonUtility nor Newtonsoft can carry a UnityEngine.Object
        // reference. Resolved against the handler's own roster at execute time, same lifetime as the
        // enemies it spawns alongside.
        public string BlueprintId;
        [Min(1)] public int Count = 1;

        public override void Execute(HordeWaveHandler handler)
        {
            ActorBlueprint blueprint = handler.EnemyBlueprints != null
                ? handler.EnemyBlueprints.GetActorBlueprint(BlueprintId)
                : null;
            if (blueprint == null) return;

            for (int i = 0; i < Count; i++)
            {
                Vector3 pos = handler.GetEventSpawnPosition();
                handler.SpawnEnemy(blueprint, pos);
            }
        }
    }
}

using System.Collections.Generic;
using Kuantech.Core.HyperCasual.Runner;
using Kuantech.Utils;
using UnityEngine;
namespace Kuantech.HyperCasual.Core.Runner
{
    public class RandomGateSelector : MonoBehaviour, IChunkElement {
        [SerializeField] private List<GameObject> GatePrefabs;
        [SerializeField] private List<Transform> GateSlots;

        public void OnPreChunkGenerated(RunnerChunk chunk)
        {
            if(GateSlots.Count == 0 || GatePrefabs.Count < GateSlots.Count) return;
            GatePrefabs.Shuffle();

            for(int i=0;i<GateSlots.Count;++i)
            {
                GameObject instantiated = Instantiate(GatePrefabs[i]);
                instantiated.AttachToParent(GateSlots[i]);
            }
        }

        public void OnChunkGenerated(RunnerChunk chunk)
        {
        }

        public void OnChunkRestart()
        {
        }

        public void OnClearChunk()
        {
        }

        public void OnPlayerEnteredChunk()
        {
        }

        public void OnPlayerExitedChunk()
        {
        }

        
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class Spawner : LevelElement
    {
        public GameObject SpawnableParent;
        public List<MonoBehaviour> spawnables;
        public List<Vector3> InitialPositions;
        public List<Quaternion> InitialRotations;
        public GameObject Prefab;

        private List<ISpawnable> _ispawnables;
        private void Awake()
        {
            if (SpawnableParent != null)
            {
                MonoBehaviour[] monos = SpawnableParent.GetComponentsInChildren<MonoBehaviour>();
                foreach (var mono in monos)
                {
                    spawnables.Add(mono);
                }
            }
            
            if (spawnables == null) return;
            InitialPositions = new List<Vector3>();
            InitialRotations = new List<Quaternion>();
            _ispawnables = new List<ISpawnable>();
            foreach (var spawnable in spawnables)
            {
                if (spawnable is ISpawnable ispawnable)
                {
                    InitialPositions.Add(spawnable.transform.position);
                    InitialRotations.Add(spawnable.transform.rotation);
                    _ispawnables.Add(ispawnable);
                }
            }
        }

        public override void OnPrepareLevel()
        {
            ResetActors();
        }

        public override void OnLeaveLevel()
        {
        }

        public override void OnPlayLevel()
        {
            SpawnActors();
        }

        public override void OnPlayerEntered()
        {
            for (int i = 0; i < _ispawnables.Count; ++i)
            {
                _ispawnables[i].OnPlayerEnteredChunk();
            }
        }

        public override void OnPlayerExited()
        {
            
        }

        
        /// <summary>
        /// Calls spawn for existing actors
        /// </summary>
        private void SpawnActors()
        {
            //todo: If not every spawnable is a mono, ther would be mismatches between initial transforms and spawnables list
            for (int i = 0; i < _ispawnables.Count; ++i)
            {
                _ispawnables[i].OnSpawn(InitialPositions[i], InitialRotations[i]);
            }
        }
        
        private void ResetActors()
        {
            //todo: If not every spawnable is a mono, ther would be mismatches between initial transforms and spawnables list
            for (int i = 0; i < _ispawnables.Count; ++i)
            {
                _ispawnables[i].OnRespawn(InitialPositions[i], InitialRotations[i]);
            }
        }
    }
}
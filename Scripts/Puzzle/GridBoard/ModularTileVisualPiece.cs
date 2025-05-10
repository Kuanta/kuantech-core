using System;
using System.Collections.Generic;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class ModularTileVisualPiece : MonoBehaviour
    {
        public ModularTilePieceCollection.ModularTileVisualType PieceType;
        public float LocalAngle;
        
        [Serializable]
        public struct DirectionalityConditionEntry
        {
            public GridBoard.Directions Direciton;
            public bool RequiredState;
        }

        public List<DirectionalityConditionEntry> Requirements;
        
        /// <summary>
        /// Toggles the visual depending on the connectivity
        /// </summary>
        /// <param name="eightConnectivitiy"></param>
        public void Toggle(bool[] eightConnectivitiy)
        {
            foreach (var req in Requirements)
            {
                bool connectivityAtDirection = eightConnectivitiy[(int)req.Direciton];
                if (connectivityAtDirection != req.RequiredState)
                {
                    gameObject.SetActive(false);
                    return;
                }
            }
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Instantiates from the collection
        /// </summary>
        [Button("Create Piece")]
        public void CreatePiece(ModularTilePieceCollection pieceCollection)
        {
#if UNITY_EDITOR
            if (!Application.isEditor) return;
            
            transform.DestroyAllChildren();
            GameObject prefab = pieceCollection.GetPrefab(PieceType);
            GameObject gameObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (gameObj == null) return;
            gameObj.AttachToParent(transform);
            transform.localRotation = Quaternion.Euler(0, LocalAngle, 0);
#endif
        }
    }
}
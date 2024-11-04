using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    [CreateAssetMenu(fileName = "Color Palette", menuName = "Kuantech/Utils/Color Palette")]
    public class ColorPalette : ScriptableObject
    {
        public List<Color> Colors;
        public List<Material> MaterialPalette;
        
        public Color GetColor(int colorIndex)
        {
            return Colors[colorIndex];
        }

        public Material GetMaterial(int index)
        {
            if (MaterialPalette.Count <= index) return null;
            return MaterialPalette[index];
        }
        
        [Button("Update Material Colors")]
        public void UpdateMaterialColors()
        {
            #if UNITY_EDITOR
            if (MaterialPalette == null) return;
            int maxCount = Mathf.Min(Colors.Count, MaterialPalette.Count);
            for (int i = 0; i < maxCount; ++i)
            {
                MaterialPalette[i].SetColor("_BaseColor", Colors[i]);
                EditorUtility.SetDirty(MaterialPalette[i]);
            }
            AssetDatabase.SaveAssets();
            #endif
        }
    }
}
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    [Serializable]
    public struct ColorPaletteEntry
    {
        public Color Color;
        public Material Material;
        public Sprite Sprite;
    }
    
    [CreateAssetMenu(fileName = "Color Palette", menuName = "Kuantech/Utils/Color Palette")]
    public class ColorPalette : ScriptableObject
    {
        // public List<Color> Colors;
        // public List<Material> MaterialPalette;
        public List<ColorPaletteEntry> ColorPaletteEntries;
        public ColorPaletteEntry InvalidEntry;
        
        public Color GetColor(int colorIndex)
        {
            return GetEntry(colorIndex).Color;
        }

        public Material GetMaterial(int index)
        {
            return GetEntry(index).Material;
        }

        public Sprite GetSprite(int index)
        {
            return GetEntry(index).Sprite;
        }

        public ColorPaletteEntry GetEntry(int index)
        {
            if (index < 0) return InvalidEntry;
            index = Mathf.Clamp(index, 0, ColorPaletteEntries.Count);
            return ColorPaletteEntries[index];
        }
        [Button("Update Material Colors")]
        public void UpdateMaterialColors()
        {
            #if UNITY_EDITOR
            for (int i = 0; i < ColorPaletteEntries.Count; ++i)
            {
                ColorPaletteEntries[i].Material.SetColor("_BaseColor",  ColorPaletteEntries[i].Color);
                EditorUtility.SetDirty(ColorPaletteEntries[i].Material);
            }
            AssetDatabase.SaveAssets();
            #endif
        }
    }
}
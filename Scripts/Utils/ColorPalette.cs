using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    [CreateAssetMenu(fileName = "Color Palette", menuName = "Kuantech/Utils/Color Palette")]
    public class ColorPalette : ScriptableObject
    {
        public List<Color> Colors;

        public Color GetColor(int colorIndex)
        {
            return Colors[colorIndex];
        }
    }
}
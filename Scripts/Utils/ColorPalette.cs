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
        // public Color GetColor(float alpha)
        // {
        //     if (Colors.Count == 1) return Colors[0];
        //
        //     float scaledAlpha = alpha * (Colors.Count - 1);
        //     int index = Mathf.FloorToInt(scaledAlpha);
        //     float localAlpha = scaledAlpha - index;
        //
        //     Color startColor = Colors[Mathf.Clamp(index, 0, Colors.Count - 1)];
        //     Color endColor = Colors[Mathf.Clamp(index + 1, 0, Colors.Count - 1)];
        //
        //     return Color.Lerp(startColor, endColor, localAlpha);
        // }
    }
}
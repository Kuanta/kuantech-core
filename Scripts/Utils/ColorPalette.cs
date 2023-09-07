using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    [Serializable]
    public class ColorPalette
    {
        public List<Color> Colors;

        public Color GetColor(float alpha)
        {
            if (Colors.Count == 1) return Colors[0];

            float scaledAlpha = alpha * (Colors.Count - 1);
            int index = Mathf.FloorToInt(scaledAlpha);
            float localAlpha = scaledAlpha - index;

            Color startColor = Colors[Mathf.Clamp(index, 0, Colors.Count - 1)];
            Color endColor = Colors[Mathf.Clamp(index + 1, 0, Colors.Count - 1)];

            return Color.Lerp(startColor, endColor, localAlpha);
        }
    }
}
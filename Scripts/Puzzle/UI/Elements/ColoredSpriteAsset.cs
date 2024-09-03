using Kuantech.Core.Utils;
using UnityEngine;

namespace Kuantech.Puzzle.UI
{
    [CreateAssetMenu(fileName = "ColoredAsset", menuName = "Kuantech/Puzzle/ColoredSprite")]
    public class ColoredSpriteAsset : ScriptableObject
    {
        public Sprite Sprite;
        public Sprite MaskSprite;
        [SerializeField] [Tooltip("If color palette is set, this index will pick a color from it")] private int ColorPaletteIndex;
        [SerializeField] [Tooltip("If set to a non null value, sprite color will be get from this")] private ColorPalette ColorPalette;
        [SerializeField] private Color SpriteColor = Color.white;

        public Color GetColor()
        {
            if (ColorPalette == null) return SpriteColor;
            if (ColorPalette.Colors.Count <= ColorPaletteIndex)
            {
                return SpriteColor;
            }
            return ColorPalette.GetColor(ColorPaletteIndex);
        }
    }
}
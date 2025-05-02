using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Utils;
using Kuantech.Utils;

namespace Kuantech.Puzzle
{
    public class ArtStyleManager : SubManager
    {
        public List<ColorPalette> ColorPalettes;

        public static ColorPalette GetColorPalette(int index)
        {
            var context = ArtStyleManager.GetContext<ArtStyleManager>();
            return context.ColorPalettes[index];
        }
        
        public static ColorPalette GetRandomColorPalette()
        {
            var context = ArtStyleManager.GetContext<ArtStyleManager>();
            return context.ColorPalettes.GetRandomElement();
        }
    }
}
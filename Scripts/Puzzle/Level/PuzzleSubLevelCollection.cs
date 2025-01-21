using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [CreateAssetMenu(fileName = "Sublevel Collection", menuName = "Kuantech/Puzzle/SubLevels")]
    public class PuzzleSubLevelCollection : ScriptableObject
    {
        public List<PuzzleSubLevel> TutorialSubLevels;

        public List<PuzzleSubLevel> RegularSubLevels;

        public virtual PuzzleSubLevel GetSubLevel(int subLevelIndex, int tutorialIndex)
        {
            if (tutorialIndex >= 0 && tutorialIndex < TutorialSubLevels.Count)
            {
                return TutorialSubLevels[tutorialIndex];
            }

            if (RegularSubLevels.IsNullOrEmpty())
            {
                return null;
            }

            return RegularSubLevels[tutorialIndex % RegularSubLevels.Count];
        }
    }
}
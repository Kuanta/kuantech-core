using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [Serializable]
    public struct SubLevelCategoryEntry
    {
        public string CategoryName;
        public List<PuzzleSubLevel> SubLevels;
    }
    [CreateAssetMenu(fileName = "Sublevel Collection", menuName = "Kuantech/Puzzle/SubLevels")]
    public class PuzzleSubLevelCollection : ScriptableObject
    {
        public List<PuzzleSubLevel> TutorialSubLevels;

        //public List<PuzzleSubLevel> RegularSubLevels;
        public List<SubLevelCategoryEntry> SubLevelCategoryEntries;
        
        public virtual PuzzleSubLevel GetSubLevel(string subLevelCategory, int subLevelIndex)
        {
            List<PuzzleSubLevel> subLevels = SubLevelCategoryEntries[0].SubLevels;
            foreach (var entry in SubLevelCategoryEntries)
            {
                if (string.Equals(entry.CategoryName, subLevelCategory, StringComparison.OrdinalIgnoreCase))
                {
                    subLevels = entry.SubLevels;
                    break;

                }
            }
            
            //Couldn't found the category
            int index = subLevelIndex % subLevels.Count;
            return subLevels[index];
        }

        public virtual PuzzleSubLevel GetTutorialSubLevel(int tutorialIndex)
        {
            return TutorialSubLevels[tutorialIndex];
        }
    }
}
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    public class MatchThreeElementData : ScriptableObject {
        public int Type;
        public string Name;
        public Sprite Icon;
        public MatchThreeElementVisual VisualPrefab;

        public virtual bool IsSameType(MatchThreeElementData otherData)
        {
            if(otherData == null) return false;
            return Type == otherData.Type;
        }
    }
}
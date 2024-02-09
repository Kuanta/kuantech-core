using UnityEngine;

namespace Kuantech.Puzzle
{
    public class GridBoardEditorTile : MonoBehaviour {
        public GameObject Prefab;
        public int Row;
        public int Column;
        public GameObject EditorObject;
        public void DestroyEditorGameobject()
        {
            if(EditorObject == null) return;
            Destroy(EditorObject);
            EditorObject = null;
        }
    }
}
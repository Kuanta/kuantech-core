#if NETWORKING_NGO
using Unity.Netcode.Editor;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Core.EditorTools
{
    /// <summary>
    /// Draws ActorModule inspectors through Unity's own property loop.
    ///
    /// NGO ships [CustomEditor(typeof(NetworkBehaviour), true)], which every ActorModule inherits once
    /// NETWORKING_NGO is on, and that editor hand-rolls its property loop rather than using Unity's: it
    /// walks the SerializedObject itself and wraps every property that is not an ObjectReference in an
    /// EditorGUILayout horizontal group. A custom PropertyDrawer that positions its own rects -- which is
    /// what SubclassSelectorDrawer, the only way polymorphic fields are pickable in this project, does --
    /// has no business being inside one.
    ///
    /// Note this is NOT what made [SerializeReference] fields invisible: they stayed invisible under
    /// DrawDefaultInspector too, which is why they need SubclassSelectorDrawer in the first place. This
    /// editor only makes sure that drawer runs in the same plain layout context as everywhere else in the
    /// project, where it is known to work.
    ///
    /// Play mode is deliberately left to NGO: its editor renders live NetworkVariable values, which is
    /// worth keeping and is pure debug output anyway -- nothing gets authored in play mode. Deriving from
    /// its editor rather than from UnityEditor.Editor also keeps its OnEnable, which is what warns about
    /// a NetworkBehaviour sitting on a GameObject with no NetworkObject.
    /// </summary>
    [CustomEditor(typeof(ActorModule), true)]
    [CanEditMultipleObjects]
    public class ActorModuleEditor : NetworkBehaviourEditor
    {
        public override void OnInspectorGUI()
        {
            if (Application.isPlaying)
            {
                base.OnInspectorGUI();
                return;
            }

            DrawDefaultInspector();
        }
    }
}
#endif

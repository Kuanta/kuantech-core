using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Kuantech.Core.Animation
{
    /// <summary>
    /// Minimal driver for an Animation Rigging aim rig. Does two things and nothing else: parks the rig's
    /// aim target on the point the camera is looking at, and pushes <see cref="Weight"/> into the Rig so the
    /// whole effect can be dialled in from the Inspector while the game runs.
    ///
    /// The target cannot simply be parented to the camera: Animation Rigging binds every transform a
    /// constraint references through the Animator, so the target has to live inside the character's own
    /// hierarchy. Copying the camera's aim point onto it each frame is the whole reason this component
    /// exists.
    ///
    /// Deliberately has no knowledge of Actor, combat or input -- this is the "does the character turn at
    /// all" step. Driving the weight from an attack comes later.
    /// </summary>
    public class AimRigController : MonoBehaviour
    {
        [Tooltip("The Rig component on the aim rig layer. Its weight is overwritten every frame with Weight below.")]
        public Rig Rig;

        [Tooltip("The target transform inside the rig that the aim constraints use as their Source Object.")]
        public Transform AimTarget;

        [Tooltip("Transform whose forward defines where the character aims. Leave empty to use Camera.main.")]
        public Transform AimSource;

        [Tooltip("How far along the aim source's forward the target is parked. Kept large and constant on " +
                 "purpose -- a target close to the chest makes small camera moves swing the spine wildly.")]
        public float Distance = 25f;

        [Tooltip("Overall strength of the rig. Slide this at runtime to see what the rig is actually doing.")]
        [Range(0f, 1f)] public float Weight = 1f;

        // Update, not LateUpdate: the rig's PlayableGraph is evaluated as part of the Animator pass, which
        // runs BEFORE LateUpdate. Moving the target in LateUpdate would leave the rig a frame behind.
        private void Update()
        {
            Transform source = GetAimSource();
            if (source == null) return;

            if (AimTarget != null)
                AimTarget.position = source.position + source.forward * Distance;

            if (Rig != null)
                Rig.weight = Weight;
        }

        private Transform GetAimSource()
        {
            if (AimSource != null) return AimSource;
            UnityEngine.Camera main = UnityEngine.Camera.main;
            return main != null ? main.transform : null;
        }
    }
}

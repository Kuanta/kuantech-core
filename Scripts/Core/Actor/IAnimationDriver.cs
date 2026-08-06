namespace Kuantech.Core
{
    /// <summary>
    /// Stand-in for an Animator. <see cref="AnimationModule"/> normally writes parameters straight onto one,
    /// but an actor is not required to have an Animator at all — a GPU-baked crowd agent, for instance, has
    /// no Animator and no SkinnedMeshRenderer, only a playback cursor into baked data.
    ///
    /// Rather than teaching every caller about that case, AnimationModule forwards the same parameter writes
    /// to a driver whenever it has no Animator. Combat, spawn and gameplay code keep calling AnimationModule
    /// exactly as before and never learn which kind of animation the actor uses.
    ///
    /// Implementations receive parameter hashes, not names, because that is what the callers already have.
    /// </summary>
    public interface IAnimationDriver
    {
        void SetFloat(int parameterHash, float value);
        void SetBool(int parameterHash, bool value);
        void SetTrigger(int parameterHash);
        void SetInteger(int parameterHash, int value);
    }
}

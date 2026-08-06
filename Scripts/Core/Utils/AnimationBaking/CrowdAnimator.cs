using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Playback state for a single baked agent — the replacement for an Animator, minus the state machine.
    /// It is a plain class on purpose: no MonoBehaviour, no Update of its own. <see cref="CrowdRenderer"/>
    /// ticks every agent from one loop, which is the whole point of the system.
    ///
    /// Its only output is three numbers: two texture rows and a weight between them. The shader always does
    /// the same thing with them (read both poses, lerp), so the CPU is free to decide what the pair means:
    ///
    ///   plain playback  → the two rows straddle the current time, weight is the sub-frame remainder,
    ///                     which is what smooths a 30 fps bake back into continuous motion
    ///   blend           → the two rows are the same phase of two different clips (locomotion idle↔run)
    ///   cross-fade      → the two rows are the outgoing and incoming clips, weight is the fade progress
    ///
    /// Blending and cross-fading therefore cost the shader nothing extra, but they do consume the pair, so
    /// sub-frame interpolation is dropped while one is active. That is invisible for a fade (it lasts a few
    /// frames) and the reason <see cref="CrowdAnimationSet.FrameRate"/> matters for sustained blends.
    /// </summary>
    public sealed class CrowdAnimator
    {
        private const float MinFadeDuration = 0.0001f;

        private readonly CrowdAnimationSet _set;

        private CrowdAnimationClip _current;
        private float _time;

        // Sustained blend (locomotion): a second clip played at the same normalized phase as _current.
        private CrowdAnimationClip _blendClip;
        private float _blendWeight;

        // Cross-fade: the clip we are leaving, kept advancing so the transition does not freeze mid-step.
        private CrowdAnimationClip _fadeFrom;
        private float _fadeFromTime;
        private float _fadeDuration;
        private float _fadeElapsed;

        /// <summary>Playback rate multiplier. Attack speed, slow effects and per-agent variety ride on this.</summary>
        public float Speed { get; set; } = 1f;

        /// <summary>First texture row the shader should read.</summary>
        public int Frame0 { get; private set; }

        /// <summary>Second texture row the shader should read.</summary>
        public int Frame1 { get; private set; }

        /// <summary>How far to lerp from <see cref="Frame0"/> towards <see cref="Frame1"/>.</summary>
        public float Weight { get; private set; }

        /// <summary>True once a non-looping clip has reached its end. Always false while a clip loops.</summary>
        public bool IsFinished { get; private set; }

        public CrowdAnimationClip CurrentClip => _current;

        public CrowdAnimator(CrowdAnimationSet set)
        {
            _set = set;
            _current = set != null ? set.DefaultClip : null;
            Resolve();
        }

        /// <summary>
        /// Plays a single clip, optionally cross-fading from whatever is playing now. Re-playing the clip
        /// that is already current is ignored so a per-frame "keep running" call does not restart the loop;
        /// use <see cref="Restart"/> when a re-trigger really is intended (a second attack swing).
        /// </summary>
        public void Play(int clipNameHash, float fadeDuration = 0.15f)
        {
            CrowdAnimationClip next = _set != null ? _set.GetClip(clipNameHash) : null;
            if (next == null || next == _current) return;

            BeginFade(next, fadeDuration);
        }

        public void Play(string clipName, float fadeDuration = 0.15f) => Play(Animator.StringToHash(clipName), fadeDuration);

        /// <summary>Plays a clip from the start even if it is already current — for re-triggered one-shots.</summary>
        public void Restart(int clipNameHash, float fadeDuration = 0.05f)
        {
            CrowdAnimationClip next = _set != null ? _set.GetClip(clipNameHash) : null;
            if (next == null) return;

            if (next == _current)
            {
                _time = 0f;
                IsFinished = false;
                Resolve();
                return;
            }

            BeginFade(next, fadeDuration);
        }

        /// <summary>
        /// Holds two clips blended at a fixed weight, both driven at the same normalized phase so their
        /// footfalls line up. This is how a locomotion blend tree is reproduced: idle at weight 0, run at 1.
        /// Timing follows <paramref name="clipNameHashA"/>, so a walk↔run blend keeps the walk's cadence.
        /// </summary>
        public void PlayBlend(int clipNameHashA, int clipNameHashB, float weight, float fadeDuration = 0.15f)
        {
            CrowdAnimationClip a = _set != null ? _set.GetClip(clipNameHashA) : null;
            CrowdAnimationClip b = _set != null ? _set.GetClip(clipNameHashB) : null;
            if (a == null) return;

            if (a != _current) BeginFade(a, fadeDuration);

            _blendClip = b;
            _blendWeight = Mathf.Clamp01(weight);
        }

        /// <summary>Drops back to single-clip playback, ending any sustained blend.</summary>
        public void ClearBlend() => _blendClip = null;

        /// <summary>
        /// Advances playback. Called by <see cref="CrowdRenderer"/> for every registered agent; nothing else
        /// should tick it, or the agent would run at a multiple of real time.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (_current == null)
            {
                if (_set == null) return;
                _current = _set.DefaultClip;
                if (_current == null) return;
            }

            float scaled = deltaTime * Speed;
            _time += scaled;

            if (_current.Loop)
            {
                if (_current.Duration > 0f) _time = Mathf.Repeat(_time, _current.Duration);
            }
            else if (_time >= _current.Duration)
            {
                _time = _current.Duration;
                IsFinished = true;
            }

            if (_fadeFrom != null)
            {
                _fadeElapsed += deltaTime;
                _fadeFromTime += scaled;
                if (_fadeFrom.Loop && _fadeFrom.Duration > 0f) _fadeFromTime = Mathf.Repeat(_fadeFromTime, _fadeFrom.Duration);
                if (_fadeElapsed >= _fadeDuration) _fadeFrom = null;
            }

            Resolve();
        }

        /// <summary>Rewinds to the default clip. Used when a pooled body is handed out again.</summary>
        public void Reset()
        {
            _current = _set != null ? _set.DefaultClip : null;
            _time = 0f;
            _blendClip = null;
            _blendWeight = 0f;
            _fadeFrom = null;
            _fadeElapsed = 0f;
            Speed = 1f;
            IsFinished = false;
            Resolve();
        }

        private void BeginFade(CrowdAnimationClip next, float fadeDuration)
        {
            // Fading out of an existing fade would need a third sample, so the in-flight one is dropped and
            // the new fade starts from the clip we were already heading towards. Barely visible at these
            // durations, and it keeps the shader at two samples.
            _fadeFrom = _current;
            _fadeFromTime = _time;
            _fadeDuration = Mathf.Max(fadeDuration, MinFadeDuration);
            _fadeElapsed = 0f;

            _current = next;
            _time = 0f;
            IsFinished = false;

            // A cross-fade and a sustained blend compete for the same sample pair; the fade wins while it runs.
            Resolve();
        }

        /// <summary>
        /// Turns the current playback state into the sample pair the shader consumes. The three cases are
        /// ordered by priority: a fade overrides a blend, and a blend overrides sub-frame interpolation.
        /// </summary>
        private void Resolve()
        {
            if (_current == null)
            {
                Frame0 = Frame1 = 0;
                Weight = 0f;
                return;
            }

            float phase = _current.Duration > 0f ? _time / _current.Duration : 0f;

            if (_fadeFrom != null)
            {
                float fromPhase = _fadeFrom.Duration > 0f ? _fadeFromTime / _fadeFrom.Duration : 0f;
                Frame0 = FrameAtPhase(_fadeFrom, fromPhase);
                Frame1 = FrameAtPhase(_current, phase);
                Weight = Mathf.Clamp01(_fadeElapsed / _fadeDuration);
                return;
            }

            if (_blendClip != null)
            {
                Frame0 = FrameAtPhase(_current, phase);
                Frame1 = FrameAtPhase(_blendClip, phase);
                Weight = _blendWeight;
                return;
            }

            // Plain playback: straddle the current time with two neighbouring rows and let the shader
            // interpolate, which is what hides the bake frame rate.
            float exact = phase * _current.FrameCount;
            int index = Mathf.FloorToInt(exact);
            float remainder = exact - index;

            int next = index + 1;
            if (_current.Loop)
            {
                index = WrapIndex(index, _current.FrameCount);
                next = WrapIndex(next, _current.FrameCount);
            }
            else
            {
                index = Mathf.Clamp(index, 0, _current.FrameCount - 1);
                next = Mathf.Clamp(next, 0, _current.FrameCount - 1);
            }

            Frame0 = _current.StartFrame + index;
            Frame1 = _current.StartFrame + next;
            Weight = remainder;
        }

        private static int FrameAtPhase(CrowdAnimationClip clip, float phase)
        {
            if (clip.FrameCount <= 0) return clip.StartFrame;

            int index = Mathf.FloorToInt(phase * clip.FrameCount);
            index = clip.Loop ? WrapIndex(index, clip.FrameCount) : Mathf.Clamp(index, 0, clip.FrameCount - 1);
            return clip.StartFrame + index;
        }

        private static int WrapIndex(int index, int count)
        {
            if (count <= 0) return 0;
            index %= count;
            return index < 0 ? index + count : index;
        }
    }
}

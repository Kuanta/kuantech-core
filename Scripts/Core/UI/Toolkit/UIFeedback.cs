using System;
using UnityEngine;

namespace Kuantech.Core.UI
{
    /// <summary>What happened in the UI. Elements and the stack report these; they never play sounds themselves.</summary>
    public enum UIFeedbackType
    {
        None = 0,
        Click,
        Back,
        Open,
        Close,
        Confirm,
        Error,
        Reward,
    }

    /// <summary>
    /// The one place where UI elements ask for sound and visual effects. Core only says "a Click happened"; the game
    /// sets <see cref="Handler"/> once and decides what a Click sounds and looks like. Without a handler it does nothing.
    /// </summary>
    public static class UIFeedback
    {
        /// <summary>Set by the game. Called for every feedback request.</summary>
        public static Action<UIFeedbackType> Handler;

        public static void Play(UIFeedbackType type)
        {
            if (type == UIFeedbackType.None) return;
            Handler?.Invoke(type);
        }

        // Static state survives when domain reload is turned off in the editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Handler = null;
    }
}

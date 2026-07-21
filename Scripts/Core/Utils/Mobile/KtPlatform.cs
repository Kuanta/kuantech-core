namespace Kuantech.Utils.Mobile
{
    /// <summary>
    /// Compile-time platform flags, so gameplay code asks "is this touch-first?" instead of repeating
    /// UNITY_ANDROID || UNITY_IOS everywhere and drifting apart.
    ///
    /// These are const bools rather than #if symbols on purpose: the C# compiler strips branches on a
    /// const false exactly like a preprocessor block would, but unlike #if the code inside still gets
    /// compiled and refactored, a typo is an error instead of silence, and nothing has to be installed
    /// into the project's scripting define symbols first.
    /// </summary>
    public static class KtPlatform
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        /// <summary>A real touch device: no mouse, no keyboard. On-screen controls are the only input.</summary>
        public const bool PureMobile = true;
#else
        public const bool PureMobile = false;
#endif

#if UNITY_EDITOR
        public const bool Editor = true;
#else
        public const bool Editor = false;
#endif

        /// <summary>
        /// Show and honour the touch controls. True in the editor too, so the mobile layout can be tested
        /// without a build — but unlike PureMobile it leaves mouse and keyboard working alongside.
        /// </summary>
        public const bool MobileOrEditor = PureMobile || Editor;
    }
}

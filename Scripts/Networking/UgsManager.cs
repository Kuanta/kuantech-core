using System;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Sirenix.OdinInspector;
using UnityEngine;
#if UGS_SERVICES
using Unity.Services.Authentication;
using Unity.Services.Core;
#endif

namespace Kuantech.Networking
{
    /// <summary>
    /// Owns the Unity Gaming Services handshake: service initialization plus an anonymous sign-in.
    /// This is the account layer -- it sits BELOW both the party layer and Netcode, and touches neither.
    /// Every later session call (create a party, join one by code) happens *as* a signed-in player, so
    /// nothing in the party layer can run before this has succeeded.
    /// </summary>
    public class UgsManager : SubManager
    {
        private const string ProfileArgument = "-ugsProfile";

        [Header("UGS")]
        [Tooltip("Sign in as soon as the game boots. Note this makes GameManager's startup wait on the " +
                 "network round-trip -- turn it off to drive sign-in from a menu button via " +
                 "EnsureSignedIn() instead, once there is a menu to drive it from.")]
        public bool SignInOnInitialize = true;

        [Tooltip("Authentication profile to sign in under. An anonymous sign-in caches its token per " +
                 "profile, so two instances sharing one profile come back as the SAME player, which makes " +
                 "it impossible for them to sit in a party together. Leave empty to let " +
                 "AutoProfilePerInstance decide. The '-ugsProfile <name>' command-line argument overrides " +
                 "whatever is set here.")]
        public string ProfileName = "";

        [Tooltip("With no explicit profile, derive one from this instance's install path. Two Multiplayer " +
                 "Play Mode virtual players run out of separate project clones, and a build lives " +
                 "somewhere else again, so each lands on its own profile and therefore its own anonymous " +
                 "player -- which is what lets them see each other in a party. Turn off to use the single " +
                 "shared default profile.")]
        public bool AutoProfilePerInstance = true;

        [Header("Status (runtime read-out)")]
        [Tooltip("Last thing that happened, mirrored here so the Inspector shows it during play.")]
        public string Status = "Not initialized";
        public string PlayerId = "";

        // Set while a sign-in is in flight so a second caller rides the first attempt instead of firing
        // its own (e.g. a menu button pressed twice before the first round-trip comes back).
        private UniTaskCompletionSource<bool> _signInOperation;

        public static UgsManager Get() => GetContext<UgsManager>();

        public bool IsSignedIn
        {
            get
            {
#if UGS_SERVICES
                return UnityServices.State == ServicesInitializationState.Initialized &&
                       AuthenticationService.Instance.IsSignedIn;
#else
                return false;
#endif
            }
        }

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            if (SignInOnInitialize) await EnsureSignedIn();
        }

        /// <summary>
        /// Initializes UGS and signs in anonymously if not already signed in. Safe to call repeatedly:
        /// returns immediately when already signed in, and joins the in-flight attempt when one is running.
        /// Never throws -- a failure comes back as false, with the reason in <see cref="Status"/>.
        /// </summary>
        public async UniTask<bool> EnsureSignedIn()
        {
            if (IsSignedIn) return true;
            if (_signInOperation != null) return await _signInOperation.Task;

            _signInOperation = new UniTaskCompletionSource<bool>();
            bool signedIn = await SignIn();
            _signInOperation.TrySetResult(signedIn);
            _signInOperation = null; // cleared either way, so a failed attempt can be retried
            return signedIn;
        }

        private async UniTask<bool> SignIn()
        {
#if UGS_SERVICES
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    string profile = ResolveProfileName();
                    InitializationOptions options = new InitializationOptions();
                    if (!string.IsNullOrEmpty(profile)) options.SetProfile(profile);

                    SetStatus($"Initializing services (profile '{(string.IsNullOrEmpty(profile) ? "default" : profile)}')...");
                    await UnityServices.InitializeAsync(options);
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    SetStatus("Signing in anonymously...");
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                PlayerId = AuthenticationService.Instance.PlayerId;
                SetStatus($"Signed in as {PlayerId}");
                return true;
            }
            catch (Exception e)
            {
                PlayerId = "";
                SetStatus($"Sign-in failed: {e.Message}");
                Debug.LogError($"[UgsManager] SignIn failed: {e}");
                return false;
            }
#else
            SetStatus("UGS_SERVICES scripting define is missing.");
            Debug.LogError("[UgsManager] The UGS_SERVICES scripting define is missing, so this manager is " +
                           "compiled out and no sign-in will ever happen. Add it under Project Settings > " +
                           "Player > Scripting Define Symbols (the same way NETWORKING_NGO is set).");
            await UniTask.CompletedTask;
            return false;
#endif
        }

        /// <summary>
        /// Most explicit wins. The command-line argument comes first because the serialized field is one
        /// shared value baked into the prefab -- every instance reads it identically, so it can never be
        /// what distinguishes them. Failing both, a path-derived profile keeps separate instances apart
        /// without anyone having to remember to pass anything.
        /// </summary>
        private string ResolveProfileName()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == ProfileArgument) return args[i + 1];
            }

            if (!string.IsNullOrEmpty(ProfileName)) return ProfileName;
            if (!AutoProfilePerInstance) return "";

            // Profile names allow only letters, digits, '-' and '_', so the path goes in as hex rather
            // than as itself.
            return $"inst{StableHash(Application.dataPath):x8}";
        }

        /// <summary>
        /// FNV-1a. Deliberately not string.GetHashCode(): .NET randomizes that per process, so the same
        /// install would land on a different profile every launch -- a brand new anonymous player each
        /// time, and no way to rejoin as who you were a minute ago.
        /// </summary>
        private static uint StableHash(string value)
        {
            uint hash = 2166136261;
            foreach (char c in value)
            {
                hash = (hash ^ c) * 16777619;
            }
            return hash;
        }

        private void SetStatus(string status)
        {
            Status = status;
            Debug.Log($"[UgsManager] {status}");
        }

        [Button("Sign In Now")]
        private void SignInNow()
        {
            EnsureSignedIn().Forget();
        }
    }
}

#if NETWORKING_NGO
using Unity.Netcode;
#endif
using UnityEngine;

namespace Kuantech.Networking
{
    /// <summary>
    /// Destroys this NetworkManager if one is already running.
    ///
    /// NetworkManager marks itself DontDestroyOnLoad, so the one created in the menu survives into the
    /// level and back. Coming back, the menu scene instantiates its own copy again -- and netcode's
    /// OnEnable only claims the singleton "if (Singleton == null)", so the newcomer neither becomes the
    /// singleton nor goes away. It just sits there, DontDestroyOnLoad'ed, one more every round trip.
    ///
    /// Keeping the ORIGINAL rather than the newcomer matters: MatchManager wired its approval callback
    /// and its events onto that instance once, at startup, and has no way to notice it was replaced.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class SingleNetworkManagerGuard : MonoBehaviour
    {
        private void Awake()
        {
#if NETWORKING_NGO
            NetworkManager self = GetComponent<NetworkManager>();
            if (self == null || NetworkManager.Singleton == null || NetworkManager.Singleton == self) return;

            Debug.Log("[SingleNetworkManagerGuard] A NetworkManager is already live -- destroying this duplicate.");
            Destroy(gameObject);
#endif
        }
    }
}

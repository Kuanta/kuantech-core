using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Networking
{
#if NETWORKING_FISHNET
    using FishNet.Connection;
    using FishNet.Object;

    public class KtActorNetworkBehaviour : NetworkBehaviour
    {
        [SerializeField] protected Actor ParentActor;
        private bool _isLocalPlayer;

        public bool GetAuthority() => IsOwner;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            if (ParentActor == null)
                ParentActor = GetComponent<Actor>();
            if (ParentActor == null)
                return;

            ParentActor.NetworkBehaviour = this;
            ParentActor.Initialize();
            ParentActor.Spawn();

            TryHandleLocalPlayerChange(null);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            if (_isLocalPlayer)
                OnStopLocalPlayer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            TryHandleLocalPlayerChange(null);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if (_isLocalPlayer)
                OnStopLocalPlayer();
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            TryHandleLocalPlayerChange(prevOwner);
        }

        private void TryHandleLocalPlayerChange(NetworkConnection prevOwner)
        {
            if (!IsClientInitialized || NetworkManager == null)
                return;

            if (!Owner.IsValid)
            {
                if (_isLocalPlayer)
                    OnStopLocalPlayer();
                return;
            }

            NetworkConnection localConn = NetworkManager.ClientManager.Connection;
            bool nowLocalPlayer = (Owner == localConn);

            if (nowLocalPlayer == _isLocalPlayer)
                return;

            _isLocalPlayer = nowLocalPlayer;

            if (nowLocalPlayer)
                OnStartLocalPlayer();
            else
                OnStopLocalPlayer();
        }

        protected virtual void OnStartLocalPlayer()
        {
            if (ParentActor != null)
                ParentActor.StartLocalPlayer();
        }

        protected virtual void OnStopLocalPlayer()
        {
            _isLocalPlayer = false;
            if (ParentActor != null)
                ParentActor.StopLocalPlayer();
        }
    }
#else
    public class KtActorNetworkBehaviour : MonoBehaviour
    {
        public bool GetAuthority() => true;
    }
#endif
}

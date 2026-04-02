using System.Collections.Generic;
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

        private readonly List<IKtNetworkComponent> _networkComponents = new List<IKtNetworkComponent>();
        private bool _isLocalPlayer;

        private void CacheNetworkComponents()
        {
            if (ParentActor == null)
                ParentActor = GetComponent<Actor>();

            _networkComponents.Clear();

            if (ParentActor != null)
            {
                // true = inactive children'ları da tara istersen, istemezsen argümanı kaldır.
                _networkComponents.AddRange(
                    ParentActor.GetComponentsInChildren<IKtNetworkComponent>(true)
                );
            }
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            if (ParentActor == null)
                ParentActor = GetComponent<Actor>();
            if (ParentActor == null)
                return;
            
            //Spawn
            ParentActor.Spawn();
            
            CacheNetworkComponents();

            foreach (var component in _networkComponents)
                component.OnStartNetwork(this);

            // Client tarafındaysak ve owner'sak local player check’i yap
            TryHandleLocalPlayerChange(null);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            // Local player’dıysak önce onu düşür
            if (_isLocalPlayer)
                OnStopLocalPlayer();

            foreach (var component in _networkComponents)
                component.OnStopNetwork();
        }

        #region Server Callbacks

        public override void OnStartServer()
        {
            base.OnStartServer();

            foreach (var component in _networkComponents)
                component.OnStartServer();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            foreach (var component in _networkComponents)
                component.OnStopServer();
        }
        #endregion

        #region Client

        public override void OnStartClient()
        {
            base.OnStartClient();

            foreach (var component in _networkComponents)
                component.OnStartClient();

            // Check if we are local player
            TryHandleLocalPlayerChange(null);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if (_isLocalPlayer)
                OnStopLocalPlayer();

            foreach (var component in _networkComponents)
                component.OnStopClient();
        }
        #endregion

        // ------ OWNERSHIP ------

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            var newOwner = Owner;
            foreach (var component in _networkComponents)
                component.OnOwnershipChanged(prevOwner, newOwner);

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

            // Lokal connection (bu client)
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
            foreach (var component in _networkComponents)
                component.OnStartLocalPlayer();
        }

        protected virtual void OnStopLocalPlayer()
        {
            _isLocalPlayer = false;

            foreach (var component in _networkComponents)
                component.OnStopLocalPlayer();
        }

    }
#else
    public class KtActorNetworkBehaviour : MonoBehaviour
    {
    }
#endif
}

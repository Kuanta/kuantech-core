using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Midcore
{
    public class CharacterPreviewManager : SubManager
    {
        public ActorBlueprint PlayerBlueprint;
        public Transform InitialSpawnPoint;
        public bool ClearPlayerOnSceneChange = true;
        //Runtime 
        [NonSerialized] public Actor Player;

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            InstantiatePlayer();
        }

        private void InstantiatePlayer()
        {
            Player = PlayerBlueprint.CreateActor();
            if(InitialSpawnPoint != null)
            {
                Player.transform.position = InitialSpawnPoint.position;
                Player.transform.rotation = InitialSpawnPoint.rotation;
                Player.transform.localScale = Vector3.one;
            }
            OnPlayerInstantiated(Player);
        }

        protected virtual void OnPlayerInstantiated(Actor player)
        {
            //Maybe child classes would want to do extra stuff with player
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if(Player == null) return;
            Player.Despawn();
        }
    }
}
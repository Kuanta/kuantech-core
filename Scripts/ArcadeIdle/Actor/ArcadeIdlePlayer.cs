using Kuantech.Core;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class ArcadeIdlePlayer : ArcadeIdleCharacter
    {
        [Header("Components")] 
        [SerializeField] private ArcadeIdleMovement MovementModule;
        [SerializeField] private ArcadeIdleInputHandler InputHandler;
        
        public override void PostInitialize()
        {
            base.PostInitialize();
            ActorWallet wallet = GetModule<ActorWallet>();
            if(wallet != null)
            {
                wallet.OnCurrencyAdded += OnCurrencyAdded;
                wallet.OnCurrencyRemoved += OnCurrencyRemoved;
            }
        }

        protected override void Update()
        {
            base.Update();
            Vector2 localDirection = InputHandler.GetLocalInput();
            MovementModule.SetMovement(localDirection);
        }

        /// <summary>
        /// Checks if player is standing still
        /// </summary>
        /// <returns></returns>
        public bool IsStandingStill()
        {
            return MovementModule.GetMovement().sqrMagnitude <= 0f;
        }

        public override bool CanInteractWith(VenueInteractable venueInteractable)
        {
            if(!IsStandingStill()) return false;
            return base.CanInteractWith(venueInteractable);
        }
        #region Event Handlers
        /// <summary>
        /// Updates the game state on currency added
        /// </summary>
        /// <param name="args"></param>
        private void OnCurrencyAdded((string, int) args)
        {
            string currencyId = args.Item1;
            int currAmount = args.Item2;
            //todo(currency): fix here
        }

        /// <summary>
        /// Updates the game state on currency removed
        /// </summary>
        /// <param name="args"></param>
        private void OnCurrencyRemoved((string, int) args)
        {
            string currencyId = args.Item1;
            int currAmount = args.Item2;
            //todo(currency): fix here
        
        }
        #endregion
    }
}
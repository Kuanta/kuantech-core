using System.Collections.Generic;
using Cinemachine;
using Kuantech.ArcadeIdle.UI;
using Kuantech.Core;
using Kuantech.HyperCasual;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{   
    public class Unlocker : VenueActor
    {
        public ArcadeIdleTriggerZone TriggerZone;
        public List<VenueActor> UnlockableActors;
        public List<VenueZone> UnlockableZones;
        public List<UpgradeData> UnlockableUpgrades;
        public List<string> CurrencyIds;
        public List<int> Prices;

        [Header("Camera Focus")]
        public CinemachineVirtualCamera CameraToSwitchOnUnlock;
        public float FocusDuration = 2.0f;
        private Dictionary<string, int> _pricesMap;
        public float SendCooldown = 0.1f;
        public float TimeToFill = 2.0f; //How many seconds should player stand in the zone in order to fill this
        private float _lastTimeCurrencySent;
        private ActorWallet _wallet;
        private bool _filled = false;

        [Header("Visuals")]
        [SerializeField] private CurrencyRequirementIndicator ProgressBar;
        [SerializeField] private TMP_Text RemainingCurrencyText;

        public override void Initialize(ActorState actorState)
        {
            base.Initialize(actorState);
            ProgressBar.SetFill(0f);
            _wallet = GetModule<ActorWallet>();
            _wallet.OnCurrencyAdded += OnCurrencyAdded;
            _pricesMap = new Dictionary<string, int>();
            for(int i=0;i<CurrencyIds.Count;++i)
            {
                _pricesMap[CurrencyIds[i]] = Prices[i];
            }
        }

        public override void PostInitialize()
        {
            base.PostInitialize();
            UpdateVisuals();
            float progress = GetProgress();
            if (progress >= 1.0f)
            {
                _filled = true;
                //Just disable the gameobject, since this is initialize
                gameObject.SetActive(false);
            }
        }

        private void UpdateVisuals()
        {
            float progress = GetProgress();
            ProgressBar.SetFill(progress);
            RemainingCurrencyText.text = GetRemainingAmount(CurrencyIds[0]).Stringfy();
        }
        protected override void Update()
        {
            if(!Initialized || _filled) return;

            if(TriggerZone == null) return;
            ArcadeIdleActor currActor = TriggerZone.GetCurrentActor();
            if(currActor == null) return;
            if(Time.time - _lastTimeCurrencySent < SendCooldown) return;
            if(currActor is ArcadeIdlePlayer player)
            {
                TransferCurrency(player);
                _lastTimeCurrencySent = Time.time;
            }

        }

        public void TransferCurrency(ArcadeIdlePlayer player)
        {
            if (!player.IsStandingStill()) return;
            ActorWallet playerWallet = player.GetModule<ActorWallet>();
            if(playerWallet == null)
            {
                Debug.LogError("Player Wallet is null");
                return;
            }
            for (int i = 0; i < CurrencyIds.Count; ++i)
            {
                string currencyId = CurrencyIds[i];
                int requiredAmount = _pricesMap[currencyId];
                int remaining = GetRemainingAmount(CurrencyIds[i]);
                int playerHeld = playerWallet.GetCurrencyAmount(currencyId);
                if(playerHeld <= 0) continue; //Player has no currency
                int maxSendableAmount;
                if(TimeToFill == 0f || SendCooldown == 0f)
                {
                    maxSendableAmount = remaining;
                }else{
                    maxSendableAmount = Mathf.CeilToInt(requiredAmount / (TimeToFill / SendCooldown));
                }

                //Send
                //todo: We can do flying money here or play a short vfx
                int amountToSend = Mathf.Min(maxSendableAmount, playerHeld);
                playerWallet.WithdrawCurrency(currencyId, amountToSend);
                _wallet.DepositCurrency(currencyId, amountToSend);
            }
        }

        public float GetProgress()
        {
            int totalCurrencies = 0;
            int requiredCurrencies = 0;
            for (int i = 0; i < CurrencyIds.Count; ++i)
            {
                requiredCurrencies += Prices[i];
                int heldAmount = _wallet.GetCurrencyAmount(CurrencyIds[i]);
                totalCurrencies += _wallet.GetCurrencyAmount(CurrencyIds[i]);
            }
            return (float)totalCurrencies / (float)requiredCurrencies;
        }
        
        public int GetRemainingAmount(string currencyId)
        {
            int currentlyHeld = _wallet.GetCurrencyAmount(currencyId);
            int remainingAmount = _pricesMap[currencyId] - currentlyHeld;
            return Mathf.Max(0, remainingAmount);
        }

        [Button("Unlock")]
        public virtual void OnFilled()
        {
            if(_filled) return;
            UnlockUnlockables();
            CameraManager camMan = CameraManager.GetContext<CameraManager>();
            if(camMan != null && CameraToSwitchOnUnlock)
            {
                camMan.SwitchToCameraForTime(CameraToSwitchOnUnlock, FocusDuration);
            }
            ProgressBar.SetFill(1.0f);
            gameObject.SetActive(false);
            _filled = true;
        }

        private void UnlockUnlockables()
        {
            if (_filled) return;
            foreach (var unlockable in UnlockableActors)
            {
                ParentZone.ParentVenue.UnlockUnlockable(unlockable);
            }
            foreach (var unlockable in UnlockableZones)
            {
                ParentZone.ParentVenue.UnlockUnlockable(unlockable);
            }
            UpgradeManager um = UpgradeManager.GetContext<UpgradeManager>();
            foreach(var unlockable in UnlockableUpgrades)
            {
                um.BuyUpgrade(unlockable.UpgradeId);
            }
        }
        private void OnCurrencyAdded((string, int) args)
        {
            float progress = GetProgress();
            UpdateVisuals();
            if(progress >= 1.0f)
            {
                OnFilled();
                return;
            }
            ProgressBar.SetFill(progress);
        }

    }
}
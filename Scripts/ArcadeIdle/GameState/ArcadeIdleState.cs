using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    [CreateAssetMenu(fileName ="ArcadeIdleState", menuName ="Kuantech/StateModules/ArcadeIdle")]
    public class ArcadeIdleState : StateModule
    {
        public Dictionary<string, VenueState> VenueStates;

        public override void SetDefaultValues()
        {
            VenueStates = new Dictionary<string, VenueState>();
        }

        /// <summary>
        /// Overide save method
        /// </summary>
        /// <returns></returns>
        public override string Save()
        {
            //Update the state of current venue
            ArcadeIdleVenue currentVenue = ArcadeIdleManager.GetContext<ArcadeIdleManager>().CurrentVenue;
            VenueState venueState = currentVenue.CurrentState;
            venueState.WorkerStates = currentVenue.GetWorkerStates();
            VenueStates[currentVenue.VenueId] = venueState;
            return base.Save();
        }

        public void UpdateVenueState(ArcadeIdleVenue venue)
        {
            if(venue.VenueId.IsNullOrEmpty())
            {
                Debug.LogError("Trying to save a venue state with null or empty id");
                return;
            }
            VenueStates[venue.VenueId] = venue.CurrentState;
            Dirtied = true;
        }

        public VenueState GetVenueState(string venueId)
        {
            if(VenueStates == null || !VenueStates.ContainsKey(venueId)) return null;
            return VenueStates[venueId];
        }
    }
}
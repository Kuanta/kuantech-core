using Kuantech.Core;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class ArcadeIdleActor : Actor
    {
        #region Resource Management
        /// <summary>
        /// Transfers a resource between two actors
        /// </summary>
        /// <param name="from">Actor that sends the resource</param>
        /// <param name="to">Actor that receices the resource</param>
        /// <param name="resourceId">Id of the resource</param>
        /// <param name="flyingResource"></param>
        /// <returns></returns>
        public static bool TransferResource(ResourceInventory from, ResourceInventory to, ResourceData resourceData, bool flyingResource)
        {
            //todo: Implement multiple resource transfering
            if (to == null)
            {
                Debug.LogError("To is null");
                return false;
            }

            if (from == null)
            {
                Debug.LogError("From is null");
                return false;
            }
            if (!to.CanAcceptResource(resourceData) || !from.CanGiveResource(resourceData)) return false;
            
            ResourceVisual visual = from.RemoveResource(resourceData.Id, 1);
            if(flyingResource && visual == null)
            {
                visual = resourceData.GetResourceVisual();
                visual.transform.position = from.transform.position;
                visual.transform.rotation = from.transform.rotation;
            }
            to.AddResource(resourceData, visual, flyingResource);
            return true;
        }
        #endregion

      
    }
}
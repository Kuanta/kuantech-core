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
        public static bool TransferResource(ResourceInventory from, ResourceInventory to, ResourceData resource, bool flyingResource)
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
            if (!to.CanAcceptResource(resource) || !from.CanGiveResource(resource)) return false;
            
            ResourceVisual visual = from.RemoveResource(resource.ResourceId, 1);
            if(flyingResource && visual == null)
            {
                visual = resource.GetResourceVisual();
                visual.transform.position = from.transform.position;
                visual.transform.rotation = from.transform.rotation;
            }
            to.AddResource(resource, visual, flyingResource);
            return true;
        }
        #endregion

      
    }
}
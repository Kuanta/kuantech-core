using System.Collections;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class ResourceSourceNode : ActorModule
    {
        [SerializeField] private ResourceInventory SourceInventory;
        [SerializeField] private ResourceDataReference SuppliedResource;

        [Header("Resource Visual")]
        [SerializeField] private float ThrowRadius = 2.0f;
        [Range(0, 0.9f)]
        [SerializeField] private float RadiusNoiseFactor = 0.0f;
        [SerializeField] private float JumpForce = 5.0f;
        [SerializeField] private float ThrowDuration = 1.0f;
        [SerializeField] private Transform ResourceThrowPoint;

        [Header("Refreshment")]
        public float RefreshTime = 10.0f;
        private float _timeDepleted;
        private bool _depleted = false;
        private IEnumerator _refreshCoroutine;

        public void Interact(ResourceNodePicker picker)
        {
            if(SourceInventory.GetAvailableAmount(SuppliedResource.GetId()) > 0)
            {
                ResourceVisual visual = SourceInventory.RemoveResource(SuppliedResource.GetId(), 1);
                if(visual == null)
                {
                    visual = SuppliedResource.GetResourceVisual();
                }

                if (visual == null)
                {
                    Debug.LogError("Visual is null");
                }
                else
                {
                    visual.transform.position = ResourceThrowPoint.position;
                    visual.transform.rotation = ResourceThrowPoint.rotation;
                    visual.transform.localScale = Vector3.one;
                    float randomAngle = Random.Range(0f, Mathf.PI*2);
                    Vector3 throwDir = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle));
                    WorldPoint targetPoint = new WorldPoint()
                    {   
                        Position = throwDir * (ThrowRadius * Random.Range(1 - RadiusNoiseFactor, 1 + RadiusNoiseFactor)) + transform.position,
                        Rotation = Quaternion.identity,
                    };
                    visual.FlyToTargetWithDoJump(targetPoint, JumpForce, ThrowDuration);
                }
      

                if(SourceInventory.GetAvailableAmount(SuppliedResource.GetId()) <= 0 && _refreshCoroutine == null)
                {
                    Debug.LogError("Started refresh");
                    _depleted = true;
                    _refreshCoroutine = RefreshCoroutine();
                    StartCoroutine(_refreshCoroutine);
                }
            }
        }
        private IEnumerator RefreshCoroutine()
        {
            yield return new WaitForSeconds(RefreshTime);
            SourceInventory.SetDefaultValues(); //Refresh
            _refreshCoroutine = null;
        }
    }
}
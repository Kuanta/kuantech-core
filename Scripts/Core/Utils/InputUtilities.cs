using UnityEngine;

namespace Kuantech.Utils
{
    public static class InputUtilities
    {
        public static Vector3 GetCursorPosition()
        {
            return Input.mousePosition;
        }

        public static WorldPoint GetObjectsUnderCursor3D(Camera camera, float raycastLength, LayerMask rayMask)
        {
            Vector3 screenPos = GetCursorPosition();
            Ray ray = camera.ScreenPointToRay(screenPos);
    
            if(UnityEngine.Physics.Raycast(ray, out RaycastHit hit, raycastLength, rayMask))
            {
                return new WorldPoint()
                {
                    Position = hit.point,
                    Target = hit.transform,
                };
            }

            return null;
        }
    }
}
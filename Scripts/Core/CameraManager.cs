using System;
using System.Collections;
using Cinemachine;
using Kuantech.Core.Camera;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public class CameraDictionary : SerializableDictionary<int, CinemachineVirtualCamera>{}
    public class CameraManager : SubManager
    {
        public KtCamera KtCamera;
        [SerializeField] private CameraDictionary Cameras;
        public int StartingCameraIndex = 0;
        private int _currentCameraId;
        private IEnumerator _switchRoutine;

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            if (Cameras.IsNullOrEmpty()) return;
            SwitchToCamera(StartingCameraIndex);
        }
        
        public void SwitchToCamera(int cameraId)
        {
            if(!Cameras.ContainsKey(cameraId))
            {
                Debug.LogError("Camera with Id:"+cameraId+" doesn't exist");
                return;
            }

            foreach(var pair in Cameras)
            {
                pair.Value.enabled = pair.Key == cameraId;
            }
            _currentCameraId = cameraId;
        }

        public void SwitchToCamera(CinemachineVirtualCamera cameraToSwitch)
        {
            foreach (var pair in Cameras)
            {
                pair.Value.enabled = false;
            }
            cameraToSwitch.enabled = true;
        }

        public void SetTargetForCamera(Transform transform, int cameraId)
        {
            if (!Cameras.ContainsKey(cameraId))
            {
                Debug.LogError("Camera with Id:" + cameraId + " doesn't exist");
                return;
            }

            Cameras[cameraId].Follow = transform;
            Cameras[cameraId].LookAt = transform;
        }

        public void SwitchToCameraForTime(int newCameraId, float duration)
        {
            if(_switchRoutine != null)
            {
                StopCoroutine(_switchRoutine);
            }
            _switchRoutine = SwitchRoutine(_currentCameraId, newCameraId, duration);
            StartCoroutine(_switchRoutine);
        }
        private IEnumerator SwitchRoutine(int cameraId, int cameraToSwitch, float duration)
        {
            SwitchToCamera(cameraToSwitch);
            yield return new WaitForSeconds(duration);
            SwitchToCamera(cameraId);
            _switchRoutine = null;
        }
        public void SwitchToCameraForTime(CinemachineVirtualCamera camera, float duration)
        {
            if (_switchRoutine != null)
            {
                StopCoroutine(_switchRoutine);
            }
            _switchRoutine = SwitchRoutine(_currentCameraId, camera, duration);
            StartCoroutine(_switchRoutine);
        }
        private IEnumerator SwitchRoutine(int cameraId, CinemachineVirtualCamera camera, float duration)
        {
            SwitchToCamera(camera);
            yield return new WaitForSeconds(duration);
            camera.enabled = false;
            SwitchToCamera(cameraId);
            _switchRoutine = null;
        }

        public static KtCamera GetKtCamera()
        {
            var context = GetContext<CameraManager>();
            if (context == null) return null;
            return context.KtCamera;
        }

    }
}


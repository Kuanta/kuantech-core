using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class Runner : MonoBehaviour
    {
        public float Speed = 10f;
        public Vector2 MovementVector = Vector2.zero;
        
        
        public void Initialize()
        {
        }
        private void Update()
        {
            //Check Current Level
            Level currentLevel = ((HCGameManager) HCGameManager.Instance).CurrentLevel;
            if (currentLevel == null || currentLevel.CurrentState != LevelState.Playing)
            {
                MovementVector = Vector2.zero;
                return;
            }
            Vector3 globalDirection = LocalToGlobalDirection(MovementVector);
            transform.position += globalDirection.normalized * (Time.deltaTime * Speed);
        }

        public Vector3 LocalToGlobalDirection(Vector2 localDirection)
        {
            Vector3 localDirection3D = new Vector3(localDirection.x, 0f, localDirection.y);
            Vector3 globalDirection = transform.TransformDirection(localDirection3D);

            return globalDirection;
        }
        
    }
}
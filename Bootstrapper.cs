using UnityEngine;

namespace Kuantech.Core
{
    public class Bootstrapper : MonoBehaviour {
        [SerializeField] private GameManager GameManagerPrefab;

        private void Start()
        {
            if(GameManager.InstanceExists()) return;
            Instantiate(GameManagerPrefab, Vector3.zero, Quaternion.identity);
        }        
    }
}
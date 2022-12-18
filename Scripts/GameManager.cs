namespace Kuantech.Core
{
    public class GameManager : Singleton<GameManager>
    {
        public PrefabPool Pool;

        protected void Awake()
        {
            Pool = new PrefabPool(transform, 1000);
        }

    }
}
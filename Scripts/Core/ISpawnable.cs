namespace Kuantech.Core
{
    public interface ISpawnable
    {
        public void Spawn();
        
        public void Despawn(float delay);
    }
}
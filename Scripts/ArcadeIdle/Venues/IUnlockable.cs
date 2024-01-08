namespace Kuantech.ArcadeIdle
{
    public interface IUnlockable
    {
        /// <summary>
        /// Unlocks the unlockable
        /// </summary>
        public void Unlock();
        /// <summary>
        /// Called to enable or disable unlockable
        /// </summary>
        public void Toggle(bool toggle);
    }
}

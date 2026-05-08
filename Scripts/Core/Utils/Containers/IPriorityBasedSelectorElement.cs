namespace Kuantech.Utils
{
    public interface IPriorityBasedSelectorElement
    {
        public bool CanBeSelected(object userData = null);
    }
}
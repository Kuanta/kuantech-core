namespace Kuantech.Core.UI
{
    public interface IDragHoverable
    {
        void OnDragHoverEnter(UIDragSlot source);
        void OnDragHoverExit();
    }
}

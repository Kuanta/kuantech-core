namespace Kuantech.Core
{
    /// <summary>
    /// Offline-mode stand-in for Unity.Netcode's NetworkVariable&lt;T&gt;, matching just the surface this
    /// project's non-networked build needs (Value get/set, OnValueChanged(previous, current)). Lets modules
    /// declare one field per synced value and keep identical Value/OnValueChanged call sites regardless of
    /// whether NETWORKING_NGO is defined — only the field's declared type switches.
    /// </summary>
    public class OfflineNetworkVariable<T>
    {
        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                T previous = _value;
                _value = value;
                OnValueChanged?.Invoke(previous, value);
            }
        }

        public event System.Action<T, T> OnValueChanged;
    }
}

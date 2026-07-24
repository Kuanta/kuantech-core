namespace Kuantech.Rpg
{
    /// <summary>
    /// A single attribute paired with a computed value — the atom that stat-readout UIs display (weapon
    /// damage per type, armor's stats, affix lines, ...). Just data; whoever produces it decides the value.
    /// </summary>
    public struct AttributeValue
    {
        public AttributeAsset Attribute;
        public float Value;

        public AttributeValue(AttributeAsset attribute, float value)
        {
            Attribute = attribute;
            Value = value;
        }
    }
}

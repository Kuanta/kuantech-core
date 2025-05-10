using Kuantech.Puzzle;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ConnectivityMask))]
public class ConnectivityMaskDrawer : PropertyDrawer
{
    // 3×3 yerleşim tablosu (ortada -1 => kendisi)
    private static readonly int[,] directionGrid = {
        {7, 0, 4},
        {3,-1, 1},
        {6, 2, 5}
    };

    private const float CellSize = 20f;
    private const float Spacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        // Label
        position = EditorGUI.PrefixLabel(position, label);

        float totalWidth  = (CellSize+Spacing)*3 - Spacing;
        float totalHeight = (CellSize+Spacing)*3 - Spacing;
        Rect gridRect = new Rect(position.x, position.y, totalWidth, totalHeight);

        // "mask" alanını bul: "[SerializeField] private ushort mask;"
        SerializedProperty maskProp = property.FindPropertyRelative("mask");
        if (maskProp == null)
        {
            EditorGUI.LabelField(gridRect, "ERROR: 'mask' not found");
            EditorGUI.EndProperty();
            return;
        }
        ushort raw = (ushort)maskProp.intValue;

        // 3×3'ü çiz, tıklandığında 0->1->-1->0
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                int dirIndex = directionGrid[r,c];
                Rect cellRect = new Rect(
                    gridRect.x + c*(CellSize+Spacing),
                    gridRect.y + r*(CellSize+Spacing),
                    CellSize, CellSize
                );
                if (dirIndex < 0)
                {
                    // merkez hücre, kendisi
                    EditorGUI.DrawRect(cellRect, new Color(0.5f,0.5f,0.5f,0.3f));
                    continue;
                }

                // 2 bit
                int shift = 2 * dirIndex;
                int val2 = (raw >> shift) & 0b11; // 0..3
                int triVal = (val2 == 2)? -1 : val2; // -1,0,1

                // Görsel gösterim: X => -1, O => 1, "" => 0
                string text = "";
                if (triVal == 1) text = "O";
                else if (triVal == -1) text = "X";

                if (GUI.Button(cellRect, text))
                {
                    // tıklama => sıradaki duruma
                    if (triVal == 0)      triVal = 1;   // 0->1
                    else if (triVal == 1) triVal = -1;  // 1->-1
                    else if (triVal == -1)triVal = 0;   // -1->0

                    // Geri 2 bit'e yaz
                    int newVal2 = (triVal == -1)? 2 : triVal;
                    raw &= (ushort)~(0b11 << shift);
                    raw |= (ushort)((newVal2 & 0b11) << shift);
                }
            }
        }

        // Güncellenen raw değeri kaydet
        maskProp.intValue = raw;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight + (CellSize+Spacing)*3;
    }
}

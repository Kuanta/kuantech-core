using System;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [Serializable]
    public struct ConnectivityMask
    {
        [SerializeField]
        private ushort mask;

        /// <summary>
        /// Üç durumlu değeri döndürür:
        ///   -1 => "no tile"
        ///    0 => "don't care"
        ///    1 => "tile"
        /// </summary>
        public int Get(GridBoard.Directions dir)
        {
            int shift = 2 * (int)dir;
            // 0..3
            int val2 = (mask >> shift) & 0b11;
            if (val2 == 2)
                return -1;  // no tile
            // 0 => 0 (don't care), 1 => 1 (tile), 3 => da rezerve
            return val2;
        }

        /// <summary>
        /// Üç durumlu değeri set eder:
        ///   value = -1 => "no tile"
        ///   value =  0 => "don't care"
        ///   value =  1 => "tile"
        /// </summary>
        public void Set(GridBoard.Directions dir, int value)
        {
            int shift = 2 * (int)dir;
            // -1 => 2 bit "2"
            int val2 = (value == -1) ? 2 : value;
            if (val2 < 0) val2 = 0;
            if (val2 > 2) val2 = 2;

            // Eski 2 bit'i temizle
            mask &= (ushort)~(0b11 << shift);
            // Yeni değer
            mask |= (ushort)((val2 & 0b11) << shift);
        }
        
        public void Set(GridBoard.Directions dir, bool value)
        {
            Set(dir, value ? 1 : 0);
        }
        
        public bool Equals(bool[] connectivityArray)
        {
            if (connectivityArray == null || connectivityArray.Length != 8)
                return false;

            for (int i = 0; i < 8; i++)
            {
                // TriConnectivityMask'ta:
                // Get(dir) => -1 => no tile, 0 => don't care, 1 => tile
                int triVal = Get((GridBoard.Directions)i); 
                // bool array
                bool actual = connectivityArray[i];

                switch (triVal)
                {
                    case 0: // don't care
                        // her şeyi kabul ediyoruz
                        break;
                    case 1: // tile
                        if (!actual)
                            return false; // beklenen true, ama actual=false => uymaz
                        break;
                    case -1: // no tile
                        if (actual)
                            return false; // beklenen false, ama actual=true => uymaz
                        break;
                }
            }

            return true;
        }
        
        public readonly bool Equals(ConnectivityMask other)
        {
            for (int i = 0; i < 8; i++)
            {
                int myVal = Get((GridBoard.Directions)i);     // -1,0,1
                int otherVal = other.Get((GridBoard.Directions)i); // -1,0,1

                // "0 => don't care" => bu yönde herhangi bir uyuşmazlığa bakma
                if (myVal == 0 || otherVal == 0)
                    continue;

                // ikisi de "tile" veya "no tile" belirtmişse, eşleşmeli
                // (myVal, otherVal) ∈ {1, -1}
                if (myVal != otherVal)
                    return false;
            }
            return true;
        }
        
        public ushort RawMask
        {
            get => mask;
            set => mask = value;
        }

        public bool[] GetEightConnectivity()
        {
            bool[] eightConnectivity = new bool[8];
            eightConnectivity[(int)GridBoard.Directions.TopLeft] = Get(GridBoard.Directions.TopLeft) > 0;
            eightConnectivity[(int)GridBoard.Directions.Top] = Get(GridBoard.Directions.Top) > 0;
            eightConnectivity[(int)GridBoard.Directions.TopRight] = Get(GridBoard.Directions.TopRight) > 0;
            eightConnectivity[(int)GridBoard.Directions.Left] = Get(GridBoard.Directions.Left) > 0;
            eightConnectivity[(int)GridBoard.Directions.Right] = Get(GridBoard.Directions.Right) > 0;
            eightConnectivity[(int)GridBoard.Directions.BottomLeft] = Get(GridBoard.Directions.BottomLeft) > 0;
            eightConnectivity[(int)GridBoard.Directions.Bottom] = Get(GridBoard.Directions.Bottom) > 0;
            eightConnectivity[(int)GridBoard.Directions.BottomRight] = Get(GridBoard.Directions.BottomRight) > 0;
            return eightConnectivity;
        }
        public override string ToString()
        {
            // 16 bit'i binary string olarak görmek için
            return Convert.ToString(mask, 2).PadLeft(16, '0');
        }
        
        public static ConnectivityMask GetFromDirection(Vector2Int offset)
        {
            var mask = new ConnectivityMask();

            // Varsayılan olarak her yön "don't care" (yani 0)
            for (int i = 0; i < 8; i++)
            {
                mask.Set((GridBoard.Directions)i, 0);
            }

            // Offset (0, 0) ise hiçbir yönü aktif yapma
            if (offset == Vector2Int.zero)
                return mask;

            // (x,y) -> direction eşlemesi
            if (offset == new Vector2Int(0, 1))
                mask.Set(GridBoard.Directions.Top, 1);
            else if (offset == new Vector2Int(1, 0))
                mask.Set(GridBoard.Directions.Right, 1);
            else if (offset == new Vector2Int(0, -1))
                mask.Set(GridBoard.Directions.Bottom, 1);
            else if (offset == new Vector2Int(-1, 0))
                mask.Set(GridBoard.Directions.Left, 1);
            else if (offset == new Vector2Int(1, 1))
                mask.Set(GridBoard.Directions.TopRight, 1);
            else if (offset == new Vector2Int(1, -1))
                mask.Set(GridBoard.Directions.BottomRight, 1);
            else if (offset == new Vector2Int(-1, -1))
                mask.Set(GridBoard.Directions.BottomLeft, 1);
            else if (offset == new Vector2Int(-1, 1))
                mask.Set(GridBoard.Directions.TopLeft, 1);

            return mask;
        }
    }
}
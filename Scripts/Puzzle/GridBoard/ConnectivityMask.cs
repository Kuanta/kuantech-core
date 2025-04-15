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
        
        public bool Equals(ConnectivityMask other)
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

        public override string ToString()
        {
            // 16 bit'i binary string olarak görmek için
            return Convert.ToString(mask, 2).PadLeft(16, '0');
        }
    }
}
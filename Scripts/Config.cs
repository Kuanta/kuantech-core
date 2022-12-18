using System;

namespace Kuantech
{
    [Serializable]
    public static class Config
    {
        public const float LEVEL_TO_HEALTH_FACTOR = 1f;
        public const float LEVEL_TO_ENERGY_FACTOR = 1f;

        public const float DEFAULT_LEVEL_TO_STAT_FACTOR = 1f;

        public const float LEVEL_FORMULA_X = 0.3f;
        public const float LEVEL_FORMULA_Y = 2;

        public const float MAX_ENCUMBRANCE = 10f;
    }
}
using System;

namespace Kuantech
{
    [Serializable]
    public static class Config
    {
        //Game parmeters
        public const float LEVEL_SPEED = 5;
        public const int ACTIVE_CHUNK_COUNT = 5;
        public const float MELEE_ENEMY_TO_PLAYER_MIN_DIST = 2f;
        public const float GLOBAL_COOLDOWN_TIME = 1f;
        
        //Rpg elements
        public const float LEVEL_TO_HEALTH_FACTOR = 1f;
        public const float LEVEL_TO_ENERGY_FACTOR = 1f;

        public const float DEFAULT_LEVEL_TO_STAT_FACTOR = 1f;

        public const float LEVEL_FORMULA_X = 0.3f;
        public const float LEVEL_FORMULA_Y = 2;

        public const float MAX_ENCUMBRANCE = 10f;
        
        //Armor
        public const float ARMOR_POWER_COEFF = 1 / 1.3f;
        public const float ARMOR_COEFF = 1;
        
        //Economics
        public const float ITEM_UPGRADE_COST_COEFF = 20;
        public const float ITEM_SELL_VALUE_COEFF = 0.2f;
    }
}
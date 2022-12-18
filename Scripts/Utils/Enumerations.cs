using System;

namespace Kuantech.Data
{
    public class Enums
    {
        public enum Genders
        {
            Male,
            Female,
        }

        public enum Races
        {
            Human,
        }
        
        public enum VisualFields
        {
            BodyType, FaceType, HairType, GenderType, EyebrowType, BeardType,
        }
        
        public enum BoneTypes
        {
            HairBone, EyeBrowBone, BeardBone, //Visuals
            HeadBone, ChestBone, ShoulderBone, ArmsBone, MainHandBone, OffHandBone, BeltBone, LegsBone, FeetBone, BackBone, RootBone, //Equipments
        }
        public enum EquipmentSlotType
        {
            None = -1,
            Head = 0,
            MainHand,
            OffHand,
            Chest,
            Legs,
            Feet,
            Arms,
            Shoulders,
            Back,
            Ring,
        }

        [Serializable]
        public enum ItemType
        {
            Default = -1,
            Weapon,
            Armor,
            Trinket,
            Consumable,
        }

        [Serializable]
        public enum WeaponType
        {
            OneHanded,
            TwoHanded,
            Bow,
            Staff,
            Shield,
        }
        public enum MovementState
        {
            Walking,
            Running,
            Sprinting,
        }

        public enum Directions
        {
            UP,
            DOWN,
            LEFT,
            RIGHT,
        }
    }
}
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    [CreateAssetMenu(fileName = "Damage Reduction Formula", menuName = "Kuantech/Core/Combat/Damage Reduction Formula")]
    public class DamageReductionFormula : ScriptableObject
    {
        [SerializeField] private float DamageReductionFactor = 10;
        
        [Button("Get Damage Multiplier")]
        public float GetDamageMultiplier(float resistanceValue)
        {
            return  Mathf.Exp(-resistanceValue * DamageReductionFactor);
        }
    }
}
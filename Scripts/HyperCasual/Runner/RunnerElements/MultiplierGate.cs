using System.Collections.Generic;
using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public enum GateOperation
    {
        None = 0,
        Addition,
        Multiplication,
    }

    public class MultiplierGate : Pickupable
    {
        [SerializeField] private GateOperation Operation;
        [SerializeField] private float Value;

    

        [Header("Visuals")]
        [SerializeField] private Color PositiveColor;
        [SerializeField] private Color NegativeColor;
        [SerializeField] private List<Renderer> Renderers;
        [SerializeField] private TMP_Text ValueText;
        public override void Spawn()
        {
            bool isPositive = (Value > 0 && Operation == GateOperation.Addition)
            || (Value >= 1 && Operation == GateOperation.Multiplication);

            //Handle Color
            foreach(var renderer in Renderers)
            {
                renderer.material.SetColor("_BaseColor", isPositive ? PositiveColor : NegativeColor);
            }
            
            string pre = "";
            if(Operation == GateOperation.Multiplication && Value >=1)
            {
                pre = "x";
            }else if(Operation == GateOperation.Multiplication && Value < 1)
            {
                pre = "%";
            }
            else if(Operation == GateOperation.Addition && Value > 0)
            {
                pre = "+";
            }
            ValueText.text = $"{pre}{Value.Stringfy()}";
        }

        protected override void OnPickup(Collider other)
        {
            if(other.gameObject.TryGetComponent(out Runner runner))
            {
                runner.OnMultiplicationGate(Operation, Value);
                base.OnPickup(other);
            }
        }
    }
}

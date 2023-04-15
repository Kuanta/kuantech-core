using System;
using DG.Tweening;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.ActionSequencer
{
    [Serializable]
    public class RotateAction : SequenceAction
    {
        [SerializeField] private float TargetForwardAngle = 0f;
        [SerializeField] private float Duration = 1f;
        private Tween _tween;
        public string TargetVariableName = "Target";
        private Actor _target;
        public override void Execute()
        {
            base.Execute();
            if(_tween != null) _tween.Kill();
            if (Sequencer == null)
            {
                _target = null;
            }

            if (Sequencer != null)
            {
                _target = Sequencer.VariableTable.GetVariable<Actor>(TargetVariableName);
            }

            Vector3 eulerAngles;
            if (_target == null)
            {
                eulerAngles = new Vector3(0, TargetForwardAngle, 0);
            }
            else
            {
                eulerAngles = _target.transform.rotation.eulerAngles;
                eulerAngles.x = 0;
                eulerAngles.z = 0;
                eulerAngles.y += Mathf.PI * Mathf.Rad2Deg + TargetForwardAngle; //Look at opposit direction
            }
            
            _tween = Parent.transform.DOLocalRotate(eulerAngles, Duration).OnComplete((() =>
            {
                IsComplete = true;
            }));
        }
        
    }
}
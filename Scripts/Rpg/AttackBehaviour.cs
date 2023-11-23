using Kuantech.Rpg;
using UnityEngine;

public class AttackBehaviour : StateMachineBehaviour
{
    public string TargetTimeKey;
    private bool _multiplierCalculated = false;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(_multiplierCalculated) return;
        float targetTime = animator.GetFloat(TargetTimeKey);
        if (targetTime == 0f) targetTime = 1f;
        float baseAnimLength = GetBaseAnimationLength(stateInfo);
        float speedMultiplier = baseAnimLength / targetTime;
        animator.SetFloat(AnimatorModule.AttackSpeed, speedMultiplier);
        _multiplierCalculated = true;
    }
    
    private float GetBaseAnimationLength(AnimatorStateInfo stateInfo)
    {
        return stateInfo.length * stateInfo.speedMultiplier;
    }
    
    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
      
    }

    //OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _multiplierCalculated=false;
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
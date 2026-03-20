using UnityEngine;

/// <summary>
/// Scales an animator state's speed so the clip plays in exactly TargetTimeKey seconds.
/// Add to any state that needs duration-driven timing (attacks, dodges, casts, etc.).
/// The state must have its Speed set to the SpeedKey parameter.
/// </summary>
public class AnimationTimeScaler : StateMachineBehaviour
{
    [Tooltip("Animator float that holds the desired duration in seconds")]
    public string TargetTimeKey = "TargetTime";
    [Tooltip("Animator float that the state's Speed is multiplied by")]
    public string SpeedKey = "AttackSpeed";

    private bool _calculated;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_calculated) return;
        float targetTime = animator.GetFloat(TargetTimeKey);
        if (targetTime <= 0f) targetTime = 1f;
        float clipLength = stateInfo.length * stateInfo.speedMultiplier;
        animator.SetFloat(SpeedKey, clipLength / targetTime);
        _calculated = true;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _calculated = false;
    }
}

/// <summary>
/// Legacy alias — kept so existing animator states don't break.
/// New states should use AnimationTimeScaler directly.
/// </summary>
public class AttackBehaviour : AnimationTimeScaler { }
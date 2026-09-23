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

    [Tooltip("Floor for the computed speed. 0 (the default) means no floor: the clip is stretched as far as " +
             "it takes to fill the target duration.\n\n" +
             "Only matters for a Start -> Hold(loop) -> Release windup, where whatever the clip does not " +
             "cover is absorbed by the loop. The trade is between two bad extremes. Stretched too far a clip " +
             "reads as slow motion; but a windup that finishes early leaves the actor sitting in a FROZEN " +
             "pose, and a frozen pose tells the player far less than a slow one does -- a windup exists to " +
             "be read, not to be pretty. Since the loop shows no motion at all, err towards stretching.\n\n" +
             "1 = never slow down (the clip always plays as authored, the loop covers the rest). 0.67 = " +
             "stretch up to 1.5x and let the loop take what is left. Speeding UP is never clamped: a windup " +
             "shorter than the clip genuinely has to fit.")]
    [Range(0f, 1f)]
    public float MinSpeed;

    private bool _calculated;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_calculated) return;
        float targetTime = animator.GetFloat(TargetTimeKey);
        if (targetTime <= 0f) targetTime = 1f;
        float clipLength = stateInfo.length * stateInfo.speedMultiplier;
        float speed = Mathf.Max(MinSpeed, clipLength / targetTime);
        animator.SetFloat(SpeedKey, speed);
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
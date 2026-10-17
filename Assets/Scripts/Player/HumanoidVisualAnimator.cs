using UnityEngine;

public sealed class HumanoidVisualAnimator : MonoBehaviour
{
    [SerializeField, Min(0f)] private float turnSpeed = 12f;
    [SerializeField, Min(0f)] private float gaitSpeed = 10f;
    [SerializeField, Range(0f, 45f)] private float strideDegrees = 22f;

    private CharacterController actor;
    private Transform leftArm;
    private Transform rightArm;
    private Transform leftLeg;
    private Transform rightLeg;

    private void Awake()
    {
        actor = GetComponentInParent<CharacterController>();
        leftArm = transform.Find("LeftArm");
        rightArm = transform.Find("RightArm");
        leftLeg = transform.Find("LeftLeg");
        rightLeg = transform.Find("RightLeg");
    }

    private void LateUpdate()
    {
        if (actor == null)
        {
            return;
        }

        var velocity = actor.velocity;
        var horizontal = new Vector3(velocity.x, 0f, velocity.z);
        var movement = Mathf.Clamp01(horizontal.magnitude / 3f);
        if (horizontal.sqrMagnitude > 0.01f)
        {
            var worldFacing = Quaternion.LookRotation(horizontal, Vector3.up);
            var localFacing = Quaternion.Inverse(transform.parent.rotation) * worldFacing;
            var turnFraction = 1f - Mathf.Exp(-turnSpeed * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, localFacing, turnFraction);
        }

        var stride = Mathf.Sin(Time.time * gaitSpeed) * strideDegrees * movement;
        RotateLimb(leftLeg, stride);
        RotateLimb(rightLeg, -stride);
        RotateLimb(leftArm, -stride * 0.65f);
        RotateLimb(rightArm, stride * 0.65f);
    }

    private static void RotateLimb(Transform limb, float degrees)
    {
        if (limb != null)
        {
            limb.localRotation = Quaternion.Euler(degrees, 0f, 0f);
        }
    }
}

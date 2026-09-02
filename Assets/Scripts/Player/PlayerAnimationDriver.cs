using UnityEngine;

namespace DreamChase
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        const float FlyingSpeedDisplay = 3f;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
        static readonly int IsFlyingHash = Animator.StringToHash("IsFlying");
        static readonly int JumpHash = Animator.StringToHash("Jump");

        Animator _animator;
        PlayerMotor _motor;

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _motor = GetComponent<PlayerMotor>();
        }

        void LateUpdate()
        {
            float speed = _motor.Phase == GamePhase.Flying
                ? FlyingSpeedDisplay
                : Mathf.Abs(_motor.Velocity.x);

            _animator.SetFloat(SpeedHash, speed);
            _animator.SetBool(IsGroundedHash, _motor.IsGrounded);
            _animator.SetBool(IsRunningHash, _motor.IsRunning);
            _animator.SetBool(IsFlyingHash, _motor.Phase == GamePhase.Flying);

            if (_motor.ConsumeJumpPulse())
                _animator.SetTrigger(JumpHash);
        }
    }
}

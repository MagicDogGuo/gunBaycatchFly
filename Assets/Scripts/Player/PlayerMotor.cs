using UnityEngine;

namespace DreamChase
{
    [DefaultExecutionOrder(0)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        const float GroundStickVelocity = -2f;
        const float MoveDeadzone = 0.01f;

        [SerializeField] float walkSpeed = 3f;
        [SerializeField] float runSpeed = 6f;
        [SerializeField] float jumpHeight = 1.5f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float flySpeed = 4f;
        [SerializeField] float groundedSkin = 0.08f;
        [SerializeField] float lockedZ = 0f;

        CharacterController _controller;
        PlayerInputReader _inputReader;
        Vector3 _velocity;
        Vector3 _baseScale;
        bool _jumpPulse;
        bool _isGrounded;

        public bool IsGrounded { get { return _isGrounded; } }
        public bool IsMoving { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsJumping { get; private set; }
        public int FacingSign { get; private set; }
        public GamePhase Phase { get; private set; }
        public Vector3 Velocity { get { return _velocity; } }

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _inputReader = GetComponent<PlayerInputReader>();
            _baseScale = transform.localScale;
            _baseScale.x = Mathf.Abs(_baseScale.x);
            FacingSign = 1;
            Phase = GamePhase.Ground;
        }

        void Start()
        {
            _controller.Move(Vector3.down * groundedSkin);
            RefreshGrounded();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            PlayerInputSnapshot input = _inputReader.Sample();

            switch (Phase)
            {
                case GamePhase.Ground:
                    TickGround(input, dt);
                    break;
                case GamePhase.Transforming:
                    TickTransforming();
                    break;
                case GamePhase.Flying:
                    TickFlying(input, dt);
                    break;
            }

            LockZ();
        }

        public void EnterTransforming()
        {
            Phase = GamePhase.Transforming;
            _velocity = Vector3.zero;
            IsMoving = false;
            IsRunning = false;
            IsJumping = false;
        }

        public void EnterFlying()
        {
            Phase = GamePhase.Flying;
            _velocity = Vector3.zero;
            _isGrounded = false;
            IsJumping = false;
            IsRunning = false;
        }

        public bool ConsumeJumpPulse()
        {
            if (!_jumpPulse)
                return false;

            _jumpPulse = false;
            return true;
        }

#if UNITY_EDITOR
        public void EditorApplyGroundInput(PlayerInputSnapshot input, float dt)
        {
            TickGround(input, dt);
            LockZ();
        }
#endif

        void TickGround(PlayerInputSnapshot input, float dt)
        {
            RefreshGrounded();

            float moveX = input.Move.x;
            bool wantsRun = input.RunHeld && Mathf.Abs(moveX) > MoveDeadzone;
            float speed = wantsRun ? runSpeed : walkSpeed;
            _velocity.x = moveX * speed;

            if (_isGrounded && input.JumpPressed)
            {
                _velocity.y = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
                _isGrounded = false;
                _jumpPulse = true;
            }
            else if (_isGrounded)
            {
                if (_velocity.y < 0f)
                    _velocity.y = GroundStickVelocity;
            }
            else
            {
                _velocity.y += gravity * dt;
            }

            _controller.Move(_velocity * dt);
            RefreshGrounded();

            IsMoving = Mathf.Abs(_velocity.x) > MoveDeadzone;
            IsRunning = wantsRun && _isGrounded;
            IsJumping = !_isGrounded;
            ApplyFacing(moveX);
        }

        void TickTransforming()
        {
            _velocity = Vector3.zero;
            _controller.Move(Vector3.zero);
            IsMoving = false;
            IsRunning = false;
            IsJumping = false;
        }

        void TickFlying(PlayerInputSnapshot input, float dt)
        {
            Vector2 move = input.Move;
            _velocity.x = move.x * flySpeed;
            _velocity.y = move.y * flySpeed;
            _controller.Move(_velocity * dt);

            _isGrounded = false;
            IsMoving = move.sqrMagnitude > MoveDeadzone * MoveDeadzone;
            IsRunning = false;
            IsJumping = false;
            ApplyFacing(move.x);
        }

        void RefreshGrounded()
        {
            _isGrounded = _controller.isGrounded;
        }

        void ApplyFacing(float moveX)
        {
            if (Mathf.Abs(moveX) <= MoveDeadzone)
                return;

            FacingSign = moveX > 0f ? 1 : -1;
            Vector3 scale = _baseScale;
            scale.x = _baseScale.x * FacingSign;
            transform.localScale = scale;
        }

        void LockZ()
        {
            Vector3 position = transform.position;
            if (Mathf.Abs(position.z - lockedZ) <= 0.0001f)
                return;

            position.z = lockedZ;
            transform.position = position;
        }
    }
}

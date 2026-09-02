using UnityEngine;

namespace DreamChase
{
    [DisallowMultipleComponent]
    public sealed class ButterflyController : MonoBehaviour
    {
        const float ArriveEpsilon = 0.001f;

        [SerializeField] Transform retreatTarget;
        [SerializeField] Vector3 retreatWorldPosition = new Vector3(8f, 1.7f, 0f);
        [SerializeField] float retreatSpeed = 2.5f;
        [SerializeField] bool idleOnly = true;

        Animator _animator;
        Rigidbody _body;
        bool _retreating;
        bool _retreatRequested;
        float _targetX;

        public bool IsRetreating { get { return _retreating; } }
        public bool HasReachedRetreatPoint { get { return _retreatRequested && !_retreating; } }

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _body = GetComponent<Rigidbody>();
            if (idleOnly && _animator != null)
                _animator.speed = 1f;
        }

        void FixedUpdate()
        {
            TickRetreat(Time.fixedDeltaTime);
        }

        public void RetreatToPoint()
        {
            _targetX = retreatTarget != null ? retreatTarget.position.x : retreatWorldPosition.x;
            _retreatRequested = true;
            _retreating = true;
        }

#if UNITY_EDITOR
        public void EditorTickRetreat(float dt)
        {
            if (!_retreatRequested)
                RetreatToPoint();

            TickRetreat(dt);
        }
#endif

        void TickRetreat(float dt)
        {
            if (!_retreating)
                return;

            Vector3 position = transform.position;
            position.x = Mathf.MoveTowards(position.x, _targetX, retreatSpeed * dt);
            ApplyPosition(position);

            if (Mathf.Abs(position.x - _targetX) <= ArriveEpsilon)
                _retreating = false;
        }

        void ApplyPosition(Vector3 position)
        {
            transform.position = position;
            if (_body == null)
                return;

            _body.position = position;
            _body.MovePosition(position);
        }
    }
}

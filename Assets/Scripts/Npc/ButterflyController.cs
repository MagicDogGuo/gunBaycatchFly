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
        [SerializeField] bool flyOnPlay = true;

        Animator _animator;
        Collider _collider;
        Rigidbody _body;
        bool _flying;
        bool _flyRequested;
        bool _caught;
        Vector3 _targetPosition;

        public bool IsRetreating { get { return _flying; } }
        public bool HasReachedRetreatPoint { get { return _flyRequested && !_flying && !_caught; } }

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _collider = GetComponent<Collider>();
            _body = GetComponent<Rigidbody>();
            if (idleOnly && _animator != null)
                _animator.speed = 1f;
        }

        void Start()
        {
            if (flyOnPlay)
                FlyToPoint();
        }

        void FixedUpdate()
        {
            TickFlight(Time.fixedDeltaTime);
        }

        public void FlyToPoint()
        {
            if (_caught)
                return;

            _targetPosition = retreatTarget != null ? retreatTarget.position : retreatWorldPosition;
            _targetPosition.z = transform.position.z;
            _flyRequested = true;
            _flying = true;
        }

        public void RetreatToPoint()
        {
            FlyToPoint();
        }

        public void OnCaught()
        {
            _caught = true;
            _flying = false;
            if (_collider != null)
                _collider.enabled = false;
        }

#if UNITY_EDITOR
        public void EditorTickRetreat(float dt)
        {
            if (!_flyRequested)
                FlyToPoint();

            TickFlight(dt);
        }

        public void EditorResetAndFly(Vector3 startPosition)
        {
            _caught = false;
            if (_collider != null)
                _collider.enabled = true;

            ApplyPosition(startPosition);
            FlyToPoint();
        }
#endif

        void TickFlight(float dt)
        {
            if (!_flying)
                return;

            Vector3 position = transform.position;
            Vector3 target = _targetPosition;
            target.z = position.z;
            position = Vector3.MoveTowards(position, target, retreatSpeed * dt);
            ApplyPosition(position);

            if ((position - target).sqrMagnitude <= ArriveEpsilon * ArriveEpsilon)
                _flying = false;
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

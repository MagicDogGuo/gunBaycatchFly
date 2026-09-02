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
        bool _retreating;
        float _targetX;

        public bool IsRetreating { get { return _retreating; } }

        void Awake()
        {
            _animator = GetComponent<Animator>();
            if (idleOnly && _animator != null)
                _animator.speed = 1f;
        }

        void Update()
        {
            if (!_retreating)
                return;

            Vector3 position = transform.position;
            position.x = Mathf.MoveTowards(position.x, _targetX, retreatSpeed * Time.deltaTime);
            transform.position = position;

            if (Mathf.Abs(position.x - _targetX) <= ArriveEpsilon)
                _retreating = false;
        }

        public void RetreatToPoint()
        {
            _targetX = retreatTarget != null ? retreatTarget.position.x : retreatWorldPosition.x;
            _retreating = true;
        }
    }
}

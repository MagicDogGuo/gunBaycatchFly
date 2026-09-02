using UnityEngine;

namespace DreamChase
{
    [DisallowMultipleComponent]
    public sealed class PlayerCatchTrigger : MonoBehaviour
    {
        const string ButterflyTag = "Butterfly";
        const int OverlapBufferSize = 8;

        [SerializeField] GameDirector gameDirector;

        readonly Collider[] _overlapHits = new Collider[OverlapBufferSize];

        PlayerMotor _motor;
        CapsuleCollider _capsule;
        int _butterflyMask;

        void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            if (_motor == null)
                _motor = GetComponentInParent<PlayerMotor>();

            _capsule = GetComponent<CapsuleCollider>();
            _butterflyMask = LayerMask.GetMask("Butterfly");
            if (gameDirector == null)
                gameDirector = FindObjectOfType<GameDirector>();
        }

        void LateUpdate()
        {
            TryCatchOverlap();
        }

        void OnTriggerEnter(Collider other)
        {
            TryBegin(other);
        }

        void TryCatchOverlap()
        {
            if (!enabled || _motor == null || _motor.Phase != GamePhase.Ground || _capsule == null)
                return;

            Vector3 worldCenter = transform.TransformPoint(_capsule.center);
            float scaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
            float scaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
            float radius = _capsule.radius * scaleX;
            float height = Mathf.Max(_capsule.height * scaleY, radius * 2f);
            float half = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 up = transform.up;
            Vector3 point0 = worldCenter + up * half;
            Vector3 point1 = worldCenter - up * half;

            int hitCount = Physics.OverlapCapsuleNonAlloc(
                point0,
                point1,
                radius,
                _overlapHits,
                _butterflyMask,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
                TryBegin(_overlapHits[i]);
        }

        void TryBegin(Collider other)
        {
            if (!enabled || other == null || !other.CompareTag(ButterflyTag))
                return;

            if (_motor == null || _motor.Phase != GamePhase.Ground)
                return;

            if (gameDirector == null)
                gameDirector = FindObjectOfType<GameDirector>();

            if (gameDirector == null)
            {
                Debug.LogError("GameDirector was not found.");
                return;
            }

            ButterflyController butterfly = other.GetComponent<ButterflyController>();
            if (butterfly == null)
                butterfly = other.GetComponentInParent<ButterflyController>();

            enabled = false;
            gameDirector.BeginTransform(_motor, butterfly);
        }
    }
}

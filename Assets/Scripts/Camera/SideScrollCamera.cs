using UnityEngine;

namespace DreamChase
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class SideScrollCamera : MonoBehaviour
    {
        const float ViewportEpsilon = 0.0001f;

        [SerializeField] Transform target;
        [SerializeField] float leftFollowNormalizedX = 0.28f;
        [SerializeField] float rightFollowNormalizedX = 0.72f;
        [SerializeField] float lookAheadDamping = 0.15f;
        [SerializeField] bool neverScrollLeft = false;

        Camera _camera;
        float _lockedY;
        float _lockedZ;
        float _smoothVelocity;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            CacheLockedAxes();
        }

        void LateUpdate()
        {
            FollowTarget(Time.deltaTime);
        }

        public void FollowTarget(float dt)
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            if (target == null || _camera == null)
            {
                LockAxes();
                return;
            }

            Vector3 viewport = _camera.WorldToViewportPoint(target.position);
            float currentX = transform.position.x;
            float desiredX = currentX;
            float leftX = Mathf.Clamp(leftFollowNormalizedX, 0f, rightFollowNormalizedX);
            float rightX = Mathf.Clamp(rightFollowNormalizedX, leftX, 1f);

            bool inFrontOfCamera = viewport.z > 0f;
            float followNormalizedX = -1f;
            if (inFrontOfCamera && viewport.x > rightX)
                followNormalizedX = rightX;
            else if (inFrontOfCamera && !neverScrollLeft && viewport.x < leftX)
                followNormalizedX = leftX;

            if (followNormalizedX >= 0f)
            {
                Vector3 thresholdWorld;
                if (TryViewportWorldX(followNormalizedX, target.position.z, out thresholdWorld))
                    desiredX = currentX + (target.position.x - thresholdWorld.x);
            }
            else
            {
                _smoothVelocity = 0f;
            }

            if (neverScrollLeft)
                desiredX = Mathf.Max(desiredX, currentX);

            float newX = currentX;
            if (Mathf.Abs(desiredX - currentX) > ViewportEpsilon)
            {
                if (lookAheadDamping <= ViewportEpsilon)
                    newX = desiredX;
                else
                    newX = Mathf.SmoothDamp(currentX, desiredX, ref _smoothVelocity, lookAheadDamping, Mathf.Infinity, dt);
            }
            else
            {
                _smoothVelocity = 0f;
            }

            transform.position = new Vector3(newX, _lockedY, _lockedZ);
        }

#if UNITY_EDITOR
        public void EditorFollowNow(float dt)
        {
            FollowTarget(dt);
        }
#endif

        void CacheLockedAxes()
        {
            Vector3 position = transform.position;
            _lockedY = position.y;
            _lockedZ = position.z;
        }

        void LockAxes()
        {
            Vector3 position = transform.position;
            if (Mathf.Abs(position.y - _lockedY) <= ViewportEpsilon && Mathf.Abs(position.z - _lockedZ) <= ViewportEpsilon)
                return;

            position.y = _lockedY;
            position.z = _lockedZ;
            transform.position = position;
        }

        bool TryViewportWorldX(float normalizedX, float worldZ, out Vector3 worldPoint)
        {
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, worldZ));
            Ray ray = _camera.ViewportPointToRay(new Vector3(normalizedX, 0.5f, 0f));
            float enter;
            if (!plane.Raycast(ray, out enter) || enter <= 0f)
            {
                worldPoint = Vector3.zero;
                return false;
            }

            worldPoint = ray.GetPoint(enter);
            return true;
        }
    }
}

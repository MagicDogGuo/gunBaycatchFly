using UnityEngine;

namespace DreamChase
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ViewportBounds : MonoBehaviour
    {
        const float MinSpan = 0.0001f;

        Camera _camera;

        public Vector3 MinWorld { get; private set; }
        public Vector3 MaxWorld { get; private set; }

        void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        public bool TryGetWorldRect(float worldZ, out Vector3 min, out Vector3 max)
        {
            min = Vector3.zero;
            max = Vector3.zero;
            if (!TryRefreshWorldRect(worldZ))
                return false;

            min = MinWorld;
            max = MaxWorld;
            return true;
        }

        public Vector3 Clamp(Vector3 worldPos, Vector2 padding)
        {
            return Clamp(worldPos, padding, true, true, true, true);
        }

        public Vector3 Clamp(
            Vector3 worldPos,
            Vector2 padding,
            bool clampXMin,
            bool clampXMax,
            bool clampYMin,
            bool clampYMax)
        {
            if (!TryRefreshWorldRect(worldPos.z))
                return worldPos;

            float minX = MinWorld.x + padding.x;
            float maxX = MaxWorld.x - padding.x;
            if (minX > maxX)
                minX = maxX = (MinWorld.x + MaxWorld.x) * 0.5f;

            float minY = MinWorld.y + padding.y;
            float maxY = MaxWorld.y - padding.y;
            if (minY > maxY)
                minY = maxY = (MinWorld.y + MaxWorld.y) * 0.5f;

            if (clampXMin)
                worldPos.x = Mathf.Max(worldPos.x, minX);
            if (clampXMax)
                worldPos.x = Mathf.Min(worldPos.x, maxX);
            if (clampYMin)
                worldPos.y = Mathf.Max(worldPos.y, minY);
            if (clampYMax)
                worldPos.y = Mathf.Min(worldPos.y, maxY);

            return worldPos;
        }

        public bool TryClampController(
            CharacterController controller,
            bool clampXMin,
            bool clampXMax,
            bool clampYMin,
            bool clampYMax)
        {
            if (controller == null)
                return false;

            if (!TryRefreshWorldRect(controller.transform.position.z))
                return false;

            Bounds bounds = controller.bounds;
            Vector3 delta = Vector3.zero;

            if (clampXMin && bounds.min.x < MinWorld.x)
                delta.x += MinWorld.x - bounds.min.x;
            if (clampXMax && bounds.max.x > MaxWorld.x)
                delta.x += MaxWorld.x - bounds.max.x;
            if (clampYMin && bounds.min.y < MinWorld.y)
                delta.y += MinWorld.y - bounds.min.y;
            if (clampYMax && bounds.max.y > MaxWorld.y)
                delta.y += MaxWorld.y - bounds.max.y;

            if (delta.sqrMagnitude <= MinSpan * MinSpan)
                return false;

            controller.Move(delta);
            return true;
        }

        public static Vector2 PaddingFrom(CharacterController controller)
        {
            if (controller == null)
                return Vector2.zero;

            Transform t = controller.transform;
            float worldRadius = controller.radius * Mathf.Abs(t.lossyScale.x);
            float worldHalfHeight = controller.height * 0.5f * Mathf.Abs(t.lossyScale.y);
            return new Vector2(worldRadius, worldHalfHeight);
        }

        bool TryRefreshWorldRect(float worldZ)
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            if (_camera == null)
                return false;

            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, worldZ));
            Vector3 bottomLeft;
            Vector3 topRight;
            if (!TryIntersect(new Vector3(0f, 0f, 0f), plane, out bottomLeft))
                return false;
            if (!TryIntersect(new Vector3(1f, 1f, 0f), plane, out topRight))
                return false;

            MinWorld = new Vector3(
                Mathf.Min(bottomLeft.x, topRight.x),
                Mathf.Min(bottomLeft.y, topRight.y),
                worldZ);
            MaxWorld = new Vector3(
                Mathf.Max(bottomLeft.x, topRight.x),
                Mathf.Max(bottomLeft.y, topRight.y),
                worldZ);
            return MaxWorld.x - MinWorld.x > MinSpan && MaxWorld.y - MinWorld.y > MinSpan;
        }

        bool TryIntersect(Vector3 viewportPoint, Plane plane, out Vector3 worldPoint)
        {
            Ray ray = _camera.ViewportPointToRay(viewportPoint);
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

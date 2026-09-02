using UnityEngine;

namespace DreamChase
{
    /// <summary>
    /// Locks the target camera viewport to 16:9.
    /// SideScrollCamera should read <see cref="LetterboxViewport"/> (or Camera.rect) rather than the full window.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AspectRatioEnforcer : MonoBehaviour
    {
        [SerializeField] float targetAspect = 16f / 9f;
        [SerializeField] Camera targetCamera;

        public Rect LetterboxViewport { get; private set; } = new Rect(0f, 0f, 1f, 1f);

        public Camera TargetCamera
        {
            get { return targetCamera; }
        }

        void Awake()
        {
            ResolveTargetCamera();
            ApplyViewport();
        }

        void OnEnable()
        {
            Camera.onPreCull += HandlePreCull;
            ApplyViewport();
        }

        void OnDisable()
        {
            Camera.onPreCull -= HandlePreCull;
            RestoreFullViewport();
        }

        void HandlePreCull(Camera cam)
        {
            if (cam == targetCamera)
                ApplyViewport();
        }

        void ResolveTargetCamera()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        void ApplyViewport()
        {
            ResolveTargetCamera();
            if (targetCamera == null)
                return;

            float windowAspect = (float)Screen.width / Screen.height;
            float scaleHeight = windowAspect / targetAspect;
            Rect rect;

            if (scaleHeight < 1f)
            {
                rect = new Rect(0f, (1f - scaleHeight) * 0.5f, 1f, scaleHeight);
            }
            else
            {
                float scaleWidth = 1f / scaleHeight;
                rect = new Rect((1f - scaleWidth) * 0.5f, 0f, scaleWidth, 1f);
            }

            LetterboxViewport = rect;
            targetCamera.rect = rect;
        }

        void RestoreFullViewport()
        {
            if (targetCamera != null)
                targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }
}

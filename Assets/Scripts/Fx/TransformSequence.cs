using UnityEngine;

namespace DreamChase
{
    [DisallowMultipleComponent]
    public sealed class TransformSequence : MonoBehaviour
    {
        [SerializeField] ParticleSystem burst;
        [SerializeField] float duration = 0.8f;

        public float Duration { get { return duration; } }

        public void PlayAt(Vector3 worldPosition)
        {
            if (burst == null)
                return;

            Transform burstTransform = burst.transform;
            burstTransform.position = worldPosition;

            GameObject burstObject = burst.gameObject;
            if (!burstObject.activeSelf)
                burstObject.SetActive(true);

            burst.Clear(true);
            burst.Play(true);
        }
    }
}

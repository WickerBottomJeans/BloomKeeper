using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.VFX
{
    public class MutationLaserView : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;

        private Vector3 defaultScale;

        private void Awake()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer == null)
                throw new InvalidOperationException("MutationLaserView requires a LineRenderer reference.");

            defaultScale = transform.localScale;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = false;
            lineRenderer.enabled = false;
        }

        public void Configure(float tileSize, float widthRatio)
        {
            if (tileSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(tileSize), tileSize, "Mutation laser VFX requires a positive tile size.");
            if (widthRatio <= 0f)
                throw new ArgumentOutOfRangeException(nameof(widthRatio), widthRatio, "Mutation laser VFX requires a positive width ratio.");

            transform.localScale = defaultScale * tileSize;
            lineRenderer.startWidth = widthRatio;
            lineRenderer.endWidth = widthRatio;
        }

        public async UniTask Play(Vector2 origin, Vector2 target, float duration)
        {
            lineRenderer.enabled = false;
            transform.position = origin;
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, transform.InverseTransformPoint(target));
            lineRenderer.enabled = true;

            await UniTask.Delay(TimeSpan.FromSeconds(duration));

            lineRenderer.enabled = false;
        }
    }
}

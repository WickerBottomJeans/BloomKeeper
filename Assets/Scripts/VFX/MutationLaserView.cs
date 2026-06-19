using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.VFX
{
    public sealed class MutationLaserView : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;

        private void Awake()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer == null)
                throw new InvalidOperationException("MutationLaserView requires a LineRenderer reference.");

            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;
        }

        public async UniTask Play(
            Vector2 origin,
            Vector2 target,
            float width,
            float duration)
        {
            lineRenderer.enabled = false;
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, target);
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.enabled = true;

            await UniTask.Delay(TimeSpan.FromSeconds(duration));

            lineRenderer.enabled = false;
        }
    }
}

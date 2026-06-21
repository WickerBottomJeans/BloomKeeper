using UnityEngine;

namespace DefaultNamespace.VFX
{
    public class VFXStripeSkill : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particleSystem;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Vector2 authoredTravelDirection = Vector2.right;

        public void Prepare(Vector2 travelDirection, float worldWidth)
        {
            float rotation = Vector2.SignedAngle(authoredTravelDirection, travelDirection);
            transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            SetWidth(worldWidth);
            particleSystem.Clear(true);
            particleSystem.Play(true);
        }

        public void SetWidth(float worldWidth)
        {
            float halfWidth = worldWidth * 0.5f;

            // Particle edge span.
            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.radius = halfWidth;

            // Visible line span.
            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, new Vector3(-halfWidth, 0f, 0f));
            lineRenderer.SetPosition(1, new Vector3(halfWidth, 0f, 0f));
        }
    }
}

using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class PetalJellyMoveTester : MonoBehaviour
{
    [SerializeField] private PetalView target;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float testCellSize = 1f;
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    private void Update()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null) return;
        if (!pointer.press.wasPressedThisFrame) return;

        Vector2 screenPos = pointer.position.ReadValue();
        float cameraDistance = Mathf.Abs(targetCamera.transform.position.z - target.transform.position.z);
        Vector3 worldPos = targetCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cameraDistance));
        PetalViewAnimator.PlayJellyishMove(target, worldPos, testCellSize, moveDuration, moveEase).Forget();
    }
}

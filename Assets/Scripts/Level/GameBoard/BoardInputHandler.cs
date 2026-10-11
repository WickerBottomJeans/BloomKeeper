using System;
using Core;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardInputHandler : MonoBehaviour
{
    [SerializeField] private float dragThresholdPixels = 20f;
    [SerializeField] private InputActionReference pointerPressAction;
    [SerializeField] private InputActionReference pointerPositionAction;
    
    /// <summary>
    /// double tap to edit petal if in admin mode
    /// </summary>
    [SerializeField] private float doubleTapInterval = 0.3f;

    public event Action<Vector2Int> OnEditRequested;

    public event Action<Vector2Int, Vector2Int> OnSwapRequested;
    public event Action<Vector2Int> OnPointerPressed;
    public event Action<Vector2Int> OnPointerMoved;
    public event Action<Vector2Int> OnPointerReleased;
    public event Action OnPointerCanceled;

    private BoardLayout boardLayout;
    private Camera boardCamera;

    private bool isDragging;
    private bool swapRequested;
    private Vector2 touchStartScreenPosition;
    private Vector2Int selectedTile;
    private Vector3 lastResolvedWorldPosition;
    private Vector2Int lastResolvedTile;
    
    private float lastTapTime = -1f;
    private Vector2Int lastTappedTile;
    public void Init(BoardLayout boardLayout, Camera boardCamera)
    {
        this.boardLayout = boardLayout;
        this.boardCamera = boardCamera;
    }

    private void Update()
    {
        InputAction pressAction = pointerPressAction.action;
        if (!pressAction.enabled)
        {
            if (isDragging)
                CancelPointer();
            return;
        }

        if (pressAction.WasPressedThisFrame())
            OnTouchBegan(pointerPositionAction.action.ReadValue<Vector2>());
        else if (pressAction.WasReleasedThisFrame())
            OnTouchEnded(pointerPositionAction.action.ReadValue<Vector2>());
        else if (isDragging && pressAction.IsPressed())
            OnTouchMoved(pointerPositionAction.action.ReadValue<Vector2>());
    }

    private void OnTouchBegan(Vector2 screenPosition)
    {
        if (!TryResolveTile(screenPosition, out var tilePosition)) return;

        if (GlobalState.IsAdminMode)
        {
            float timeSinceLast = Time.time - lastTapTime;
            if (timeSinceLast <= doubleTapInterval && tilePosition == lastTappedTile)
            {
                OnEditRequested?.Invoke(tilePosition);
                lastTapTime = -1f;
                return;
            }

            lastTapTime = Time.time;
            lastTappedTile = tilePosition;
            return;
        }

        isDragging = true;
        swapRequested = false;
        touchStartScreenPosition = screenPosition;
        selectedTile = tilePosition;
        OnPointerPressed?.Invoke(tilePosition);
    }

    private void OnTouchMoved(Vector2 screenPosition)
    {
        if (!TryResolveTile(screenPosition, out Vector2Int tilePosition))
        {
            CancelPointer();
            return;
        }

        OnPointerMoved?.Invoke(tilePosition);
        if (swapRequested) return;

        Vector2 pointerDelta = screenPosition - touchStartScreenPosition;
        if (pointerDelta.magnitude < dragThresholdPixels) return;

        Vector2Int direction = ResolveDirection(pointerDelta);
        Vector2Int targetTile = selectedTile + direction;

        if (!IsInBounds(targetTile)) 
        {
            CancelPointer();
            return;
        }

        OnSwapRequested?.Invoke(selectedTile, targetTile);
        swapRequested = true;
    }

    private void OnTouchEnded(Vector2 screenPosition)
    {
        if (!isDragging) return;

        if (TryResolveTile(screenPosition, out Vector2Int tilePosition))
            OnPointerReleased?.Invoke(tilePosition);
        else
            OnPointerCanceled?.Invoke();

        ClearDragState();
    }

    private void CancelPointer()
    {
        OnPointerCanceled?.Invoke();
        ClearDragState();
    }

    private void ClearDragState()
    {
        isDragging = false;
        swapRequested = false;
        touchStartScreenPosition = default;
        selectedTile = default;
    }

    private void OnDisable()
    {
        if (isDragging)
            CancelPointer();
    }

    private bool TryResolveTile(Vector2 screenPosition, out Vector2Int tilePosition)
    {
        tilePosition = default;
        Vector3 worldPosition = boardCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(boardCamera.transform.position.z)));
        lastResolvedWorldPosition = worldPosition;
        
        int columnIndex = Mathf.RoundToInt((worldPosition.x - boardLayout.OriginWorldPosition.x) / boardLayout.TileSize);
        int rowIndex = Mathf.RoundToInt((worldPosition.y - boardLayout.OriginWorldPosition.y) / boardLayout.TileSize);
        tilePosition = new Vector2Int(columnIndex, rowIndex);
        lastResolvedTile = tilePosition;

        return IsInBounds(tilePosition);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (boardLayout == null) return;
        
        if (isDragging)
        {
            Vector2 selected = boardLayout.OriginWorldPosition + new Vector2(selectedTile.x * boardLayout.TileSize, selectedTile.y * boardLayout.TileSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(selected, Vector3.one * boardLayout.TileSize);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(lastResolvedWorldPosition, boardLayout.TileSize * 0.05f);

        Gizmos.color = Color.blue;
        Vector2 resolvedCenter = boardLayout.OriginWorldPosition + new Vector2(lastResolvedTile.x * boardLayout.TileSize, lastResolvedTile.y * boardLayout.TileSize);
        Gizmos.DrawWireCube(resolvedCenter, Vector3.one * boardLayout.TileSize);
    }

    private bool IsInBounds(Vector2Int tilePosition) =>
        tilePosition.x >= 0 && tilePosition.x < boardLayout.ColumnCount &&
        tilePosition.y >= 0 && tilePosition.y < boardLayout.RowCount;

    private  Vector2Int ResolveDirection(Vector2 pointerDelta)
    {
        if (Mathf.Abs(pointerDelta.x) > Mathf.Abs(pointerDelta.y))
            return pointerDelta.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            return pointerDelta.y > 0 ? Vector2Int.up : Vector2Int.down;
    }
    
}

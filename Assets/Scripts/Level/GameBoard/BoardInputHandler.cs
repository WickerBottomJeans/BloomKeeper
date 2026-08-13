using System;
using Core;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardInputHandler : MonoBehaviour
{
    [SerializeField] private float dragThresholdPx = 20f;
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
    private Camera _camera;

    private bool isDragging;
    private bool swapRequested;
    private Vector2 touchStartScreenPos;
    private Vector2Int selectedTile;
    private Vector3 lastResolvedWorldPos;
    private Vector2Int lastResolvedTile;
    
    private float lastTapTime = -1f;
    private Vector2Int lastTappedTile;
    public void Init(BoardLayout boardLayout, Camera camera)
    {
        this.boardLayout = boardLayout;
        this._camera = camera;
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

    private void OnTouchBegan(Vector2 screenPos)
    {
        if (!TryResolveTile(screenPos, out var tile)) return;

        if (GlobalState.IsAdminMode)
        {
            float timeSinceLast = Time.time - lastTapTime;
            if (timeSinceLast <= doubleTapInterval && tile == lastTappedTile)
            {
                OnEditRequested?.Invoke(tile);
                lastTapTime = -1f;
                return;
            }

            lastTapTime = Time.time;
            lastTappedTile = tile;
            return;
        }

        isDragging = true;
        swapRequested = false;
        touchStartScreenPos = screenPos;
        selectedTile = tile;
        OnPointerPressed?.Invoke(tile);
    }

    private void OnTouchMoved(Vector2 screenPos)
    {
        if (!TryResolveTile(screenPos, out Vector2Int tile))
        {
            CancelPointer();
            return;
        }

        OnPointerMoved?.Invoke(tile);
        if (swapRequested) return;

        Vector2 delta = screenPos - touchStartScreenPos;
        if (delta.magnitude < dragThresholdPx) return;

        Vector2Int direction = ResolveDirection(delta);
        Vector2Int targetTile = selectedTile + direction;

        if (!IsInBounds(targetTile)) 
        {
            CancelPointer();
            return;
        }

        OnSwapRequested?.Invoke(selectedTile, targetTile);
        swapRequested = true;
    }

    private void OnTouchEnded(Vector2 screenPos)
    {
        if (!isDragging) return;

        if (TryResolveTile(screenPos, out Vector2Int tile))
            OnPointerReleased?.Invoke(tile);
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
        touchStartScreenPos = default;
        selectedTile = default;
    }

    private void OnDisable()
    {
        if (isDragging)
            CancelPointer();
    }

    private bool TryResolveTile(Vector2 screenPos, out Vector2Int tile)
    {
        tile = default;
        Vector3 worldPos = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(_camera.transform.position.z)));
        lastResolvedWorldPos = worldPos;
        
        int col = Mathf.RoundToInt((worldPos.x - boardLayout.OriginWorldPos.x) / boardLayout.TileSize);
        int row = Mathf.RoundToInt((worldPos.y - boardLayout.OriginWorldPos.y) / boardLayout.TileSize);
        tile = new Vector2Int(col, row);
        lastResolvedTile = tile;

        return IsInBounds(tile);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (boardLayout == null) return;
        
        if (isDragging)
        {
            Vector2 selected = boardLayout.OriginWorldPos + new Vector2(selectedTile.x * boardLayout.TileSize, selectedTile.y * boardLayout.TileSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(selected, Vector3.one * boardLayout.TileSize);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(lastResolvedWorldPos, boardLayout.TileSize * 0.05f);

        Gizmos.color = Color.blue;
        Vector2 resolvedCenter = boardLayout.OriginWorldPos + new Vector2(lastResolvedTile.x * boardLayout.TileSize, lastResolvedTile.y * boardLayout.TileSize);
        Gizmos.DrawWireCube(resolvedCenter, Vector3.one * boardLayout.TileSize);
    }

    private bool IsInBounds(Vector2Int tile) =>
        tile.x >= 0 && tile.x < boardLayout.Cols &&
        tile.y >= 0 && tile.y < boardLayout.Rows;

    private  Vector2Int ResolveDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
    }
    
}

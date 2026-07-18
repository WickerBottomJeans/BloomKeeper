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

    private BoardLayout boardLayout;
    private Camera _camera;

    private bool isDragging;
    private Vector2 touchStartScreenPos;
    private Vector2Int selectedCell;
    private Vector3 lastResolvedWorldPos;
    private Vector2Int lastResolvedCell;
    
    private float lastTapTime = -1f;
    private Vector2Int lastTappedCell;
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
                ClearDragState();
            return;
        }

        if (pressAction.WasPressedThisFrame())
            OnTouchBegan(pointerPositionAction.action.ReadValue<Vector2>());
        else if (pressAction.WasReleasedThisFrame())
            OnTouchCanceled();
        else if (isDragging && pressAction.IsPressed())
            OnTouchMoved(pointerPositionAction.action.ReadValue<Vector2>());
    }

    private void OnTouchBegan(Vector2 screenPos)
    {
        if (!TryResolveCell(screenPos, out var cell)) return;

        if (GlobalState.IsAdminMode)
        {
            float timeSinceLast = Time.time - lastTapTime;
            if (timeSinceLast <= doubleTapInterval && cell == lastTappedCell)
            {
                OnEditRequested?.Invoke(cell);
                lastTapTime = -1f;
                return;
            }

            lastTapTime = Time.time;
            lastTappedCell = cell;
            return;
        }

        isDragging = true;
        touchStartScreenPos = screenPos;
        selectedCell = cell;
    }

    private void OnTouchMoved(Vector2 screenPos)
    {
        Vector2 delta = screenPos - touchStartScreenPos;
        if (delta.magnitude < dragThresholdPx) return;

        Vector2Int direction = ResolveDirection(delta);
        Vector2Int targetCell = selectedCell + direction;

        if (!IsInBounds(targetCell)) 
        {
            ClearDragState();
            return;
        }

        OnSwapRequested?.Invoke(selectedCell, targetCell);
        ClearDragState();
    }

    private void OnTouchCanceled() => ClearDragState();

    private void ClearDragState()
    {
        isDragging = false;
        touchStartScreenPos = default;
        selectedCell = default;
    }

    private void OnDisable() => ClearDragState();

    private bool TryResolveCell(Vector2 screenPos, out Vector2Int cell)
    {
        cell = default;
        Vector3 worldPos = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(_camera.transform.position.z)));
        lastResolvedWorldPos = worldPos;
        
        int col = Mathf.RoundToInt((worldPos.x - boardLayout.OriginWorldPos.x) / boardLayout.CellSize);
        int row = Mathf.RoundToInt((worldPos.y - boardLayout.OriginWorldPos.y) / boardLayout.CellSize);
        cell = new Vector2Int(col, row);
        lastResolvedCell = cell;

        return IsInBounds(cell);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (boardLayout == null) return;
        
        if (isDragging)
        {
            Vector2 selected = boardLayout.OriginWorldPos + new Vector2(selectedCell.x * boardLayout.CellSize, selectedCell.y * boardLayout.CellSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(selected, Vector3.one * boardLayout.CellSize);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(lastResolvedWorldPos, boardLayout.CellSize * 0.05f);

        Gizmos.color = Color.blue;
        Vector2 resolvedCenter = boardLayout.OriginWorldPos + new Vector2(lastResolvedCell.x * boardLayout.CellSize, lastResolvedCell.y * boardLayout.CellSize);
        Gizmos.DrawWireCube(resolvedCenter, Vector3.one * boardLayout.CellSize);
    }

    private bool IsInBounds(Vector2Int cell) =>
        cell.x >= 0 && cell.x < boardLayout.Cols &&
        cell.y >= 0 && cell.y < boardLayout.Rows;

    private static Vector2Int ResolveDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
    }
    
}

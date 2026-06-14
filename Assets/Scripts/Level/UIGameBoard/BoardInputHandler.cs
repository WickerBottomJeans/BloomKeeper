using System;
using DefaultNamespace.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardInputHandler : MonoBehaviour
{
    [SerializeField] private float dragThresholdPx = 20f;

    public event Action<Vector2Int, Vector2Int> OnSwapRequested;

    private BoardLayout boardLayout;
    private Camera camera;

    private bool isDragging;
    private Vector2 touchStartScreenPos;
    private Vector2Int selectedCell;
    private Vector3 lastResolvedWorldPos;
    private Vector2Int lastResolvedCell;
    public void Init(BoardLayout boardLayout, Camera camera)
    {
        this.boardLayout = boardLayout;
        this.camera = camera;
    }

    private void Update()
    {
        var pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer.press.wasPressedThisFrame)
            OnTouchBegan(pointer.position.ReadValue());
        else if (pointer.press.wasReleasedThisFrame)
            OnTouchCanceled();
        else if (isDragging && pointer.press.isPressed)
            OnTouchMoved(pointer.position.ReadValue());
    }

    private void OnTouchBegan(Vector2 screenPos)
    {
        if (!TryResolveCell(screenPos, out var cell)) return;

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
        Debug.Log($"[BoardInputHandler] Swap requested: {selectedCell} → {targetCell}");
        ClearDragState();
    }

    private void OnTouchCanceled() => ClearDragState();

    private void ClearDragState()
    {
        isDragging = false;
        touchStartScreenPos = default;
        selectedCell = default;
    }

    private bool TryResolveCell(Vector2 screenPos, out Vector2Int cell)
    {
        cell = default;
        Vector3 worldPos = camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(camera.transform.position.z)));
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
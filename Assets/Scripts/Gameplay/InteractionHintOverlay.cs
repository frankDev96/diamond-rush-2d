using UnityEngine;
using UnityEngine.UI;

public class InteractionHintOverlay : MonoBehaviour
{
    [SerializeField] GridBoard board;
    [SerializeField] PlayerGridMotor player;
    [SerializeField] GameplayCameraFit cameraFit;
    [SerializeField] RectTransform arrow;
    [SerializeField] Image arrowImage;
    [SerializeField] float edgeOffset = 36f;

    RectTransform area;
    bool dug;

    void Awake()
    {
        area = (RectTransform)transform;
        SetVisible(false);
    }

    public void NotifyDug()
    {
        dug = true;
        SetVisible(false);
    }

    void LateUpdate()
    {
        if (dug || board == null || player == null || cameraFit == null || arrow == null || cameraFit.IsPanning)
        {
            SetVisible(false);
            return;
        }

        Vector2Int target;
        if (TryFindDigHint(player.GridPosition, out var dirt, out _))
            target = dirt;
        else if (TryFindPushHint(player.GridPosition, out var boulder, out _))
            target = boulder;
        else
        {
            SetVisible(false);
            return;
        }

        var cam = cameraFit.Camera;
        if (cam == null)
            return;

        var playerScreen = cam.WorldToScreenPoint(board.CellToWorld(player.GridPosition.x, player.GridPosition.y));
        var dirtScreen = cam.WorldToScreenPoint(board.CellToWorld(target.x, target.y));
        var dir = ((Vector2)dirtScreen - (Vector2)playerScreen).normalized;
        if (dir.sqrMagnitude < 0.01f)
        {
            SetVisible(false);
            return;
        }

        var point = (Vector2)playerScreen + dir * (edgeOffset * cameraFit.UiScale);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, point, null, out var local))
            return;

        SetVisible(true);
        arrow.anchoredPosition = local;
        arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    bool TryFindDigHint(Vector2Int gridPos, out Vector2Int dirtCell, out Vector2Int direction)
    {
        dirtCell = default;
        direction = default;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        var bestDist = int.MaxValue;
        var found = false;
        for (var y = 0; y < board.Height; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                if (board.GetCell(x, y) != GridCell.Dirt)
                    continue;
                var dist = Mathf.Abs(x - gridPos.x) + Mathf.Abs(y - gridPos.y);
                if (dist >= bestDist)
                    continue;
                foreach (var dir in dirs)
                {
                    if (gridPos + dir != new Vector2Int(x, y))
                        continue;
                    bestDist = dist;
                    dirtCell = new Vector2Int(x, y);
                    direction = dir;
                    found = true;
                    break;
                }
            }
        }

        return found;
    }

    bool TryFindPushHint(Vector2Int gridPos, out Vector2Int boulderCell, out Vector2Int direction)
    {
        boulderCell = default;
        direction = default;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            var boulder = gridPos + dir;
            if (!board.HasBoulder(boulder.x, boulder.y) || !board.CanPushBoulder(boulder, dir))
                continue;
            boulderCell = boulder;
            direction = dir;
            return true;
        }

        return false;
    }

    void SetVisible(bool visible)
    {
        if (arrowImage != null && arrowImage.enabled != visible)
            arrowImage.enabled = visible;
    }
}

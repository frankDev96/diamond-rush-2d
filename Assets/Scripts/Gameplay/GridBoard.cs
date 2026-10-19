using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GridCell
{
    Void,
    Stone,
    Floor,
    Dirt,
    RedGem,
    Exit,
}

public class GridBoard : MonoBehaviour
{
    const int FloorOrder = 0;
    const int ShadowOrder = 1;
    const int DirtOrder = 2;
    const int WallOrder = 3;
    const int DoorOrder = 4;
    const int SlotOrder = 5;
    const int GemGlowOrder = 6;
    const int GemOrder = 7;

    static readonly Color SlotUnlit = new Color(0.16f, 0.12f, 0.1f, 0.95f);

    [SerializeField] float cellSize = 1f;
    [SerializeField] float doorWidthCells = 2.1f;
    [SerializeField] float gemSize = 0.7f;
    [SerializeField] Sprite floorSprite;
    [SerializeField] Sprite floorAltSprite;
    [SerializeField] Sprite dirtSprite;
    [SerializeField] Sprite wallSprite;
    [SerializeField] Sprite gemSprite;
    [SerializeField] Sprite doorClosedSprite;
    [SerializeField] Sprite doorOpenSprite;
    [SerializeField] Transform tilesRoot;
    [SerializeField] Transform entitiesRoot;

    readonly Dictionary<Vector2Int, SpriteRenderer> dirtViews = new Dictionary<Vector2Int, SpriteRenderer>();
    readonly Dictionary<Vector2Int, GameObject> gemViews = new Dictionary<Vector2Int, GameObject>();
    readonly List<SpriteRenderer> slotViews = new List<SpriteRenderer>();

    GridCell[,] cells;
    int width;
    int height;
    Vector2Int spawn;
    bool exitUnlocked;
    int litSlots;
    int requiredReds;
    SpriteRenderer doorView;
    Vector3 doorBase;
    float doorHeight;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
    public Vector2Int Spawn => spawn;
    public bool ExitUnlocked => exitUnlocked;
    public Vector3 WorldCenter => transform.position;
    public Vector2 WorldSize => new Vector2(width * cellSize, height * cellSize);
    public Vector3 DoorWorldPosition => doorView != null ? doorView.transform.position : transform.position;

    public void BuildFromRows(string[] rows, int redsRequired)
    {
        requiredReds = redsRequired;
        ClearChildren(tilesRoot);
        ClearChildren(entitiesRoot);
        dirtViews.Clear();
        gemViews.Clear();
        slotViews.Clear();
        exitUnlocked = false;
        litSlots = 0;
        doorView = null;

        height = rows.Length;
        width = 0;
        foreach (var row in rows)
            width = Mathf.Max(width, row.Length);

        cells = new GridCell[width, height];
        var exitCells = new List<Vector2Int>();
        for (var y = 0; y < height; y++)
        {
            var row = rows[height - 1 - y];
            for (var x = 0; x < width; x++)
            {
                var ch = x < row.Length ? row[x] : '#';
                if (ch == 'P')
                    spawn = new Vector2Int(x, y);
                cells[x, y] = CharToCell(ch);
                if (cells[x, y] == GridCell.Exit)
                    exitCells.Add(new Vector2Int(x, y));
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                SpawnCellViews(x, y);
        }

        SpawnDoor(exitCells);
    }

    static GridCell CharToCell(char ch)
    {
        switch (ch)
        {
            case '.':
            case 'P':
                return GridCell.Floor;
            case 'd':
                return GridCell.Dirt;
            case 'r':
                return GridCell.RedGem;
            case 'E':
                return GridCell.Exit;
            default:
                return GridCell.Stone;
        }
    }

    void SpawnCellViews(int x, int y)
    {
        var cell = cells[x, y];
        var position = CellToWorld(x, y);

        if (cell == GridCell.Stone)
        {
            var wall = SpawnTile("Wall", wallSprite, position, WallOrder);
            if (wall != null && IsOpen(x, y - 1))
                SpawnShadow(x, y - 1);
            return;
        }

        var hash = (x * 73856093) ^ (y * 19349663);
        var alt = floorAltSprite != null && (hash & 1) == 1;
        var floor = SpawnTile("Floor", alt ? floorAltSprite : floorSprite, position, FloorOrder);
        if (floor != null)
            floor.flipX = (hash & 2) == 2;

        if (cell == GridCell.Dirt)
        {
            var dirt = SpawnTile("Dirt", dirtSprite, position, DirtOrder);
            if (dirt != null)
            {
                dirt.flipX = (hash & 4) == 4;
                dirtViews[new Vector2Int(x, y)] = dirt;
            }
        }
        else if (cell == GridCell.RedGem)
        {
            SpawnGem(x, y, position);
        }
    }

    bool IsOpen(int x, int y)
    {
        var cell = GetCell(x, y);
        return cell != GridCell.Void && cell != GridCell.Stone;
    }

    SpriteRenderer SpawnTile(string name, Sprite sprite, Vector3 position, int order)
    {
        if (sprite == null)
            return null;

        var go = new GameObject(name);
        go.transform.SetParent(tilesRoot, false);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        GameplaySprites.FitCell(go.transform, sprite, cellSize * 1.01f);
        return renderer;
    }

    void SpawnShadow(int x, int y)
    {
        var go = new GameObject("WallShadow");
        go.transform.SetParent(tilesRoot, false);
        go.transform.position = CellToWorld(x, y) + Vector3.up * (cellSize * 0.38f);
        go.transform.localScale = new Vector3(cellSize, cellSize * 0.24f, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GameplaySprites.WhiteSquare;
        renderer.color = new Color(0f, 0f, 0f, 0.28f);
        renderer.sortingOrder = ShadowOrder;
    }

    void SpawnGem(int x, int y, Vector3 position)
    {
        if (gemSprite == null)
            return;

        var root = new GameObject("RedGem");
        root.transform.SetParent(entitiesRoot, false);
        root.transform.position = position;

        var glow = GameplaySprites.Spawn("Glow", root.transform, GameplaySprites.SoftCircle, position, gemSize * 1.6f, GemGlowOrder);
        glow.color = new Color(1f, 0.25f, 0.3f, 0.35f);

        var gem = GameplaySprites.Spawn("Gem", root.transform, gemSprite, position, gemSize, GemOrder);
        var idle = gem.gameObject.AddComponent<GemIdle>();
        idle.SetGlow(glow);
        gemViews[new Vector2Int(x, y)] = root;
    }

    void SpawnDoor(List<Vector2Int> exitCells)
    {
        if (exitCells.Count == 0 || doorClosedSprite == null)
            return;

        var center = Vector3.zero;
        foreach (var cell in exitCells)
            center += CellToWorld(cell.x, cell.y);
        center /= exitCells.Count;

        var doorWidth = cellSize * Mathf.Max(doorWidthCells, exitCells.Count);
        var scale = doorWidth / doorClosedSprite.bounds.size.x;
        doorHeight = doorClosedSprite.bounds.size.y * scale;
        var bottom = center.y - cellSize * 0.5f;
        doorBase = new Vector3(center.x, bottom + doorHeight * 0.5f, 0f);

        var go = new GameObject("ExitDoor");
        go.transform.SetParent(entitiesRoot, false);
        go.transform.position = doorBase;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        doorView = go.AddComponent<SpriteRenderer>();
        doorView.sprite = doorClosedSprite;
        doorView.sortingOrder = DoorOrder;

        var slotY = bottom + doorHeight * 0.66f;
        var spacing = doorWidth * 0.2f;
        for (var i = 0; i < requiredReds; i++)
        {
            var offset = (i - (requiredReds - 1) * 0.5f) * spacing;
            var slot = GameplaySprites.Spawn("DoorSlot" + i, entitiesRoot, gemSprite, new Vector3(center.x + offset, slotY, 0f), doorWidth * 0.14f, SlotOrder);
            slot.color = SlotUnlit;
            slotViews.Add(slot);
        }
    }

    public Vector3 CellToWorld(int x, int y)
    {
        var local = new Vector3(
            (x - (width - 1) * 0.5f) * cellSize,
            (y - (height - 1) * 0.5f) * cellSize,
            0f);
        return transform.TransformPoint(local);
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    public GridCell GetCell(int x, int y)
    {
        if (!InBounds(x, y))
            return GridCell.Void;
        return cells[x, y];
    }

    public bool IsBlocked(int x, int y)
    {
        var cell = GetCell(x, y);
        if (cell == GridCell.Exit)
            return !exitUnlocked;
        return cell == GridCell.Void || cell == GridCell.Stone;
    }

    public bool IsExit(int x, int y)
    {
        return GetCell(x, y) == GridCell.Exit;
    }

    public bool TryDig(int x, int y)
    {
        if (!InBounds(x, y) || cells[x, y] != GridCell.Dirt)
            return false;

        cells[x, y] = GridCell.Floor;
        var key = new Vector2Int(x, y);
        if (dirtViews.TryGetValue(key, out var view) && view != null)
            StartCoroutine(ShrinkAndDestroy(view));
        dirtViews.Remove(key);
        return true;
    }

    public bool TryCollectGem(int x, int y, out GameObject gem)
    {
        gem = null;
        if (!InBounds(x, y) || cells[x, y] != GridCell.RedGem)
            return false;

        cells[x, y] = GridCell.Floor;
        var key = new Vector2Int(x, y);
        if (gemViews.TryGetValue(key, out gem) && gem != null)
        {
            foreach (var idle in gem.GetComponentsInChildren<GemIdle>())
                Destroy(idle);
        }

        gemViews.Remove(key);
        return true;
    }

    public void LightNextSlot()
    {
        if (litSlots >= slotViews.Count)
            return;

        var slot = slotViews[litSlots++];
        slot.color = Color.white;
        StartCoroutine(Pulse(slot.transform, 1.5f, 0.25f));
    }

    public void FlashSlots()
    {
        for (var i = litSlots; i < slotViews.Count; i++)
            StartCoroutine(FlashSlot(slotViews[i]));
        if (doorView != null)
            StartCoroutine(Pulse(doorView.transform, 1.03f, 0.18f));
    }

    public void SetExitUnlocked()
    {
        if (exitUnlocked)
            return;

        exitUnlocked = true;
        if (doorView == null)
            return;

        if (doorOpenSprite != null)
        {
            var doorWidth = doorView.sprite.bounds.size.x * doorView.transform.localScale.x;
            doorView.sprite = doorOpenSprite;
            GameplaySprites.FitWidth(doorView.transform, doorOpenSprite, doorWidth);
        }

        var glow = GameplaySprites.Spawn("DoorGlow", entitiesRoot, GameplaySprites.SoftCircle, doorBase, cellSize * 3f, DoorOrder - 1);
        glow.color = new Color(1f, 0.85f, 0.4f, 0.55f);
        StartCoroutine(Pulse(doorView.transform, 1.08f, 0.4f));
    }

    IEnumerator ShrinkAndDestroy(SpriteRenderer view)
    {
        var start = view.transform.localScale;
        var color = view.color;
        var t = 0f;
        const float duration = 0.18f;
        while (t < duration && view != null)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / duration);
            view.transform.localScale = start * (1f - 0.4f * k);
            color.a = 1f - k;
            view.color = color;
            yield return null;
        }

        if (view != null)
            Destroy(view.gameObject);
    }

    IEnumerator Pulse(Transform target, float peak, float duration)
    {
        if (target == null)
            yield break;

        var start = target.localScale;
        var t = 0f;
        while (t < duration && target != null)
        {
            t += Time.deltaTime;
            var k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            target.localScale = start * Mathf.Lerp(1f, peak, k);
            yield return null;
        }

        if (target != null)
            target.localScale = start;
    }

    IEnumerator FlashSlot(SpriteRenderer slot)
    {
        var t = 0f;
        const float duration = 0.4f;
        while (t < duration && slot != null)
        {
            t += Time.deltaTime;
            var k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI * 2f);
            slot.color = Color.Lerp(SlotUnlit, new Color(1f, 0.3f, 0.3f, 1f), Mathf.Abs(k));
            yield return null;
        }

        if (slot != null)
            slot.color = SlotUnlit;
    }

    void ClearChildren(Transform root)
    {
        if (root == null)
            return;

        for (var i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}

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
    Boulder,
    PurpleGem,
    Chest,
    SecretChest,
    Fire,
    Spear,
    Checkpoint,
}

public class GridBoard : MonoBehaviour
{
    const int BackdropOrder = -50;
    const int ExteriorBushOrder = 5;
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
    [SerializeField] int backdropMargin = 18;
    [SerializeField] Sprite floorSprite;
    [SerializeField] Sprite floorAltSprite;
    [SerializeField] Sprite floorMossSprite;
    [SerializeField] Sprite floorCrackedSprite;
    [SerializeField] Sprite dirtSprite;
    [SerializeField] Sprite wallSprite;
    [SerializeField] Sprite wallTopSprite;
    [SerializeField] Sprite jungleFillSprite;
    [SerializeField] Sprite jungleEdgeSprite;
    [SerializeField] float exteriorBushDepthCells = 2.2f;
    [SerializeField] float exteriorBushPadCells = 3f;
    [SerializeField] Sprite gemSprite;
    [SerializeField] Sprite purpleGemSprite;
    [SerializeField] Sprite boulderSprite;
    [SerializeField] Sprite chestSprite;
    [SerializeField] Sprite secretChestSprite;
    [SerializeField] Sprite fireSprite;
    [SerializeField] Sprite spearSprite;
    [SerializeField] Sprite checkpointSprite;
    [SerializeField] Sprite doorClosedSprite;
    [SerializeField] Sprite doorOpenSprite;
    [SerializeField] Transform tilesRoot;
    [SerializeField] Transform entitiesRoot;

    readonly Dictionary<Vector2Int, SpriteRenderer> dirtViews = new Dictionary<Vector2Int, SpriteRenderer>();
    readonly Dictionary<Vector2Int, GameObject> gemViews = new Dictionary<Vector2Int, GameObject>();
    readonly Dictionary<Vector2Int, Transform> boulderViews = new Dictionary<Vector2Int, Transform>();
    readonly List<SpriteRenderer> slotViews = new List<SpriteRenderer>();
    GridWorldController world;

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
    public Vector3 DoorCameraAnchor => doorView != null ? doorBase + Vector3.down * (doorHeight * 0.12f) : DoorWorldPosition;

    public void BuildFromSpec(GameplayLevelSpec spec)
    {
        BuildFromRows(spec.Rows, spec.RequiredReds);
        world = GetComponent<GridWorldController>();
        if (world != null)
            world.Initialize(this, spec);
    }

    public void BuildFromRows(string[] rows, int redsRequired)
    {
        requiredReds = redsRequired;
        ClearChildren(tilesRoot);
        ClearChildren(entitiesRoot);
        dirtViews.Clear();
        gemViews.Clear();
        boulderViews.Clear();
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

        SpawnBackdrop();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                SpawnCellViews(x, y);
        }

        SpawnExteriorBushes();
        SpawnDoor(exitCells);
    }

    void SpawnExteriorBushes()
    {
        if (jungleEdgeSprite == null)
            return;

        var root = new GameObject("JungleExterior");
        root.transform.SetParent(tilesRoot, false);
        root.transform.localPosition = Vector3.zero;

        var boardW = width * cellSize;
        var boardH = height * cellSize;
        var depth = exteriorBushDepthCells * cellSize;
        var pad = exteriorBushPadCells * cellSize;
        var halfW = boardW * 0.5f;
        var halfH = boardH * 0.5f;

        // Leaf/vine bands sit just outside the stone border (world space, moves with the board + camera).
        SpawnBushEdge(root.transform, "BushTop", new Vector3(0f, halfH + depth * 0.5f, 0f), boardW + pad, depth, 0f);
        SpawnBushEdge(root.transform, "BushBottom", new Vector3(0f, -halfH - depth * 0.5f, 0f), boardW + pad, depth, 180f);
        SpawnBushEdge(root.transform, "BushLeft", new Vector3(-halfW - depth * 0.5f, 0f, 0f), boardH + pad, depth, 90f);
        SpawnBushEdge(root.transform, "BushRight", new Vector3(halfW + depth * 0.5f, 0f, 0f), boardH + pad, depth, -90f);
    }

    void SpawnBushEdge(Transform parent, string name, Vector3 localPos, float along, float thick, float zDegrees)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, zDegrees);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = jungleEdgeSprite;
        renderer.sortingOrder = ExteriorBushOrder;
        renderer.color = new Color(1f, 1f, 1f, 0.94f);
        var bounds = jungleEdgeSprite.bounds.size;
        if (bounds.x <= 0f || bounds.y <= 0f)
            return;
        go.transform.localScale = new Vector3(along / bounds.x, thick / bounds.y, 1f);
    }

    void SpawnBackdrop()
    {
        if (jungleFillSprite == null)
            return;

        var go = new GameObject("JungleBackdrop");
        go.transform.SetParent(tilesRoot, false);
        go.transform.position = transform.position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = jungleFillSprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.tileMode = SpriteTileMode.Continuous;
        var size = new Vector2((width + backdropMargin * 2) * cellSize, (height + backdropMargin * 2) * cellSize);
        renderer.size = size;
        renderer.sortingOrder = BackdropOrder;
        renderer.color = new Color(0.72f, 0.82f, 0.68f);

        // Darker under-layer so solid rock reads as deep jungle mass (mockup image 3).
        var under = new GameObject("JungleBackdropDeep");
        under.transform.SetParent(tilesRoot, false);
        under.transform.position = transform.position;
        var deep = under.AddComponent<SpriteRenderer>();
        deep.sprite = jungleFillSprite;
        deep.drawMode = SpriteDrawMode.Tiled;
        deep.tileMode = SpriteTileMode.Continuous;
        deep.size = size * 1.04f;
        deep.sortingOrder = BackdropOrder - 1;
        deep.color = new Color(0.45f, 0.58f, 0.42f);
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
            case 'g':
                return GridCell.PurpleGem;
            case 'B':
                return GridCell.Boulder;
            case 'T':
                return GridCell.Chest;
            case 't':
                return GridCell.SecretChest;
            case 'F':
                return GridCell.Fire;
            case '!':
                return GridCell.Spear;
            case 'K':
                return GridCell.Checkpoint;
            case 'S':
                return GridCell.Floor;
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
            SpawnWall(x, y, position);
            return;
        }

        var hash = Hash(x, y);
        var floorChoice = floorSprite;
        var roll = hash % 100;
        if (floorMossSprite != null && roll < 18)
            floorChoice = floorMossSprite;
        else if (floorCrackedSprite != null && roll < 28)
            floorChoice = floorCrackedSprite;
        else if (floorAltSprite != null && (hash & 1) == 1)
            floorChoice = floorAltSprite;
        var floor = SpawnTile("Floor", floorChoice, position, FloorOrder);
        if (floor != null)
            floor.flipX = (hash & 2) == 2;
        if (!IsOpen(x - 1, y))
            SpawnSideShadow(x, y, -1);
        if (!IsOpen(x + 1, y))
            SpawnSideShadow(x, y, 1);

        if (cell == GridCell.Dirt)
        {
            // Soft-clear tile: full-cell jungle grass (still GridCell.Dirt for dig rules).
            var dirt = SpawnGrass(dirtSprite, position, hash);
            if (dirt != null)
                dirtViews[new Vector2Int(x, y)] = dirt;
        }
        else if (cell == GridCell.RedGem)
            SpawnGem(x, y, position);
        else if (cell == GridCell.PurpleGem)
            SpawnPurpleGem(x, y, position);
        else if (cell == GridCell.Boulder)
            SpawnBoulder(x, y, position);
        else if (cell == GridCell.Chest)
            SpawnProp("Chest", chestSprite, x, y, position, GemOrder);
        else if (cell == GridCell.SecretChest)
            SpawnProp("SecretChest", secretChestSprite, x, y, position, GemOrder);
        else if (cell == GridCell.Fire)
            SpawnProp("Fire", fireSprite, x, y, position, DirtOrder + 1);
        else if (cell == GridCell.Spear)
            SpawnProp("Spear", spearSprite, x, y, position, WallOrder + 1);
        else if (cell == GridCell.Checkpoint)
            SpawnProp("Checkpoint", checkpointSprite, x, y, position, FloorOrder + 1);
    }

    void SpawnPurpleGem(int x, int y, Vector3 position)
    {
        var sprite = purpleGemSprite != null ? purpleGemSprite : gemSprite;
        if (sprite == null)
            return;

        var root = new GameObject("PurpleGem");
        root.transform.SetParent(entitiesRoot, false);
        root.transform.position = position;
        var glow = GameplaySprites.Spawn("Glow", root.transform, GameplaySprites.SoftCircle, position, gemSize * 1.4f, GemGlowOrder);
        glow.color = new Color(0.65f, 0.35f, 1f, 0.35f);
        var gem = GameplaySprites.Spawn("Gem", root.transform, sprite, position, gemSize * 0.85f, GemOrder);
        var idle = gem.gameObject.AddComponent<GemIdle>();
        idle.SetGlow(glow);
        gemViews[new Vector2Int(x, y)] = root;
    }

    void SpawnBoulder(int x, int y, Vector3 position)
    {
        if (boulderSprite == null)
            return;

        var go = new GameObject("Boulder");
        go.transform.SetParent(entitiesRoot, false);
        go.transform.position = position;
        var view = GameplaySprites.Spawn("Face", go.transform, boulderSprite, position, cellSize * 0.92f, WallOrder + 2);
        boulderViews[new Vector2Int(x, y)] = go.transform;
    }

    void SpawnProp(string name, Sprite sprite, int x, int y, Vector3 position, int order)
    {
        if (sprite == null)
            return;

        var go = new GameObject(name);
        go.transform.SetParent(entitiesRoot, false);
        go.transform.position = position;
        GameplaySprites.Spawn(name + "View", go.transform, sprite, position, cellSize * 0.88f, order);
        gemViews[new Vector2Int(x, y)] = go;
    }

    static int Hash(int x, int y)
    {
        var h = (x * 73856093) ^ (y * 19349663);
        return h & 0x7fffffff;
    }

    bool IsOpen(int x, int y)
    {
        var cell = GetCell(x, y);
        return cell != GridCell.Void && cell != GridCell.Stone;
    }

    bool HasOpenNeighbour(int x, int y)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if ((dx != 0 || dy != 0) && IsOpen(x + dx, y + dy))
                    return true;
            }
        }

        return false;
    }

    bool HasCardinalOpen(int x, int y)
    {
        return IsOpen(x + 1, y) || IsOpen(x - 1, y) || IsOpen(x, y + 1) || IsOpen(x, y - 1);
    }

    // Oblique hybrid: player approaches from bottom — vertical walls read as one-cell **top caps** (no tall front face).
    void SpawnWall(int x, int y, Vector3 position)
    {
        if (HasCardinalOpen(x, y))
        {
            var openSouth = IsOpen(x, y - 1);
            var openNorth = IsOpen(x, y + 1);
            var openEast = IsOpen(x + 1, y);
            var openWest = IsOpen(x - 1, y);

            var capOnlyNorth = openNorth && !openSouth && !openEast && !openWest;
            var sprite = capOnlyNorth && wallTopSprite != null
                ? wallTopSprite
                : wallSprite != null ? wallSprite : wallTopSprite;

            var wall = SpawnTile(capOnlyNorth ? "WallTop" : "Wall", sprite, position, WallOrder);
            if (wall != null)
            {
                if (openWest && !openEast)
                    wall.flipX = true;
                else if (openEast && !openWest)
                    wall.flipX = false;
                else if (openEast || openWest)
                    wall.flipX = openWest;
                else
                    wall.flipX = (Hash(x, y) & 4) == 4;

                if (openSouth)
                    SpawnShadow(x, y - 1);
            }

            return;
        }

        if (HasOpenNeighbour(x, y))
        {
            var top = SpawnTile("WallTop", wallTopSprite != null ? wallTopSprite : wallSprite, position, WallOrder);
            if (top != null)
                top.flipX = (Hash(x, y) & 4) == 4;
            return;
        }

        if (jungleFillSprite == null)
            return;

        var go = new GameObject("JungleMass");
        go.transform.SetParent(tilesRoot, false);
        go.transform.position = position;
        var fill = go.AddComponent<SpriteRenderer>();
        fill.sprite = jungleFillSprite;
        fill.drawMode = SpriteDrawMode.Tiled;
        fill.tileMode = SpriteTileMode.Continuous;
        fill.size = new Vector2(cellSize, cellSize);
        fill.sortingOrder = WallOrder - 1;
        fill.color = new Color(0.85f, 0.92f, 0.82f);
        fill.flipX = (Hash(x, y) & 8) == 8;
    }

    void SpawnSideShadow(int x, int y, int side)
    {
        var go = new GameObject("SideShadow");
        go.transform.SetParent(tilesRoot, false);
        go.transform.position = CellToWorld(x, y) + Vector3.right * (side * cellSize * 0.44f);
        go.transform.localScale = new Vector3(cellSize * 0.12f, cellSize, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GameplaySprites.WhiteSquare;
        renderer.color = new Color(0f, 0f, 0f, 0.2f);
        renderer.sortingOrder = ShadowOrder;
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

    SpriteRenderer SpawnGrass(Sprite sprite, Vector3 position, int hash)
    {
        if (sprite == null)
            return null;

        var go = new GameObject("Grass");
        go.transform.SetParent(tilesRoot, false);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = DirtOrder;
        renderer.flipX = (hash & 4) == 4;
        // Fill the square cell edge-to-edge (slight overlap hides seams between neighbours).
        GameplaySprites.FitCell(go.transform, sprite, cellSize * 1.04f);
        go.AddComponent<BushSway>();
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
        return cell == GridCell.Void || cell == GridCell.Stone || cell == GridCell.Boulder;
    }

    public bool HasBoulder(int x, int y) => GetCell(x, y) == GridCell.Boulder;

    public bool CanPushBoulder(Vector2Int boulderCell, Vector2Int direction)
    {
        var landing = boulderCell + direction;
        if (GetCell(boulderCell.x, boulderCell.y) != GridCell.Boulder || !InBounds(landing.x, landing.y))
            return false;

        var targetCell = GetCell(landing.x, landing.y);
        if (targetCell == GridCell.Stone || targetCell == GridCell.Boulder || targetCell == GridCell.Void
            || targetCell == GridCell.Dirt || targetCell == GridCell.RedGem)
            return false;
        return targetCell != GridCell.Exit || exitUnlocked;
    }

    public bool TryPushBoulder(Vector2Int boulderCell, Vector2Int direction, out Vector2Int landing)
    {
        landing = boulderCell + direction;
        if (!CanPushBoulder(boulderCell, direction))
            return false;

        var targetCell = GetCell(landing.x, landing.y);

        cells[boulderCell.x, boulderCell.y] = GridCell.Floor;
        cells[landing.x, landing.y] = GridCell.Boulder;
        if (boulderViews.TryGetValue(boulderCell, out var view) && view != null)
        {
            boulderViews.Remove(boulderCell);
            boulderViews[landing] = view;
            view.position = CellToWorld(landing.x, landing.y);
        }

        if (targetCell == GridCell.Fire)
            ClearHazard(landing.x, landing.y);

        if (world != null)
            world.OnBoulderMoved(landing);
        return true;
    }

    public bool TryCollectPurple(int x, int y, out GameObject view)
    {
        view = null;
        if (!InBounds(x, y) || cells[x, y] != GridCell.PurpleGem)
            return false;
        cells[x, y] = GridCell.Floor;
        var key = new Vector2Int(x, y);
        if (gemViews.TryGetValue(key, out view))
            gemViews.Remove(key);
        return true;
    }

    public bool TryOpenChest(int x, int y, out int purpleAmount, out bool secret)
    {
        purpleAmount = 0;
        secret = false;
        if (!InBounds(x, y))
            return false;
        var cell = cells[x, y];
        if (cell != GridCell.Chest && cell != GridCell.SecretChest)
            return false;
        secret = cell == GridCell.SecretChest;
        purpleAmount = secret ? 8 : 4;
        cells[x, y] = GridCell.Floor;
        var key = new Vector2Int(x, y);
        if (gemViews.TryGetValue(key, out var go) && go != null)
            Destroy(go);
        gemViews.Remove(key);
        return true;
    }

    public void ClearHazard(int x, int y)
    {
        if (!InBounds(x, y))
            return;
        if (cells[x, y] != GridCell.Fire)
            return;
        cells[x, y] = GridCell.Floor;
        var key = new Vector2Int(x, y);
        if (gemViews.TryGetValue(key, out var go) && go != null)
            Destroy(go);
        gemViews.Remove(key);
    }

    public bool IsExit(int x, int y)
    {
        return GetCell(x, y) == GridCell.Exit;
    }

    public void BeginDirtCrack(int x, int y)
    {
        BeginDirtCrack(x, y, Vector2.zero);
    }

    public void BeginDirtCrack(int x, int y, Vector2 pushDir)
    {
        if (!InBounds(x, y) || cells[x, y] != GridCell.Dirt)
            return;

        var key = new Vector2Int(x, y);
        if (!dirtViews.TryGetValue(key, out var view) || view == null)
            return;

        var sway = view.GetComponent<BushSway>();
        if (sway != null)
            sway.Rustle(pushDir);
    }

    public bool TryDig(int x, int y)
    {
        return TryDig(x, y, Vector2.zero);
    }

    public bool TryDig(int x, int y, Vector2 pushDir)
    {
        if (!InBounds(x, y) || cells[x, y] != GridCell.Dirt)
            return false;

        cells[x, y] = GridCell.Floor;
        var key = new Vector2Int(x, y);
        if (dirtViews.TryGetValue(key, out var view) && view != null)
            StartCoroutine(ClearGrass(view, pushDir, 0.22f));
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

    IEnumerator ClearGrass(SpriteRenderer view, Vector2 pushDir, float duration = 0.22f)
    {
        if (view == null)
            yield break;

        var sway = view.GetComponent<BushSway>();
        if (sway != null)
            sway.Rustle(pushDir);

        // Let the live breeze + rustle read for a beat, then part and fade.
        var keepSway = Mathf.Min(0.08f, duration * 0.35f);
        var waited = 0f;
        while (waited < keepSway && view != null)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        if (sway != null)
            sway.enabled = false;

        if (view == null)
            yield break;

        var start = view.transform.localScale;
        var startRot = view.transform.localRotation;
        var origin = view.transform.position;
        var color = view.color;
        var push = pushDir.sqrMagnitude > 0.0001f ? pushDir.normalized : Vector2.right;
        var t = 0f;
        var clearDuration = Mathf.Max(0.05f, duration - keepSway);
        while (t < clearDuration && view != null)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / clearDuration);
            var flutter = Mathf.Sin(k * Mathf.PI * 6f) * (1f - k);
            view.transform.localScale = new Vector3(
                start.x * (1f - 0.35f * k + flutter * 0.06f),
                start.y * (1f - 0.7f * k),
                start.z);
            view.transform.localRotation = startRot * Quaternion.Euler(0f, 0f, (18f * push.x + flutter * 10f) * k);
            view.transform.position = origin + new Vector3(push.x * 0.08f * k, 0.04f * k, 0f);
            color.a = 1f - k;
            view.color = color;
            yield return null;
        }

        if (view != null)
            Destroy(view.gameObject);
    }

    IEnumerator ShrinkAndDestroy(SpriteRenderer view, float duration = 0.18f)
    {
        if (view == null)
            yield break;

        var sway = view.GetComponent<BushSway>();
        if (sway != null)
            sway.enabled = false;

        var start = view.transform.localScale;
        var startRot = view.transform.localRotation;
        var color = view.color;
        var t = 0f;
        while (t < duration && view != null)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / duration);
            view.transform.localScale = start * (1f - 0.55f * k);
            view.transform.localRotation = startRot * Quaternion.Euler(0f, 0f, 12f * k);
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

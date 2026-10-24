using System.Collections.Generic;
using UnityEngine;

public class GridWorldController : MonoBehaviour
{
    struct Snake
    {
        public Vector2Int Cell;
        public Vector2Int Direction;
        public int Length;
        public int Step;
        public Transform View;
        public bool Alive;
    }

    struct Spear
    {
        public Vector2Int Cell;
        public bool Extended;
        public Transform View;
    }

    [SerializeField] Sprite snakeSprite;

    GridBoard board;
    readonly List<Snake> snakes = new List<Snake>();
    readonly List<Spear> spears = new List<Spear>();
    Vector2Int checkpoint = new Vector2Int(-1, -1);
    float snakeTimer;
    float spearTimer;
    const float SnakeStepSeconds = 0.55f;
    const float SpearCycleSeconds = 1.4f;

    public Vector2Int Checkpoint => checkpoint;

    public void Initialize(GridBoard gridBoard, GameplayLevelSpec spec)
    {
        board = gridBoard;
        snakes.Clear();
        spears.Clear();
        checkpoint = new Vector2Int(-1, -1);
        snakeTimer = 0f;
        spearTimer = 0f;

        if (spec.Snakes != null)
        {
            foreach (var patrol in spec.Snakes)
            {
                var cell = new Vector2Int(patrol.X, patrol.Y);
                var view = SpawnEntity("Snake", snakeSprite, cell, WallOrder + 3, 0.82f);
                snakes.Add(new Snake
                {
                    Cell = cell,
                    Direction = new Vector2Int(patrol.Dx, patrol.Dy),
                    Length = Mathf.Max(1, patrol.Length),
                    Step = 0,
                    View = view,
                    Alive = true,
                });
            }
        }

        for (var y = 0; y < board.Height; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                var cell = board.GetCell(x, y);
                if (cell == GridCell.Spear)
                {
                    var view = board.transform.Find("Entities");
                    spears.Add(new Spear { Cell = new Vector2Int(x, y), Extended = false });
                }
                else if (cell == GridCell.Checkpoint)
                    checkpoint = new Vector2Int(x, y);
            }
        }
    }

    static int WallOrder => 3;

    Transform SpawnEntity(string name, Sprite sprite, Vector2Int cell, int order, float scale)
    {
        if (board == null || sprite == null)
            return null;
        var root = board.transform.Find("Entities");
        if (root == null)
            return null;
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        var pos = board.CellToWorld(cell.x, cell.y);
        go.transform.position = pos;
        GameplaySprites.Spawn(name + "View", go.transform, sprite, pos, board.CellSize * scale, order);
        return go.transform;
    }

    void Update()
    {
        if (board == null)
            return;

        snakeTimer += Time.deltaTime;
        if (snakeTimer >= SnakeStepSeconds)
        {
            snakeTimer = 0f;
            StepSnakes();
        }

        spearTimer += Time.deltaTime;
        if (spearTimer >= SpearCycleSeconds * 0.5f)
        {
            spearTimer = 0f;
            ToggleSpears();
        }
    }

    void StepSnakes()
    {
        for (var i = 0; i < snakes.Count; i++)
        {
            var snake = snakes[i];
            if (!snake.Alive)
                continue;

            snake.Step++;
            if (snake.Step > snake.Length)
            {
                snake.Direction *= -1;
                snake.Step = 0;
            }

            var next = snake.Cell + snake.Direction;
            if (!board.InBounds(next.x, next.y) || board.GetCell(next.x, next.y) == GridCell.Stone)
            {
                snake.Direction *= -1;
                next = snake.Cell + snake.Direction;
            }

            snake.Cell = next;
            if (snake.View != null)
                snake.View.position = board.CellToWorld(next.x, next.y);
            snakes[i] = snake;
        }
    }

    void ToggleSpears()
    {
        for (var i = 0; i < spears.Count; i++)
        {
            var spear = spears[i];
            spear.Extended = !spear.Extended;
            spears[i] = spear;
        }
    }

    public bool IsSnakeAt(Vector2Int cell)
    {
        for (var i = 0; i < snakes.Count; i++)
        {
            if (snakes[i].Alive && snakes[i].Cell == cell)
                return true;
        }

        return false;
    }

    public bool IsSpearDanger(Vector2Int cell)
    {
        for (var i = 0; i < spears.Count; i++)
        {
            if (spears[i].Cell == cell && spears[i].Extended)
                return true;
        }

        return false;
    }

    public bool IsFireAt(Vector2Int cell) => board != null && board.GetCell(cell.x, cell.y) == GridCell.Fire;

    public void OnBoulderMoved(Vector2Int cell)
    {
        if (board.GetCell(cell.x, cell.y) == GridCell.Fire)
            board.ClearHazard(cell.x, cell.y);

        for (var i = 0; i < snakes.Count; i++)
        {
            if (snakes[i].Alive && snakes[i].Cell == cell)
            {
                var snake = snakes[i];
                snake.Alive = false;
                if (snake.View != null)
                    Destroy(snake.View.gameObject);
                snakes[i] = snake;
            }
        }
    }

    public void SetCheckpoint(Vector2Int cell)
    {
        checkpoint = cell;
    }
}

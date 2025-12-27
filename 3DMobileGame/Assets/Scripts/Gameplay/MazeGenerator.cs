using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Settings")]
    [SerializeField] private Cell _CellPrefab;
    [SerializeField] private int _CellWidth = 10;
    [SerializeField] private int _CellDepth = 10;

    [Header("Cell Scale")]
    [Tooltip("Physical size of each cell in Unity units (default 1 = 1x1 meters)")]
    [SerializeField] private float cellSize = 3f; // NOW CORRIDORS ARE 3x3 meters!

    [Header("Generation")]
    [SerializeField] private bool clearOnStart = true;
    [SerializeField] private Transform mazeParent;

    private Cell[,] _CellGrid;
    private Vector3 mazeOffset;

    void Start()
    {
        if (clearOnStart)
            ClearMaze();

        GenerateNewMaze();
    }

    [ContextMenu("Generate Maze Now")]
    public void GenerateNewMaze()
    {
        if (_CellPrefab == null)
        {
            Debug.LogError("Cell Prefab not assigned!");
            return;
        }

        // Calculate offset to center maze (accounting for cell size)
        mazeOffset = new Vector3(-_CellWidth * cellSize / 2f, 0, -_CellDepth * cellSize / 2f);

        Debug.Log($"Generating {_CellWidth}x{_CellDepth} maze with {cellSize}m cells...");

        _CellGrid = new Cell[_CellWidth, _CellDepth];

        // Create all cells with proper spacing
        for (int x = 0; x < _CellWidth; x++)
        {
            for (int z = 0; z < _CellDepth; z++)
            {
                // Position cells with cellSize spacing
                Vector3 position = new Vector3(x * cellSize, 0, z * cellSize) + mazeOffset;
                Cell newCell = Instantiate(_CellPrefab, position, Quaternion.identity);

                // Scale the cell to match cellSize
                newCell.transform.localScale = Vector3.one * cellSize;

                if (mazeParent != null)
                    newCell.transform.SetParent(mazeParent);
                else
                    newCell.transform.SetParent(transform);

                newCell.name = $"Cell_{x}_{z}";
                _CellGrid[x, z] = newCell;
            }
        }

        StartCoroutine(GenerateMazeCoroutine(_CellGrid[0, 0]));
    }

    IEnumerator GenerateMazeCoroutine(Cell startCell)
    {
        Stack<Cell> pathStack = new Stack<Cell>();
        Cell currentCell = startCell;
        currentCell.Visit();

        int cellsVisited = 1;
        int totalCells = _CellWidth * _CellDepth;

        while (cellsVisited < totalCells)
        {
            Cell nextCell = GetNextUnvisitedCell(currentCell);

            if (nextCell != null)
            {
                ClearWalls(currentCell, nextCell);
                pathStack.Push(currentCell);
                currentCell = nextCell;
                currentCell.Visit();
                cellsVisited++;
            }
            else if (pathStack.Count > 0)
            {
                currentCell = pathStack.Pop();
            }
            else
            {
                break;
            }

            if (cellsVisited % 10 == 0)
                yield return null;
        }

        Debug.Log($"✅ Maze generation complete! ({cellsVisited} cells)");

        yield return new WaitForSeconds(0.5f);
        OnMazeGenerationComplete();
    }

    void OnMazeGenerationComplete()
    {
        var enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner != null)
        {
            enemySpawner.enabled = true;
            Debug.Log("EnemySpawner enabled");
        }

        var collectibleSpawner = FindFirstObjectByType<CollectibleSpawner>();
        if (collectibleSpawner != null)
        {
            collectibleSpawner.enabled = true;
            Debug.Log("CollectibleSpawner enabled");
        }
    }

    private Cell GetNextUnvisitedCell(Cell currentCell)
    {
        var unvisitedCells = GetUnvisitedCells(currentCell).ToList();
        if (unvisitedCells.Count == 0)
            return null;

        return unvisitedCells[Random.Range(0, unvisitedCells.Count)];
    }

    private IEnumerable<Cell> GetUnvisitedCells(Cell currentCell)
    {
        // Get grid coordinates (accounting for cellSize)
        Vector3 localPos = currentCell.transform.position - mazeOffset;
        int x = Mathf.RoundToInt(localPos.x / cellSize);
        int z = Mathf.RoundToInt(localPos.z / cellSize);

        // Check all four directions
        if (x + 1 < _CellWidth)
        {
            var cellToRight = _CellGrid[x + 1, z];
            if (cellToRight != null && !cellToRight.IsVisited)
                yield return cellToRight;
        }

        if (x - 1 >= 0)
        {
            var cellToLeft = _CellGrid[x - 1, z];
            if (cellToLeft != null && !cellToLeft.IsVisited)
                yield return cellToLeft;
        }

        if (z + 1 < _CellDepth)
        {
            var cellToFront = _CellGrid[x, z + 1];
            if (cellToFront != null && !cellToFront.IsVisited)
                yield return cellToFront;
        }

        if (z - 1 >= 0)
        {
            var cellToBack = _CellGrid[x, z - 1];
            if (cellToBack != null && !cellToBack.IsVisited)
                yield return cellToBack;
        }
    }

    private void ClearWalls(Cell previousCell, Cell currentCell)
    {
        if (previousCell == null || currentCell == null)
            return;

        float xDiff = currentCell.transform.position.x - previousCell.transform.position.x;
        float zDiff = currentCell.transform.position.z - previousCell.transform.position.z;

        // Use threshold to handle floating point errors with scaled cells
        float threshold = cellSize * 0.5f;

        if (xDiff > threshold)
        {
            previousCell.ClearRightWall();
            currentCell.ClearLeftWall();
        }
        else if (xDiff < -threshold)
        {
            previousCell.ClearLeftWall();
            currentCell.ClearRightWall();
        }
        else if (zDiff > threshold)
        {
            previousCell.ClearFrontWall();
            currentCell.ClearBackWall();
        }
        else if (zDiff < -threshold)
        {
            previousCell.ClearBackWall();
            currentCell.ClearFrontWall();
        }
    }

    [ContextMenu("Clear Maze")]
    public void ClearMaze()
    {
        Transform parent = mazeParent != null ? mazeParent : transform;

        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in parent)
        {
            toDestroy.Add(child.gameObject);
        }

        foreach (var obj in toDestroy)
        {
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        _CellGrid = null;
        Debug.Log($"✅ Maze cleared - destroyed {toDestroy.Count} cells");
    }

    public Vector3 GetMazeCenter()
    {
        return Vector3.zero;
    }

    public Bounds GetMazeBounds()
    {
        // Account for cell size in bounds calculation
        return new Bounds(Vector3.zero, new Vector3(_CellWidth * cellSize, 2, _CellDepth * cellSize));
    }

    public Vector3 GetRandomFloorPosition()
    {
        if (_CellGrid == null || _CellGrid.Length == 0)
        {
            Debug.LogWarning("Maze not generated yet!");
            return Vector3.zero;
        }

        int x = Random.Range(0, _CellWidth);
        int z = Random.Range(0, _CellDepth);

        Cell cell = _CellGrid[x, z];
        if (cell != null)
        {
            // Return center of cell at floor level
            return cell.transform.position + Vector3.up * 0.5f;
        }

        return Vector3.zero;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 size = new Vector3(_CellWidth * cellSize, 2, _CellDepth * cellSize);
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}
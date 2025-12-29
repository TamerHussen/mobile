using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.AI.Navigation;
using System;


public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Settings")]
    [SerializeField] private Cell _CellPrefab;
    [SerializeField] private int _CellWidth = 10;
    [SerializeField] private int _CellDepth = 10;

    [Header("Cell Scale")]
    [Tooltip("Physical size of each cell in Unity units (default 1 = 1x1 meters)")]
    [SerializeField] private float cellSize = 3f;

    [Header("Generation")]
    [SerializeField] private bool clearOnStart = true;
    [SerializeField] private Transform mazeParent;

    [Header("NavMesh")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    public static System.Action MazeReady;

    private Cell[,] _CellGrid;
    private Vector3 mazeOffset;
    private List<Vector3> availableSpawnPoints = new List<Vector3>();

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

        Transform parent = mazeParent != null ? mazeParent : transform;

        mazeOffset = new Vector3(
            -(_CellWidth - 1) * cellSize / 2f,
            0,
            -(_CellDepth - 1) * cellSize / 2f
        );

        _CellGrid = new Cell[_CellWidth, _CellDepth];

        for (int x = 0; x < _CellWidth; x++)
        {
            for (int z = 0; z < _CellDepth; z++)
            {
                Vector3 localPos = new Vector3(x * cellSize, 0, z * cellSize) + mazeOffset;

                Cell newCell = Instantiate(_CellPrefab, parent);
                newCell.transform.localPosition = localPos;
                newCell.transform.localRotation = Quaternion.identity;
                newCell.transform.localScale = Vector3.one * cellSize;

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

        Debug.Log($" Maze generation complete! ({cellsVisited} cells)");

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        RebuildNavMesh();

        yield return new WaitForSeconds(0.5f);

        BuildSpawnPointList();

        OnMazeGenerationComplete();
    }

    void RebuildNavMesh()
    {
        if (navMeshSurface == null)
        {
            navMeshSurface = FindFirstObjectByType<NavMeshSurface>();
        }

        if (navMeshSurface != null)
        {
            Debug.Log("Rebuilding NavMesh...");
            navMeshSurface.BuildNavMesh();
            Debug.Log("NavMesh rebuild complete!");

            StartCoroutine(WaitForNavMeshAndSpawn());
        }
        else
        {
            Debug.LogWarning(" NavMeshSurface not found!");
        }
    }

    IEnumerator WaitForNavMeshAndSpawn()
    {
        yield return new WaitForSeconds(0.5f);

        BuildSpawnPointList();
        OnMazeGenerationComplete();
    }

    void BuildSpawnPointList()
    {
        availableSpawnPoints.Clear();

        for (int x = 0; x < _CellWidth; x++)
        {
            for (int z = 0; z < _CellDepth; z++)
            {
                Cell cell = _CellGrid[x, z];
                if (cell != null)
                {
                    availableSpawnPoints.Add(cell.GetFloorPosition());
                }
            }
        }

        Debug.Log($" Built {availableSpawnPoints.Count} spawn points");
    }

    void OnMazeGenerationComplete()
    {
        MazeReady?.Invoke();

        var playerSpawner = FindFirstObjectByType<LevelPlayerSpawner>();
        if (playerSpawner != null)
        {
            playerSpawner.enabled = true;
            Debug.Log(" LevelPlayerSpawner enabled");
        }

        var enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner != null)
        {
            enemySpawner.enabled = true;
            Debug.Log(" EnemySpawner enabled");
        }

        var collectibleSpawner = FindFirstObjectByType<CollectibleSpawner>();
        if (collectibleSpawner != null)
        {
            collectibleSpawner.enabled = true;
            Debug.Log(" CollectibleSpawner enabled");
        }
    }

    private Cell GetNextUnvisitedCell(Cell currentCell)
    {
        var unvisitedCells = GetUnvisitedCells(currentCell).ToList();
        if (unvisitedCells.Count == 0)
            return null;

        return unvisitedCells[UnityEngine.Random.Range(0, unvisitedCells.Count)];
    }

    private IEnumerable<Cell> GetUnvisitedCells(Cell currentCell)
    {
        Vector3 localPos = currentCell.transform.position - mazeOffset;
        int x = Mathf.RoundToInt(localPos.x / cellSize);
        int z = Mathf.RoundToInt(localPos.z / cellSize);

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
        availableSpawnPoints.Clear();
        Debug.Log($" Maze cleared - destroyed {toDestroy.Count} cells");
    }

    public Vector3 GetMazeCenter()
    {
        return Vector3.zero;
    }

    public Bounds GetMazeBounds()
    {
        return new Bounds(Vector3.zero, new Vector3(_CellWidth * cellSize, 2, _CellDepth * cellSize));
    }

    public Vector3 GetRandomFloorPosition()
    {
        if (availableSpawnPoints == null || availableSpawnPoints.Count == 0)
        {
            Debug.LogWarning(" No spawn points available! Using center.");
            return Vector3.up * 0.5f;
        }

        Vector3 spawnPoint = availableSpawnPoints[UnityEngine.Random.Range(0, availableSpawnPoints.Count)];
        return spawnPoint;
    }

    public Vector3 GetSpawnPointAwayFrom(Vector3 position, float minDistance)
    {
        if (availableSpawnPoints == null || availableSpawnPoints.Count == 0)
        {
            return Vector3.up * 0.5f;
        }

        List<Vector3> validPoints = new List<Vector3>();
        foreach (var point in availableSpawnPoints)
        {
            float distance = Vector3.Distance(point, position);
            if (distance >= minDistance)
            {
                validPoints.Add(point);
            }
        }

        if (validPoints.Count == 0)
        {
            return availableSpawnPoints[UnityEngine.Random.Range(0, availableSpawnPoints.Count)];
        }

        return validPoints[UnityEngine.Random.Range(0, validPoints.Count)];
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 size = new Vector3(_CellWidth * cellSize, 2, _CellDepth * cellSize);
        Vector3 center = mazeParent != null ? mazeParent.position : transform.position;
        Gizmos.DrawWireCube(center, size);

        if (availableSpawnPoints != null && availableSpawnPoints.Count > 0)
        {
            Gizmos.color = Color.green;
            foreach (var point in availableSpawnPoints)
            {
                Gizmos.DrawWireSphere(point, 0.3f);
            }
        }
    }

    public List<Vector3> GetAllFloorPositions()
    {
        return new List<Vector3>(availableSpawnPoints);
    }

}
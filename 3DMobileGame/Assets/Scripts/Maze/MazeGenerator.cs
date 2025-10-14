using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [SerializeField]
    private Cell _CellPrefab;

    [SerializeField]
    private int _CellWidth;

    [SerializeField]
    private int _CellDepth;

    private Cell[,] _CellGrid;

    void Start()
    {
        _CellGrid = new Cell[_CellWidth, _CellDepth];

        for (int x = 0; x < _CellWidth; x++)
        {
            for (int z = 0; z < _CellDepth; z++)
            {
                _CellGrid[x, z] = Instantiate(_CellPrefab, new Vector3(x, 0, z), Quaternion.identity);
            }
        }

        GenerateMaze(null, _CellGrid[0, 0]);
    }

    private void GenerateMaze(Cell previousCell, Cell currentCell)
    {
        currentCell.Visit();
        Clearwalls(previousCell, currentCell);

        Cell nextCell;
        do
        {
            nextCell = GetNextUnvisitedCell(currentCell);

            if (nextCell != null)
            {
                GenerateMaze(currentCell, nextCell);
            }
        } while (nextCell != null);

    }

    private Cell GetNextUnvisitedCell(Cell currentcell)
    {
        var unvisitedCells = GetUnvisitedCells(currentcell);

        return unvisitedCells.OrderBy(_ => Random.Range(1, 10)).FirstOrDefault();
    }

    private IEnumerable<Cell> GetUnvisitedCells(Cell currentCell)
    {
        int x = (int)currentCell.transform.position.x;
        int z = (int)currentCell.transform.position.z;

        if (x + 1 < _CellWidth)
        {
            var cellToRight = _CellGrid[x + 1, z];

            if (cellToRight.IsVisited == false)
            {
                yield return cellToRight;
            }
        }

        if (x - 1 >= 0)
        {
            var cellToLeft = _CellGrid[x - 1, z];

            if (cellToLeft.IsVisited == false)
            {
                yield return cellToLeft;
            }
        }

        if (z + 1 < _CellDepth)
        {
            var cellToFront = _CellGrid[x, z + 1];

            if (cellToFront.IsVisited == false)
            {
                yield return cellToFront;
            }
        }

        if (z - 1 >= 0)
        {
            var cellToBack = _CellGrid[x, z - 1];

            if (cellToBack.IsVisited == false)
            {
                yield return cellToBack;
            }
        }
    }

    private void Clearwalls(Cell previouscell, Cell currentCell)
    {
        if (previouscell == null)
        {
            return;
        }

        if (previouscell.transform.position.x < currentCell.transform.position.x)
        {
            previouscell.ClearRightWall();
            currentCell.ClearLeftWall();
            return;
        }

        if (previouscell.transform.position.x > currentCell.transform.position.x)
        {
            previouscell.ClearLeftWall();
            currentCell.ClearRightWall();
            return;
        }

        if (previouscell.transform.position.z < currentCell.transform.position.z)
        {
            previouscell.ClearFrontWall();
            currentCell.ClearBackWall();
            return;
        }

        if (previouscell.transform.position.z > currentCell.transform.position.z)
        {
            previouscell.ClearBackWall();
            currentCell.ClearFrontWall();
            return;
        }
    }

    void Update()
    {

    }
}

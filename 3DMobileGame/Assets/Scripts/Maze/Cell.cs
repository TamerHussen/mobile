using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField]
    private GameObject _LeftWall;
    [SerializeField]
    private GameObject _RightWall;
    [SerializeField]
    private GameObject _FrontWall;
    [SerializeField]
    private GameObject _BackWall;
    [SerializeField]
    private GameObject _UnvisitedBlock;

    public bool IsVisited { get; private set; }

    public void Visit()
    {
        IsVisited = true;
        _UnvisitedBlock.SetActive(false);
    }

    public void ClearLeftWall()
    {
        _LeftWall.SetActive(false);
    }
    public void ClearRightWall()
    {
        _RightWall.SetActive(false);
    }
    public void ClearFrontWall()
    {
        _FrontWall.SetActive(false);
    }
    public void ClearBackWall()
    {
        _BackWall.SetActive(false);
    }
}

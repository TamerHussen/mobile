using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private GameObject _LeftWall;
    [SerializeField] private GameObject _RightWall;
    [SerializeField] private GameObject _FrontWall;
    [SerializeField] private GameObject _BackWall;
    [SerializeField] private GameObject _UnvisitedBlock;
    [SerializeField] private GameObject _Floor;

    public bool IsVisited { get; private set; }

    public void Visit()
    {
        IsVisited = true;
        if (_UnvisitedBlock != null)
            _UnvisitedBlock.SetActive(false);
    }

    public void ClearLeftWall()
    {
        if (_LeftWall != null)
            _LeftWall.SetActive(false);
    }

    public void ClearRightWall()
    {
        if (_RightWall != null)
            _RightWall.SetActive(false);
    }

    public void ClearFrontWall()
    {
        if (_FrontWall != null)
            _FrontWall.SetActive(false);
    }

    public void ClearBackWall()
    {
        if (_BackWall != null)
            _BackWall.SetActive(false);
    }

    public Vector3 GetFloorPosition()
    {
        if (_Floor != null)
            return _Floor.transform.position + Vector3.up * 0.5f;

        return transform.position + Vector3.up * 0.5f;
    }
}
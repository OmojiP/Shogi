using UnityEngine;

public class MasGen : MonoBehaviour
{
    [SerializeField] private GameObject _stageCellPrefab;
    [SerializeField] private int _stageCellWidth = 9;
    [SerializeField] private int _stageCellHeight = 9;
    [SerializeField] private Vector3 _centerPosition = new Vector3(0, 0.25f, 0);
    [SerializeField] private float _cellSpacing = 0.1f;
    [SerializeField] private Transform _cellParentTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateMas();
    }

    private Transform[,] GenerateMas()
    {
        var stageCells = new Transform[_stageCellWidth, _stageCellHeight];
        for (int x = 0; x < _stageCellWidth; x++)
        {
            for (int y = 0; y < _stageCellHeight; y++)
            {
                Vector3 masPos = new Vector3(
                    _centerPosition.x + (x - _stageCellWidth / 2) * _cellSpacing,
                    _centerPosition.y,
                    _centerPosition.z + (y - _stageCellHeight / 2) * _cellSpacing
                );
                stageCells[x, y] = Instantiate(_stageCellPrefab, masPos, Quaternion.identity, _cellParentTransform).transform;
            }
        }
        return stageCells;
    }
}

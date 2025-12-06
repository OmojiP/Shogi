using UnityEngine;

/// <summary>
/// ステージのマスを表すコンポーネント.
/// ロジック座標の保持やマスの表示切り替えを担当
/// </summary>
public class StageCell : MonoBehaviour
{
    /// <summary>
    /// ロジック上の座標
    /// </summary>
    private Vector2Int _logicPos;
    /// <summary>
    /// ロジック上の座標
    /// </summary>
    public Vector2Int LogicPos 
    {
        get => _logicPos;
        set => _logicPos = value;
    }
    /// <summary>
    /// マスのマテリアル
    /// </summary>
    private Material _cellMaterial;
    /// <summary>
    /// マスのマテリアル
    /// </summary>
    private Material CellMaterial
    {
        get 
        { 
            if(_cellMaterial == null)
            {
                var renderer = GetComponent<Renderer>();
                if(renderer != null)
                {
                    _cellMaterial = renderer.material;
                }
            }
            return _cellMaterial; 
        }
    }

    /// <summary>
    /// デフォルトの色 FFF0C4
    /// </summary>
    [SerializeField] private Color _defaultColor = new Color(1.0f, 0.94f, 0.77f);
    /// <summary>
    /// ハイライトの色 99CBFF
    /// </summary>
    [SerializeField] private Color _highlightColor = new Color(0.6f, 0.8f, 1.0f);

    /// <summary>
    /// マスを初期化する
    /// </summary>
    /// <param name="logicPos">ロジック上の座標</param>
    public void Initialize(Vector2Int logicPos)
    {
        _logicPos = logicPos;
        SetHighlight(false);
    }

    /// <summary>
    /// マスの色を設定する
    /// </summary>
    /// <param name="isHighlighted"></param>
    public void SetHighlight(bool isHighlighted)
    {
        if (isHighlighted)
        {
            CellMaterial.color = _highlightColor;
        }
        else
        {
            CellMaterial.color = _defaultColor;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将棋盤を表すコンポーネント.
/// 盤上のマスと駒の管理を担当
/// </summary>
public class MainStage : MonoBehaviour
{
    // 他オブジェクトの参照
    /// <summary>
    /// UIマネージャー
    /// </summary>
    [SerializeField] private IngameUIManager _uiManager;

    // 盤データ
    /// <summary>
    /// 将棋盤のマスのプレハブ
    /// </summary>
    [SerializeField] private StageCell _stageCellPrefab;
    /// <summary>
    /// 将棋盤のマスの親オブジェクト
    /// </summary>
    [SerializeField] private Transform _cellParentTransform;
    /// <summary>
    /// 将棋盤のマスTransform
    /// </summary>
    private StageCell[,] _stageCells;
    /// <summary>
    /// 将棋盤のマスの中心位置
    /// </summary>
    [SerializeField] private Vector3 _cellCenterPosition = new Vector3(0, 0.25f, 0);
    /// <summary>
    /// マス間の間隔
    /// </summary>
    [SerializeField] private float _cellSpacing = 0.1f;

    /// <summary>
    /// 将棋駒のプレハブ配列 : 0: 歩, 1: 香車, 2: 桂馬, 3: 銀将, 4: 金将, 5: 角行, 6: 飛車, 7: 王将, 8: 玉将
    /// </summary>
    [SerializeField, Tooltip("0: 歩, 1: 香車, 2: 桂馬, 3: 銀将, 4: 金将, 5: 角行, 6: 飛車, 7: 王将, 8: 玉将 となるように設定")] private Piece[] _piecePrefabs;
    /// <summary>
    /// 駒の親オブジェクト
    /// </summary>
    [SerializeField] private Transform _pieceParentTransform;

    /// <summary>
    /// 将棋盤上の駒の配列
    /// </summary>
    private Piece[,] _pieces;
    /// <summary>
    /// 歩のオブジェクト配列
    /// </summary>
    private Piece[] _fuPieces;
    /// <summary>
    /// 歩のオブジェクト配列
    /// </summary>
    public Piece[] FuPieces => _fuPieces;

    public Piece GetPieceAt(Vector2Int pos)
    {
        return _pieces[pos.x, pos.y];
    }

    /// <summary>
    /// 駒を配置する(成るかの確認は駒側で行う)
    /// </summary>
    /// <param name="piece">配置する駒</param>
    /// <param name="pos">配置先の座標</param>
    public IEnumerator PlacePiece(Piece piece, Vector2Int pos)
    {
        if(_pieces[pos.x, pos.y] != null)
        {
            Debug.LogError("その位置にはすでに駒が存在します");
            yield break;
        }
        _pieces[pos.x, pos.y] = piece;
        yield return piece.MoveToMainStage(pos, _stageCells);
    }

    /// <summary>
    /// 駒を将棋盤から取り除く
    /// </summary>
    /// <param name="pos">取り除く駒の座標</param>
    /// <returns>取り除いた駒</returns>
    public Piece PickupPiece(Vector2Int pos)
    {
        if (_pieces[pos.x, pos.y] == null)
        {
            Debug.LogError("その位置には駒が存在しません");
            return null;
        }
        var piece = _pieces[pos.x, pos.y];
        _pieces[pos.x, pos.y] = null;
        return piece;
    }

    /// <summary>
    /// 将棋盤のマスを生成する
    /// </summary>
    /// <returns>盤のマスのTransform配列</returns>
    public void GenerateStageCells()
    {
        // 配列の初期化
        _stageCells = new StageCell[LogicFunction.BOARD_SIZE, LogicFunction.BOARD_SIZE];

        for (int x = 0; x < LogicFunction.BOARD_SIZE; x++)
        {
            for (int y = 0; y < LogicFunction.BOARD_SIZE; y++)
            {
                // マスのワールド座標を計算して生成
                Vector3 masPos = new Vector3(
                    _cellCenterPosition.x + (x - LogicFunction.BOARD_SIZE / 2) * _cellSpacing,
                    _cellCenterPosition.y,
                    _cellCenterPosition.z + (y - LogicFunction.BOARD_SIZE / 2) * _cellSpacing
                );
                _stageCells[x, y] = Instantiate(_stageCellPrefab, masPos, Quaternion.identity, _cellParentTransform);
                // マスの初期化
                _stageCells[x, y].Initialize(new Vector2Int(x, y));
            }
        }
    }
    /// <summary>
    /// 駒を初期配置データに基づいて生成する
    /// </summary>
    /// <param name="initialPlacementInfos">初期配置データのテキストアセット</param>
    public void SpawnPieces(PiecePlacementInfo[] initialPlacementInfos)
    {
        List<Piece> fuPieces = new List<Piece>();
        _pieces = new Piece[LogicFunction.BOARD_SIZE, LogicFunction.BOARD_SIZE];

        foreach (var placementInfo in initialPlacementInfos)
        {
            // 駒の生成(_piecePrefabs配列から駒名に対応するプレハブを取得)
            Piece piecePrefab = null;
            switch (placementInfo.PieceType)
            {
                case PieceType.FU: 
                    piecePrefab = _piecePrefabs[0]; 
                    break;
                case PieceType.KYOSHA: 
                    piecePrefab = _piecePrefabs[1]; 
                    break;
                case PieceType.KEIMA: 
                    piecePrefab = _piecePrefabs[2]; 
                    break;
                case PieceType.GIN: 
                    piecePrefab = _piecePrefabs[3]; 
                    break;
                case PieceType.KIN: 
                    piecePrefab = _piecePrefabs[4]; 
                    break;
                case PieceType.KAKU: 
                    piecePrefab = _piecePrefabs[5]; 
                    break;
                case PieceType.HISHA: 
                    piecePrefab = _piecePrefabs[6]; 
                    break;
                case PieceType.OU: 
                    piecePrefab = _piecePrefabs[7]; 
                    break;
                case PieceType.GYOKU: 
                    piecePrefab = _piecePrefabs[8]; 
                    break;
            }

            if (piecePrefab != null)
            {
                // 駒オブジェクトの位置と回転を指定して生成
                Vector3 spawnPosition = _stageCells[placementInfo.LogicPosition.x, placementInfo.LogicPosition.y].transform.position + Vector3.up * 0.01f; // 少し浮かせて配置
                var piece = Instantiate(piecePrefab, spawnPosition, Quaternion.identity, _pieceParentTransform);
                
                // 駒の情報を設定
                piece.Initialize(placementInfo.LogicPosition, placementInfo.PlayerSide, placementInfo.IsPromoted, _uiManager);

                // 駒を将棋盤上の配列に登録
                _pieces[placementInfo.LogicPosition.x, placementInfo.LogicPosition.y] = piece;

                // 歩のオブジェクトなら歩オブジェクトリストに追加
                if(piece.PieceType == PieceType.FU)
                {
                    fuPieces.Add(piece);
                }
            }
        }

        _fuPieces = fuPieces.ToArray();
    }

    /// <summary>
    /// マスのマテリアルを変更する
    /// </summary>
    /// <param name="isHighlight">ハイライトするかどうか</param>
    /// <param name="logicPos">ロジック座標</param>
    public void ChangeCellColor(bool isHighlight, Vector2Int logicPos)
    {
        if (logicPos.x < 0 || logicPos.x >= LogicFunction.BOARD_SIZE || logicPos.y < 0 || logicPos.y >= LogicFunction.BOARD_SIZE)
        {
            Debug.LogError("指定されたロジック座標は範囲外です");
            return;
        }

        _stageCells[logicPos.x, logicPos.y].SetHighlight(isHighlight);
    }

    /// <summary>
    /// 指定したロジック座標に駒が存在するか確認する
    /// </summary>
    /// <param name="logicPos">ロジック座標</param>
    /// <param name="occupyingPiece">駒が存在する場合、その駒の参照</param>
    /// <returns>駒が存在する場合はtrue、存在しない場合はfalse</returns>
    public bool TryGetPieceAt(Vector2Int logicPos, out Piece occupyingPiece)
    {
        // 範囲外チェック
        if (logicPos.x < 0 || logicPos.x >= LogicFunction.BOARD_SIZE || logicPos.y < 0 || logicPos.y >= LogicFunction.BOARD_SIZE)
        {
            occupyingPiece = null;
            return false;
        }

        occupyingPiece = _pieces[logicPos.x, logicPos.y];
        return occupyingPiece != null;
    }
}

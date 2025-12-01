using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 盤データ
    // 盤のサイズ
    private readonly int BOARD_SIZE = 9;
    // 将棋盤のマスのプレハブ
    [SerializeField] private GameObject _stageCellPrefab;
    // 将棋盤のマスの親オブジェクト
    [SerializeField] private Transform _cellParentTransform;
    // 将棋盤のマスTransform
    private Transform[,] _stageCells;
    // 将棋盤のマスの中心位置
    [SerializeField] private Vector3 _cellCenterPosition = new Vector3(0, 0.25f, 0);
    // マス間の間隔
    [SerializeField] private float _cellSpacing = 0.1f;
    // マスのロジック座標からワールド座標への変換テーブル
    private Vector3[,] _cellWorldPositions;
    // 駒台
    [SerializeField] private Transform _pieceStageTop;
    [SerializeField] private Transform _pieceStageBottom;
    private Vector3 _pieceStageTopPosition;
    private Vector3 _pieceStageBottomPosition;
    // 歩のオブジェクト
    private List<Piece> _fuPieces = new List<Piece>();

    // マテリアル
    [SerializeField] private Material _highlightCellMaterial; // 移動可能マスのハイライト用マテリアル
    [SerializeField] private Material _defaultCellMaterial; // デフォルトの将棋盤マテリアル

    // 将棋駒のプレハブ配列
    // 0: 歩, 1: 香車, 2: 桂馬, 3: 銀将, 4: 金将, 5: 角行, 6: 飛車, 7: 王将, 8: 玉将
    [SerializeField] private GameObject[] _piecePrefabs;
    // 駒の親オブジェクト
    [SerializeField] private Transform _pieceParentTransform;

    // 駒の初期配置のCSVデータ
    // "x座標, y座標, 駒名, Upper/Lower" の形式で記述
    [SerializeField] private TextAsset _initialPlacementTextAsset;

    // クリックで座標を取得するコンポーネント
    [SerializeField] private ClickRaycaster _clickRaycaster;

    // 現在のターン数
    private int _turnCount = 0;    
    // 現在のプレイヤーサイド
    private PlayerSide _currentPlayerSide = PlayerSide.Bottom;
    // インゲームのシーケンス管理
    private IngameSequence _currentSequence = IngameSequence.WaitingPieceSelect;
    // 現在選択中の駒
    private Piece _currentSelectedPiece = null;
    // 現在選択中の駒の移動先候補
    private Vector2Int[] _currentSelectedPieceDestinationCandidates = null;
    // 現在の移動先
    private Vector2Int _currentSelectedPieceDestination = new Vector2Int(-1, -1);
    // 駒を移動中かどうか
    private bool _isPieceMoving = false;
    // 駒を何秒で移動させるか
    private float _pieceMoveDuration = 0.5f;

    void Start()
    {
        Debug.Log("[GameManager] Start");

        // 駒台の座標を取得
        _pieceStageTopPosition = _pieceStageTop.position;
        _pieceStageBottomPosition = _pieceStageBottom.position;

        // 将棋盤のマスを生成
        _stageCells = GenerateStageCells();

        // 将棋盤に駒を生成
        SpawnPieces(_initialPlacementTextAsset);
    }

    void Update()
    {
        switch (_currentSequence)
        {
            case IngameSequence.WaitingPieceSelect:
                // 駒選択待ちの処理
                HandlePieceSelect();
                break;
            case IngameSequence.PieceDestinationSelect:
                // 駒の移動先選択待ちの処理
                HandlePieceDestinationSelect();
                break;
            case IngameSequence.PieceMoving:
                // 駒を移動中の処理
                HandlePieceMoving();
                break;
            case IngameSequence.TurnEnded:
                // ターン終了処理
                HandleTurnEnded();
                break;
        }
    }

    // 将棋盤のマスを生成する
    private Transform[,] GenerateStageCells()
    {
        var stageCells = new Transform[BOARD_SIZE, BOARD_SIZE];
        _cellWorldPositions = new Vector3[BOARD_SIZE, BOARD_SIZE];

        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                Vector3 masPos = new Vector3(
                    _cellCenterPosition.x + (x - BOARD_SIZE / 2) * _cellSpacing,
                    _cellCenterPosition.y,
                    _cellCenterPosition.z + (y - BOARD_SIZE / 2) * _cellSpacing
                );
                stageCells[x, y] = Instantiate(_stageCellPrefab, masPos, Quaternion.identity, _cellParentTransform).transform;
                // マスのワールド座標を保存
                _cellWorldPositions[x, y] = masPos;
            }
        }
        return stageCells;
    }

    private void ChangeCellMaterial(bool isHighlight, Vector2Int logicPos)
    {
        if(isHighlight)
        {
            // ハイライト用マテリアルに変更
            _stageCells[logicPos.x, logicPos.y].GetComponent<Renderer>().material = _highlightCellMaterial;
        }
        else
        {
            // デフォルトマテリアルに変更
            _stageCells[logicPos.x, logicPos.y].GetComponent<Renderer>().material = _defaultCellMaterial;
        }
    }

    // ワールド座標値から最も近いロジック座標に変換する
    private Vector2Int WorldToLogicPosition(Vector3 worldPosition)
    {
        float minDistance = float.MaxValue;
        Vector2Int closestLogicPos = new Vector2Int(-1, -1);

        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                float distance = Vector3.SqrMagnitude(worldPosition - _cellWorldPositions[x, y]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestLogicPos = new Vector2Int(x, y);
                }
            }
        }

        return closestLogicPos;
    }

    // 駒を初期配置データに基づいて生成する
    private void SpawnPieces(TextAsset initialPlacement)
    {
        // 初期配置データの解析
        string[] lines = initialPlacement.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            // x座標, y座標, 駒名, Upper/Lower の形式で記述されていることを想定して解析
            string[] tokens = line.Split(',');
            if (tokens.Length != 4) continue; // 不正な行はスキップ

            int x = int.Parse(tokens[0]);
            int y = int.Parse(tokens[1]);
            string pieceName = tokens[2].Trim();
            string side = tokens[3].Trim();

            // 駒の生成
            GameObject piecePrefab = null;
            switch (pieceName)
            {
                case "FU": piecePrefab = _piecePrefabs[0]; break;
                case "KYOSHA": piecePrefab = _piecePrefabs[1]; break;
                case "KEIMA": piecePrefab = _piecePrefabs[2]; break;
                case "GIN": piecePrefab = _piecePrefabs[3]; break;
                case "KIN": piecePrefab = _piecePrefabs[4]; break;
                case "KAKU": piecePrefab = _piecePrefabs[5]; break;
                case "HISHA": piecePrefab = _piecePrefabs[6]; break;
                case "OU": piecePrefab = _piecePrefabs[7]; break;
                case "GYOKU": piecePrefab = _piecePrefabs[8]; break;
            }

            if (piecePrefab != null)
            {
                Vector3 spawnPosition = _cellWorldPositions[x, y] + Vector3.up * 0.01f; // 少し浮かせて配置
                Quaternion spawnRotation = (side == "Upper") ? Quaternion.identity : Quaternion.Euler(0, 180, 0); // 下側の駒は180度回転
                var piece = Instantiate(piecePrefab, spawnPosition, spawnRotation, _pieceParentTransform);
                // 駒のプレイヤーサイドを設定
                piece.GetComponent<Piece>()._playerSide = (side == "Upper") ? PlayerSide.Top : PlayerSide.Bottom;

                // 歩のオブジェクトならリストに追加
                if(piece.GetComponent<Piece>()._pieceType == PieceType.FU)
                {
                    _fuPieces.Add(piece.GetComponent<Piece>());
                }
            }
        }
    }

    // 移動先候補を取得
    private Vector2Int[] GetMoveDestinationCandidates(Piece playerPiece, PlayerSide playerSide)
    {
        Debug.Log($"Getting move destinations for piece: {playerPiece._pieceType} at {WorldToLogicPosition(playerPiece.transform.position)}");

        // 移動先候補リスト
        List<Vector2Int> destinationCandidates = new List<Vector2Int>();

        // 持ち駒の場合は処理を分ける
        if (!playerPiece._isMainStagePiece)
        {
            // 全空きマスに移動可能(二歩注意)
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                for (int y = 0; y < BOARD_SIZE; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    // そのマスに駒が存在しない場合のみ追加
                    if(!IsCellOccupied(pos, out var occupyingPiece))
                    {
                        destinationCandidates.Add(pos);
                    }
                }
            }

            // 二歩を除外
            if(playerPiece._pieceType == PieceType.FU)
            {
                foreach(var fu in _fuPieces)
                {
                    // 同じプレイヤーサイドの歩のみ対象
                    if(fu._playerSide == playerSide && fu._isMainStagePiece)
                    {
                        // 歩のいる列を取得
                        Vector2Int fuLogicPos = WorldToLogicPosition(fu.transform.position);
                        int fuColumn = fuLogicPos.x;
                        // その列のマスを移動先候補から削除
                        destinationCandidates.RemoveAll(pos => pos.x == fuColumn);
                    }
                }
            }
        }

        // PlayerPieceのロジック座標を取得
        Vector2Int playerPieceLogicPos = WorldToLogicPosition(playerPiece.transform.position);

        // 実際に扱う駒の種類を決定
        PieceType actualPieceType = playerPiece._pieceType;

        // 歩, 香車, 桂馬, 銀将 はなっていた場合, 金将として扱う
        if(playerPiece._isPromoted)
        {
            if(playerPiece._pieceType == PieceType.FU ||
               playerPiece._pieceType == PieceType.KYOSHA ||
               playerPiece._pieceType == PieceType.KEIMA ||
               playerPiece._pieceType == PieceType.GIN)
            {
                // 金将として扱う
                actualPieceType = PieceType.KIN;
            }
        }

        // 駒の種類ごとに移動先候補を取得(範囲外チェックと角龍の成の扱いは後ろでまとめて行う)
        switch (actualPieceType)
        {
            case PieceType.FU:
                // 歩は1マス前進のみ
                if(playerSide == PlayerSide.Bottom)
                {
                    destinationCandidates = new List<Vector2Int>
                    {
                        new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y + 1)
                    };
                }
                else // PlayerSide.Top
                {
                    destinationCandidates = new List<Vector2Int>
                    {
                        new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y - 1)
                    };
                }       
                break;
            case PieceType.KYOSHA:
                // 香車は前方に何マスでも進める
                if(playerSide == PlayerSide.Bottom)
                {
                    for(int y = playerPieceLogicPos.y + 1; y < BOARD_SIZE; y++)
                    {
                        // 候補に追加
                        destinationCandidates.Add(new Vector2Int(playerPieceLogicPos.x, y));
                        // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                        if(IsCellOccupied(new Vector2Int(playerPieceLogicPos.x, y), out var occupyingPiece))
                        {
                            break;   
                        }
                    }
                }
                else // PlayerSide.Top
                {
                    for(int y = playerPieceLogicPos.y - 1; y >= 0; y--)
                    {
                        // 候補に追加
                        destinationCandidates.Add(new Vector2Int(playerPieceLogicPos.x, y));
                        // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                        if(IsCellOccupied(new Vector2Int(playerPieceLogicPos.x, y), out var occupyingPiece))
                        {
                            break;
                        }
                    }
                }
                break;
            case PieceType.KEIMA:
                // 桂馬は2マス前方に1マス左右
                if(playerSide == PlayerSide.Bottom)
                {
                    destinationCandidates = new List<Vector2Int>
                    {
                        new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y + 2),
                        new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y + 2)
                    };
                }
                else // PlayerSide.Top
                {
                    destinationCandidates = new List<Vector2Int>
                    {
                        new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y - 2),
                        new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y - 2)
                    };
                }
                break;
            case PieceType.GIN:
                // 銀将は1マス前方と斜め前方、斜め後方に移動可能
                if(playerSide == PlayerSide.Bottom)
                {
                    destinationCandidates = new List<Vector2Int>
                    {
                        new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y + 1),
                        new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y + 1),
                        new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y + 1),
                        new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y - 1),
                        new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y - 1)
                    };
                }
                else // PlayerSide.Top
                {
                    destinationCandidates = new List<Vector2Int>
                    {
                        new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y - 1),
                        new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y - 1),
                        new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y - 1),
                        new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y + 1),
                        new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y + 1)
                    };
                }
                break;
            case PieceType.KIN:
                // 金将は1マス前方、左右、前方斜めに移動可能
                if(playerSide == PlayerSide.Bottom)
                {
                    destinationCandidates = new List<Vector2Int>
                    {
                        new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y + 1),
                        new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y),
                        new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y),
                        new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y + 1),
                        new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y - 1),
                        new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y + 1)
                    };
                }
                else // PlayerSide.Top
                {
                    destinationCandidates = new List<Vector2Int>
                    {
                        new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y - 1),
                        new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y),
                        new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y),
                        new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y - 1),
                        new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y + 1),
                        new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y - 1)
                    };
                }
                break;
            case PieceType.KAKU:
                // 角行は斜め方向に何マスでも移動可能
                // 斜めに追加していき、味方がいる場合はそのマスの前まで、相手がいる場合はそのマスまで追加して止める
                // 左上
                for(int  distance = 1; distance < BOARD_SIZE; distance++)
                {
                    Vector2Int p0 = new Vector2Int(playerPieceLogicPos.x + distance, playerPieceLogicPos.y + distance);

                    // 候補に追加
                    destinationCandidates.Add(p0);
                    // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                    if(IsCellOccupied(p0, out var occupyingPiece))
                    {
                        break; 
                    }
                }
                // 右上
                for(int  distance = 1; distance < BOARD_SIZE; distance++)
                {
                    Vector2Int p1 = new Vector2Int(playerPieceLogicPos.x - distance, playerPieceLogicPos.y + distance);
                    
                    // 候補に追加
                    destinationCandidates.Add(p1);
                    // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                    if(IsCellOccupied(p1, out var occupyingPiece))
                    {
                        break;
                    }
                }
                // 左下
                for(int  distance = 1; distance < BOARD_SIZE; distance++)
                {
                    Vector2Int p2 = new Vector2Int(playerPieceLogicPos.x + distance, playerPieceLogicPos.y - distance);

                    // 候補に追加
                    destinationCandidates.Add(p2);
                    // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                    if(IsCellOccupied(p2, out var occupyingPiece))
                    {
                        break;
                    }
                }
                // 右下
                for(int  distance = 1; distance < BOARD_SIZE; distance++)
                {
                    Vector2Int p3 = new Vector2Int(playerPieceLogicPos.x - distance, playerPieceLogicPos.y - distance);

                    // 候補に追加
                    destinationCandidates.Add(p3);
                    // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                    if(IsCellOccupied(p3, out var occupyingPiece))
                    {
                        break;
                    }
                }
                break;
            case PieceType.HISHA:
                // 飛車は縦横方向に何マスでも移動可能
                // 縦横に追加していき、味方がいる場合はそのマスの前まで、相手がいる場合はそのマスまで追加して止める
                // 上
                for(int  distance = 1; distance < BOARD_SIZE; distance++)
                {
                    Vector2Int p0 = new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y + distance);

                    // 候補に追加
                    destinationCandidates.Add(p0);
                    // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                    if(IsCellOccupied(p0, out var occupyingPiece))
                    {
                        break;
                    }
                }
                // 下
                for(int  distance = 1; distance < BOARD_SIZE; distance++)
                {
                    Vector2Int p1 = new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y - distance);

                    // 候補に追加
                    destinationCandidates.Add(p1);
                    // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                    if(IsCellOccupied(p1, out var occupyingPiece))
                    {
                        break;
                    }
                }
                // 左
                for(int  distance = 1; distance < BOARD_SIZE; distance++)
                {
                    Vector2Int p2 = new Vector2Int(playerPieceLogicPos.x - distance, playerPieceLogicPos.y);

                    // 候補に追加
                    destinationCandidates.Add(p2);
                    // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                    if(IsCellOccupied(p2, out var occupyingPiece))
                    {
                        break;
                    }
                }
                // 右
                for(int  distance = 1; distance < BOARD_SIZE; distance++)
                {
                    Vector2Int p3 = new Vector2Int(playerPieceLogicPos.x + distance, playerPieceLogicPos.y);

                    // 候補に追加
                    destinationCandidates.Add(p3);
                    // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                    if(IsCellOccupied(p3, out var occupyingPiece))
                    {
                        break;
                    }
                }
                break;
            case PieceType.OU or PieceType.GYOKU:
                // 王将・玉将は1マス八方に移動可能
                destinationCandidates = new List<Vector2Int>
                {
                    new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y),
                    new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y),
                    new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y - 1),
                    new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y + 1),
                    new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y - 1),
                    new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y - 1),
                    new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y + 1),
                    new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y + 1)
                };
                break;
            default:
                throw new System.NotImplementedException("未対応の駒タイプです");
        }

        // 角行・飛車は成っていた場合、候補に王将・玉将の動きを追加
        if(playerPiece._isPromoted)
        {
            if(actualPieceType == PieceType.KAKU ||
               actualPieceType == PieceType.HISHA)
            {
                // 王将・玉将の動きを追加
                destinationCandidates.AddRange(new List<Vector2Int>
                {
                    new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y),
                    new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y),
                    new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y - 1),
                    new Vector2Int(playerPieceLogicPos.x, playerPieceLogicPos.y + 1),
                    new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y - 1),
                    new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y - 1),
                    new Vector2Int(playerPieceLogicPos.x - 1, playerPieceLogicPos.y + 1),
                    new Vector2Int(playerPieceLogicPos.x + 1, playerPieceLogicPos.y + 1)
                });
            }
        }

        // 将棋盤の範囲外の候補を削除
        destinationCandidates.RemoveAll(pos => pos.x < 0 || pos.x >= BOARD_SIZE || pos.y < 0 || pos.y >= BOARD_SIZE);

        // 味方の駒がいるマスの候補を削除
        destinationCandidates.RemoveAll(pos => IsCellOccupied(pos, out var occupyingPiece) && occupyingPiece._playerSide == playerSide);

        Debug.Log($"候補座標: {string.Join(", ", destinationCandidates)}");

        return destinationCandidates.ToArray();
    }
    // 指定したロジック座標に駒が存在するかどうかを判定する
    private bool IsCellOccupied(Vector2Int logicPos, out Piece occupyingPiece)
    {
        // 範囲外チェック
        if (logicPos.x < 0 || logicPos.x >= BOARD_SIZE || logicPos.y < 0 || logicPos.y >= BOARD_SIZE)
        {
            occupyingPiece = null;
            return false;
        }

        Vector3 cellWorldPos = _cellWorldPositions[logicPos.x, logicPos.y];

        _clickRaycaster.TryGetPiece(cellWorldPos, out occupyingPiece);
        return occupyingPiece != null;
    }

    // 駒選択待ちの処理
    private void HandlePieceSelect()
    {
        // 自分の駒がクリックされた→移動先のクリック待ちへ遷移
        if (_clickRaycaster.TryGetClickedPosition(out Vector3 clickedPosition, out var clickedPiece))
        {
            if(clickedPiece != null && clickedPiece._playerSide == _currentPlayerSide)
            {
                // 駒が選択された状態へ遷移
                _currentSequence = IngameSequence.PieceDestinationSelect;
                _currentSelectedPiece = clickedPiece;
                Debug.Log($"Selected piece: {clickedPiece._pieceType} at {WorldToLogicPosition(clickedPosition)}");
            }
        }
    }

    // 駒の移動先選択待ちの処理
    private void HandlePieceDestinationSelect()
    {
        // 一度だけ候補を取得して表示する
        if (_currentSelectedPieceDestinationCandidates == null)
        {
            _currentSelectedPieceDestinationCandidates = GetMoveDestinationCandidates(_currentSelectedPiece, _currentPlayerSide);
            // 候補先がない場合、駒選択待ちへ遷移
            if(_currentSelectedPieceDestinationCandidates.Length == 0)
            {
                Debug.Log("移動先の候補がありません。駒選択待ちに戻ります。");
                _currentSequence = IngameSequence.WaitingPieceSelect;
                _currentSelectedPiece = null;
                _currentSelectedPieceDestinationCandidates = null;
                return;
            }
            else
            {            
                Debug.Log($"Move destinations for {_currentSelectedPiece._pieceType}: {string.Join(", ", _currentSelectedPieceDestinationCandidates)}");
                // 移動先候補のマスをハイライト表示
                foreach(var pos in _currentSelectedPieceDestinationCandidates)
                {
                    ChangeCellMaterial(true, pos);
                }
            }
        }

        bool isClicked = _clickRaycaster.TryGetClickedPosition(out Vector3 clickedPosition, out var clickedPiece);

        // クリックされなければ何もしない
        if(!isClicked) return;

        // 移動先候補のマスのハイライトを元に戻す
        foreach(var pos in _currentSelectedPieceDestinationCandidates)
        {
            ChangeCellMaterial(false, pos);
        }

        // クリックされた座標のロジック座標を取得
        Vector2Int logicPos = WorldToLogicPosition(clickedPosition);
        // 選択された駒の移動先候補がクリックされたかどうかを判定
        bool isClickedOnDestination = false;
        // ここで選択された駒の移動先候補かどうかを判定する処理を追加
        // 仮に常に true としておく
        isClickedOnDestination = System.Array.Exists(_currentSelectedPieceDestinationCandidates, pos => pos == logicPos);

        // 選択された駒の移動先候補がクリックされた→駒を移動中へ遷移
        if (isClickedOnDestination)
        {
            // 駒の移動先を設定
            _currentSelectedPieceDestination = logicPos;

            Debug.Log($"Moving piece: {_currentSelectedPiece._pieceType} to {_currentSelectedPieceDestination}");

            // 駒を移動中へ遷移
            _currentSequence = IngameSequence.PieceMoving;
        }
        // 選択された駒の移動先以外がクリックされた→駒選択待ちへ遷移
        else
        {
            // 駒選択待ちへ遷移
            _currentSequence = IngameSequence.WaitingPieceSelect;
            _currentSelectedPiece = null;
            _currentSelectedPieceDestinationCandidates = null;
            _currentSelectedPieceDestination = new Vector2Int(-1, -1);
            Debug.Log("移動できないマスがクリックされました。駒選択待ちに戻ります。");
        }
    }

    // 駒を移動中の処理
    private void HandlePieceMoving()
    {
        // 移動中なら何もしない
        if (_isPieceMoving) return;

        // 駒を移動中フラグを立てる        
        _isPieceMoving = true;

        // コルーチンで駒を移動
        StartCoroutine(MovePieceToDestination(_currentSelectedPiece, _currentSelectedPieceDestination));
    }

    // 駒を目的地まで移動させるコルーチン
    private IEnumerator MovePieceToDestination(Piece piece, Vector2Int destination)
    {
        Vector3 targetPosition = _cellWorldPositions[destination.x, destination.y]+ Vector3.up * 0.01f;

        // targetPositionに相手の駒があるか確認
        _clickRaycaster.TryGetPiece(targetPosition, out var occupyingPiece);
        bool isEnemyPiecePresent = occupyingPiece != null && occupyingPiece._playerSide != piece._playerSide;

        // 移動アニメーション
        float elapsedTime = 0f;
        Vector3 startingPosition = piece.transform.position;
        while (elapsedTime < _pieceMoveDuration)
        {
            piece.transform.position = Vector3.Lerp(startingPosition, targetPosition, elapsedTime / _pieceMoveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        piece.transform.position = targetPosition;

        // 相手の駒を自分のサイドに書き換えて陣地に移動
        if(isEnemyPiecePresent)
        {
            Debug.Log($"Captured piece: {occupyingPiece._pieceType} at {destination}");
            // 駒の状態を変更
            occupyingPiece._playerSide = piece._playerSide;
            occupyingPiece._isPromoted = false;
            occupyingPiece._isMainStagePiece = false;
            // 駒を陣地に移動させる(仮に盤外の位置に移動させる)
            Vector3 offBoardPosition = new Vector3(
                (piece._playerSide == PlayerSide.Bottom) ? _pieceStageBottomPosition.x : _pieceStageTopPosition.x,
                occupyingPiece.transform.position.y,
                (piece._playerSide == PlayerSide.Bottom) ? _pieceStageBottomPosition.z : _pieceStageTopPosition.z
            );
            occupyingPiece.transform.position = offBoardPosition;
            // 駒の向きを変更
            occupyingPiece.transform.rotation = (piece._playerSide == PlayerSide.Bottom) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        }

        Vector2Int pieceLogicPos = WorldToLogicPosition(piece.transform.position);
        bool isInPromotionZone = (piece._playerSide == PlayerSide.Bottom && pieceLogicPos.y >= 6) ||
                                 (piece._playerSide == PlayerSide.Top && pieceLogicPos.y <= 2);
        // 相手陣地に入っていて、まだ成っていなくて、今ターンに盤上に出た駒でなければ成る
        // TODO: 後で成るか選択できるようにする & もう移動先がない場合(歩や香車、桂馬)は自動で成る
        // TODO: 駒の種類によっては成れない場合もあるのでその処理も追加する
        // TODO: 移動前に相手陣地にいる場合もなれるので追加する
        if(isInPromotionZone && !piece._isPromoted && piece._isMainStagePiece)
        {
            // 成る処理を行う(後で成るか選択できるようにする)
            piece._isPromoted = true;
            // 裏返す
            piece.transform.Rotate(0, 0, 180);
            Debug.Log($"Piece promoted: {piece._pieceType} at {pieceLogicPos}");
        }

        // 駒の移動完了後の処理
        piece._isMainStagePiece = true;
        _isPieceMoving = false;
        _currentSelectedPiece = null;
        _currentSelectedPieceDestinationCandidates = null;
        _currentSelectedPieceDestination = new Vector2Int(-1, -1);

        _currentSequence = IngameSequence.TurnEnded;
    }

    // ターン終了処理
    private void HandleTurnEnded()
    {
        // ターン数を増やす
        _turnCount++;
        // プレイヤーサイドを交代
        _currentPlayerSide = (_currentPlayerSide == PlayerSide.Bottom) ? PlayerSide.Top : PlayerSide.Bottom;
        
        Debug.Log($"Turn {_turnCount} ended. Next player: {_currentPlayerSide}");

        // 各種変数のリセット
        _currentSelectedPiece = null;
        _currentSelectedPieceDestinationCandidates = null;
        _currentSelectedPieceDestination = new Vector2Int(-1, -1);
        _isPieceMoving = false;

        // 駒選択待ちへ遷移
        _currentSequence = IngameSequence.WaitingPieceSelect;
    }

    // インゲームのシーケンス
    private enum IngameSequence
    {
        // 駒選択待ち
        WaitingPieceSelect,
        // 駒の移動先選択待ち
        PieceDestinationSelect,
        // 駒を移動中
        PieceMoving,
        // ターン終了処理
        TurnEnded,
    }
}

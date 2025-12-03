using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// インゲーム全体を管理するコンポーネント.
/// </summary>
public class GameManager : MonoBehaviour
{
    // 盤データ
    /// <summary>
    /// 盤のサイズ
    /// </summary>
    private readonly int BOARD_SIZE = 9;
    /// <summary>
    /// 将棋盤のマスのプレハブ
    /// </summary>
    [SerializeField] private GameObject _stageCellPrefab;
    /// <summary>
    /// 将棋盤のマスの親オブジェクト
    /// </summary>
    [SerializeField] private Transform _cellParentTransform;
    /// <summary>
    /// 将棋盤のマスTransform
    /// </summary>
    private Transform[,] _stageCells;
    /// <summary>
    /// 将棋盤のマスの中心位置
    /// </summary>
    [SerializeField] private Vector3 _cellCenterPosition = new Vector3(0, 0.25f, 0);
    /// <summary>
    /// マス間の間隔
    /// </summary>
    [SerializeField] private float _cellSpacing = 0.1f;
    /// <summary>
    /// マスのロジック座標からワールド座標への変換テーブル
    /// </summary>
    private Vector3[,] _cellWorldPositions;
    
    // 駒台
    /// <summary>
    /// 上側の駒台
    /// </summary>
    [SerializeField] private Transform _pieceStageTop;
    /// <summary>
    /// 下側の駒台
    /// </summary>
    [SerializeField] private Transform _pieceStageBottom;
    /// <summary>
    /// 上側の駒台のワールド座標
    /// </summary>
    private Vector3 _pieceStageTopPosition;
    /// <summary>
    /// 下側の駒台のワールド座標
    /// </summary>
    private Vector3 _pieceStageBottomPosition;
    /// <summary>
    /// 駒の配置開始箇所のx座標オフセット
    /// </summary>
    [SerializeField] private float _pieceStagePlaceStartOffsetX = -0.4f;
    /// <summary>
    /// 駒の配置箇所のx座標間隔
    /// </summary>
    [SerializeField] private float _pieceStagePlaceSpanX = 0.1f;
    /// <summary>
    /// 上側の駒台の駒オブジェクト
    /// </summary>
    private List<GameObject> _pieceStageTopPieces = new List<GameObject>();
    /// <summary>
    /// 下側の駒台の駒オブジェクト
    /// </summary>
    private List<GameObject> _pieceStageBottomPieces = new List<GameObject>();
    /// <summary>
    /// 歩のオブジェクト
    /// </summary>
    private List<Piece> _fuPieces = new List<Piece>();

    // マテリアル
    /// <summary>
    /// 移動可能マスのハイライト用マテリアル
    /// </summary>
    [SerializeField] private Material _highlightCellMaterial;
    /// <summary>
    /// デフォルトの将棋盤マテリアル
    /// </summary>
    [SerializeField] private Material _defaultCellMaterial;

    /// <summary>
    /// 将棋駒のプレハブ配列 : 0: 歩, 1: 香車, 2: 桂馬, 3: 銀将, 4: 金将, 5: 角行, 6: 飛車, 7: 王将, 8: 玉将
    /// </summary>
    [SerializeField] private GameObject[] _piecePrefabs;
    /// <summary>
    /// 駒の親オブジェクト
    /// </summary>
    [SerializeField] private Transform _pieceParentTransform;

    /// <summary>
    /// 駒の初期配置のCSVデータ
    /// "x座標, y座標, 駒名, Upper/Lower" の形式で記述
    /// </summary>
    [SerializeField] private TextAsset _initialPlacementTextAsset;

    /// <summary>
    /// クリックで座標を取得するコンポーネント
    /// </summary>
    [SerializeField] private ClickRaycaster _clickRaycaster;

    // 成るか確認するUI
    /// <summary>
    /// 成るか確認UIのパネルオブジェクト
    /// </summary>
    [SerializeField] private GameObject _promotionConfirmUI;
    /// <summary>
    /// 成るボタン
    /// </summary>
    [SerializeField] private Button _promotionYesButton;
    /// <summary>
    /// 成らないボタン
    /// </summary>
    [SerializeField] private Button _promotionNoButton;
    /// <summary>
    /// 成るかの確認をクリックした際にtrueにする(確認したらリセットする)
    /// </summary>
    private bool _isPromotionConfirmClicked;
    /// <summary>
    /// 成るかの確認のどちらをクリックしたか
    /// </summary>
    private bool _isPromotionConfirmYesClicked;

    // 結果画面UI
    /// <summary>
    /// 結果表示用UIのパネルオブジェクト
    /// </summary>
    [SerializeField] private GameObject _resultUI;
    /// <summary>
    /// 結果表示用テキスト
    /// </summary>
    [SerializeField] private TextMeshProUGUI _resultText;
    /// <summary>
    /// タイトルに戻るボタン
    /// </summary>
    [SerializeField] private Button _backToTitleButton;
    /// <summary>
    /// もう一度遊ぶボタン
    /// </summary>
    [SerializeField] private Button _restartButton;

    /// <summary>
    /// 現在のターン数
    /// </summary>
    private int _turnCount = 0;    
    /// <summary>
    /// 現在のプレイヤーサイド
    /// </summary>
    private PlayerSide _currentPlayerSide = PlayerSide.Bottom;
    /// <summary>
    /// インゲームのシーケンス管理
    /// </summary>
    private IngameSequence _currentSequence = IngameSequence.WaitingPieceSelect;
    /// <summary>
    /// 現在選択中の駒
    /// </summary>
    private Piece _currentSelectedPiece = null;
    /// <summary>
    /// 現在選択中の駒の移動先候補
    /// </summary>
    private Vector2Int[] _currentSelectedPieceDestinationCandidates = null;
    /// <summary>
    /// 現在の移動先
    /// </summary>
    private Vector2Int _currentSelectedPieceDestination = new Vector2Int(-1, -1);
    /// <summary>
    /// 駒を移動中かどうか
    /// </summary>
    private bool _isPieceMoving = false;
    /// <summary>
    /// 駒を何秒で移動させるか
    /// </summary>
    private float _pieceMoveDuration = 0.5f;

    void Start()
    {
        Debug.Log("[GameManager] Start");

        // 成り確認UIの設定
        _promotionConfirmUI.SetActive(false);
        // ボタンに処理を登録
        _promotionYesButton.onClick.AddListener(() =>
        {
            _isPromotionConfirmClicked = true;
            _isPromotionConfirmYesClicked = true;
        });
        _promotionNoButton.onClick.AddListener(() =>
        {
            _isPromotionConfirmClicked = true;
            _isPromotionConfirmYesClicked = false;
        });

        // 結果画面UIの設定
        _resultUI.SetActive(false);
        _backToTitleButton.onClick.AddListener(() =>
        {
            // タイトルに戻る処理
            SceneManager.LoadScene("Title");
        });
        _restartButton.onClick.AddListener(() =>
        {
            // もう一度遊ぶ処理
            SceneManager.LoadScene("Ingame");
        });

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

    /// <summary>
    /// 将棋盤のマスを生成する
    /// </summary>
    /// <returns>盤のマスのTransform配列</returns>
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

    /// <summary>
    /// マスのマテリアルを変更する
    /// </summary>
    /// <param name="isHighlight">ハイライトするかどうか</param>
    /// <param name="logicPos">ロジック座標</param>
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

    /// <summary>
    /// ワールド座標値に最も近いロジック座標に変換する
    /// </summary>
    /// <param name="worldPosition">ワールド座標</param>
    /// <returns>最も近いロジック座標</returns>
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

    /// <summary>
    /// 駒を初期配置データに基づいて生成する
    /// </summary>
    /// <param name="initialPlacement">初期配置データのテキストアセット</param>
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

    /// <summary>
    /// 移動先候補を取得
    /// </summary>
    /// <param name="playerPiece">移動させる駒</param>
    /// <param name="playerSide">駒のプレイヤーサイド</param>
    /// <returns>移動先候補のロジック座標配列</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private Vector2Int[] GetMoveDestinationCandidates(Piece playerPiece, PlayerSide playerSide)
    {
        Debug.Log($"Getting move destinations for piece: {playerPiece._pieceType} at {WorldToLogicPosition(playerPiece.transform.position)}");

        // 移動先候補リスト
        List<Vector2Int> destinationCandidates = new List<Vector2Int>();

        // 持ち駒の場合は処理を分ける
        if (!playerPiece._isMainStagePiece)
        {
            // 基本的に全空きマスに移動可能
            // TODO: 打ち歩詰めは未対応
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                for (int y = 0; y < BOARD_SIZE; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);

                    // 歩, 香車は1列目, 桂馬は2列目には打てない
                    bool isInvalidDropPosition = (
                        (playerPiece._pieceType == PieceType.FU || playerPiece._pieceType == PieceType.KYOSHA) && 
                        ((playerSide == PlayerSide.Bottom && y == BOARD_SIZE - 1) || (playerSide == PlayerSide.Top && y == 0))
                    ) || (
                        playerPiece._pieceType == PieceType.KEIMA &&
                        ((playerSide == PlayerSide.Bottom && y >= BOARD_SIZE - 2) || (playerSide == PlayerSide.Top && y <= 1))
                    );

                    // そのマスに駒が存在しない場合のみ追加
                    if(!isInvalidDropPosition && !IsCellOccupied(pos, out var occupyingPiece))
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
                    // 同じプレイヤーサイドのと金でない歩のみ対象
                    if(fu._playerSide == playerSide && fu._isMainStagePiece && !fu._isPromoted)
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
        else
        {
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
        }

        Debug.Log($"候補座標: {string.Join(", ", destinationCandidates)}");

        return destinationCandidates.ToArray();
    }
    /// <summary>
    /// 指定したロジック座標に駒が存在するかどうかを判定する
    /// </summary>
    /// <param name="logicPos">ロジック座標</param>
    /// <param name="occupyingPiece">駒が存在する場合、その駒の参照</param>
    /// <returns>駒が存在する場合はtrue、存在しない場合はfalse</returns>
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

    /// <summary>
    /// 駒選択待ちの処理
    /// </summary>
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

    /// <summary>
    /// 駒の移動先選択待ちの処理
    /// </summary>
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

    /// <summary>
    /// 駒を移動中の処理
    /// </summary>
    private void HandlePieceMoving()
    {
        // 移動中なら何もしない
        if (_isPieceMoving) return;

        // 駒を移動中フラグを立てる        
        _isPieceMoving = true;

        // コルーチンで駒を移動
        StartCoroutine(MovePieceToDestination(_currentSelectedPiece, _currentSelectedPieceDestination));
    }

    /// <summary>
    /// 駒を目的地まで移動させるコルーチン
    /// </summary>
    /// <param name="piece">移動させる駒</param>
    /// <param name="destination">目的地のロジック座標</param>
    /// <returns></returns>
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

            // 王 or 玉なら試合終了
            if(occupyingPiece._pieceType == PieceType.OU || occupyingPiece._pieceType == PieceType.GYOKU)
            {
                _resultText.text = $"{piece._playerSide} win!";
                _resultUI.SetActive(true);
            }

            // 駒の状態を変更
            occupyingPiece._playerSide = piece._playerSide;
            occupyingPiece._isPromoted = false;
            occupyingPiece._isMainStagePiece = false;

            // 駒台に追加
            if(piece._playerSide == PlayerSide.Bottom)
            {
                _pieceStageBottomPieces.Add(occupyingPiece.gameObject);
            }
            else
            {
                _pieceStageTopPieces.Add(occupyingPiece.gameObject);
            }
            // 駒台の駒を配置し直す
            RearrangePieceStage(occupyingPiece._playerSide);
            // 駒の向きを変更
            occupyingPiece.transform.rotation = (piece._playerSide == PlayerSide.Bottom) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        }

        Vector2Int previousLogicPos = WorldToLogicPosition(startingPosition);
        Vector2Int pieceLogicPos = WorldToLogicPosition(piece.transform.position);
        bool isInPromotionZone = (piece._playerSide == PlayerSide.Bottom && pieceLogicPos.y >= 6) ||
                                 (piece._playerSide == PlayerSide.Top && pieceLogicPos.y <= 2) ||
                                 (piece._playerSide == PlayerSide.Bottom && previousLogicPos.y >= 6) ||
                                 (piece._playerSide == PlayerSide.Top && previousLogicPos.y <= 2);
        bool isPromotablePiece = piece._pieceType == PieceType.FU ||
                                 piece._pieceType == PieceType.KYOSHA ||
                                 piece._pieceType == PieceType.KEIMA ||
                                 piece._pieceType == PieceType.GIN ||
                                 piece._pieceType == PieceType.KAKU ||
                                 piece._pieceType == PieceType.HISHA;
        // 移動の前後どちらかで相手陣地に入っていて、まだ成っていなくて、今ターンに盤上に出た駒でなければ成る
        if(isInPromotionZone && !piece._isPromoted && piece._isMainStagePiece && isPromotablePiece)
        {
            // 成れる状態

            // 1列目の歩, 香車, 2列目の桂馬は強制的に成る
            bool isForcedPromotion = ((piece._pieceType == PieceType.FU || piece._pieceType == PieceType.KYOSHA) &&
                                     ((piece._playerSide == PlayerSide.Bottom && pieceLogicPos.y == 8) ||
                                      (piece._playerSide == PlayerSide.Top && pieceLogicPos.y == 0)))
                                     ||
                                     (piece._pieceType == PieceType.KEIMA &&
                                     ((piece._playerSide == PlayerSide.Bottom && pieceLogicPos.y >= 7) ||
                                      (piece._playerSide == PlayerSide.Top && pieceLogicPos.y <= 1)));

            if (isForcedPromotion)
            {
                // 強制的に成る
                piece._isPromoted = true;
                // 裏返す
                piece.transform.Rotate(0, 0, 180);
                Debug.Log($"Piece forced promoted: {piece._pieceType} at {pieceLogicPos}");
            }
            else
            {
                // 成るか確認UIを表示
                _promotionConfirmUI.SetActive(true);
                // プレイヤーの入力待ち
                yield return new WaitUntil(() => _isPromotionConfirmClicked);
                // 成るが選択された場合
                if (_isPromotionConfirmYesClicked)
                {
                    // 成る処理を行う
                    piece._isPromoted = true;
                    // 裏返す
                    piece.transform.Rotate(0, 0, 180);
                    Debug.Log($"Piece promoted: {piece._pieceType} at {pieceLogicPos}");
                }
                // 成るか確認UIを非表示にしてフラグをリセット
                _isPromotionConfirmClicked = false;
                _isPromotionConfirmYesClicked = false;
                _promotionConfirmUI.SetActive(false);
            }
        }

        // 駒が駒台から盤上に出た場合の処理
        if(!piece._isMainStagePiece)
        {
            if(piece._playerSide == PlayerSide.Bottom)
            {
                _pieceStageBottomPieces.Remove(piece.gameObject);
                RearrangePieceStage(PlayerSide.Bottom);
            }
            else
            {
                _pieceStageTopPieces.Remove(piece.gameObject);
                RearrangePieceStage(PlayerSide.Top);
            }
        }

        // 駒の移動完了後の処理
        piece._isMainStagePiece = true;
        _isPieceMoving = false;
        _currentSelectedPiece = null;
        _currentSelectedPieceDestinationCandidates = null;
        _currentSelectedPieceDestination = new Vector2Int(-1, -1);

        _currentSequence = IngameSequence.TurnEnded;
    }

    /// <summary>
    /// 駒台の配置を直す
    /// </summary>
    /// <param name="playerSide">駒台のプレイヤーサイド</param>
    private void RearrangePieceStage(PlayerSide playerSide)
    {
        List<GameObject> pieceStagePieces = (playerSide == PlayerSide.Bottom) ? _pieceStageBottomPieces : _pieceStageTopPieces;
        Vector3 pieceStagePosition = (playerSide == PlayerSide.Bottom) ? _pieceStageBottomPosition : _pieceStageTopPosition;

        for(int i = 0; i < pieceStagePieces.Count; i++)
        {
            GameObject p = pieceStagePieces[i];
            // 駒の配置場所
            var piecePlacePosX = pieceStagePosition.x + _pieceStagePlaceStartOffsetX + _pieceStagePlaceSpanX * i;
            Vector3 offBoardPosition = new Vector3(
                piecePlacePosX,
                p.transform.position.y,
                pieceStagePosition.z
            );
            p.transform.position = offBoardPosition;
        }
    }

    /// <summary>
    /// ターン終了処理
    /// </summary>
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

    /// <summary>
    /// インゲームのシーケンス列挙型
    /// </summary>
    private enum IngameSequence
    {
        /// <summary>
        /// 駒選択待ち
        /// </summary>
        WaitingPieceSelect,
        /// <summary>
        /// 駒の移動先選択待ち
        /// </summary>
        PieceDestinationSelect,
        /// <summary>
        /// 駒を移動中
        /// </summary>
        PieceMoving,
        /// <summary>
        /// ターン終了処理
        /// </summary>
        TurnEnded,
    }
}

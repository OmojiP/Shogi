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
    /// <summary>
    /// 将棋盤上の駒情報配列(空きマスはnull)
    /// </summary>
    private Piece[,] _pieces;
    
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
    private Piece[] _fuPieces;

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
    [SerializeField] private Piece[] _piecePrefabs;
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
    private PlayerSide _currentPlayerSide = PlayerSide.BOTTOM;
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
        // UIを非表示にする
        _promotionConfirmUI.SetActive(false);
        // ボタンに処理を登録
        _promotionYesButton.onClick.AddListener(() =>
        {
            // 成るボタンがクリックされたら、クリックフラグとYesフラグを立てる
            _isPromotionConfirmClicked = true;
            _isPromotionConfirmYesClicked = true;
        });
        _promotionNoButton.onClick.AddListener(() =>
        {
            // 成らないボタンがクリックされたら、クリックフラグとNoフラグを立てる
            _isPromotionConfirmClicked = true;
            _isPromotionConfirmYesClicked = false;
        });

        // 結果画面UIの設定
        // UIを非表示にする
        _resultUI.SetActive(false);
        _backToTitleButton.onClick.AddListener(() =>
        {
            // タイトルに戻るボタンがクリックされたら、タイトルシーンに遷移する
            SceneManager.LoadScene("Title");
        });
        _restartButton.onClick.AddListener(() =>
        {
            // もう一度ボタンがクリックされたら、インゲームシーンに遷移する
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
        // シーケンスごとにフレーム処理を分岐する
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
        // 配列の初期化
        var stageCells = new Transform[BOARD_SIZE, BOARD_SIZE];
        _cellWorldPositions = new Vector3[BOARD_SIZE, BOARD_SIZE];

        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                // マスのワールド座標を計算して生成
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

        // すべてのセルのワールド座標と worldPosition を比較
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                // ワールド座標の距離の二乗を計算し、最も近いロジック座標を更新
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
        // 将棋盤配列の初期化
        _pieces = new Piece[BOARD_SIZE, BOARD_SIZE];

        // 歩オブジェクトリストの初期化
        List<Piece> fuPieces = new List<Piece>();

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

            // 駒の生成(_piecePrefabs配列から駒名に対応するプレハブを取得)
            Piece piecePrefab = null;
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
                // 駒オブジェクトの位置と回転を指定して生成
                Vector3 spawnPosition = _cellWorldPositions[x, y] + Vector3.up * 0.01f; // 少し浮かせて配置
                Quaternion spawnRotation = (side == "Upper") ? Quaternion.identity : Quaternion.Euler(0, 180, 0); // 下側の駒は180度回転
                var piece = Instantiate(piecePrefab, spawnPosition, spawnRotation, _pieceParentTransform);
                // 駒のプレイヤーサイドを設定
                piece.PlayerSide = (side == "Upper") ? PlayerSide.TOP : PlayerSide.BOTTOM;
                // 駒のロジック座標を設定
                piece.LogicPos = new Vector2Int(x, y);

                // 盤上の駒情報配列に登録
                _pieces[x, y] = piece;

                // 歩のオブジェクトなら歩オブジェクトリストに追加
                if(piece.PieceType == PieceType.FU)
                {
                    fuPieces.Add(piece);
                }
            }
        }
        // 配列に変換
        _fuPieces = fuPieces.ToArray();
    }

    /// <summary>
    /// 駒選択待ちの処理
    /// </summary>
    private void HandlePieceSelect()
    {
        // 自分の駒がクリックされた→移動先のクリック待ちへ遷移
        if (_clickRaycaster.TryGetClickedPosition(out Vector3 clickedPosition, out var clickedPiece))
        {
            // 自分の駒がクリックされた場合, 処理する
            if(clickedPiece != null && clickedPiece.PlayerSide == _currentPlayerSide)
            {
                // 駒が選択された状態へ遷移
                _currentSequence = IngameSequence.PieceDestinationSelect;
                _currentSelectedPiece = clickedPiece;
                Debug.Log($"Selected piece: {clickedPiece.PieceType} at {WorldToLogicPosition(clickedPosition)}");
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
            // 選択中の駒の移動先候補を取得
            _currentSelectedPieceDestinationCandidates = LogicFunction.GetMoveDestinationCandidates(_currentSelectedPiece, _pieces, _fuPieces);
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
                Debug.Log($"Move destinations for {_currentSelectedPiece.PieceType}: {string.Join(", ", _currentSelectedPieceDestinationCandidates)}");
                // 移動先候補のマスをハイライト表示
                foreach(var pos in _currentSelectedPieceDestinationCandidates)
                {
                    ChangeCellMaterial(true, pos);
                }
            }
        }

        // プレイヤーの入力を受け取り、クリックされた座標と駒を取得する
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

            Debug.Log($"Moving piece: {_currentSelectedPiece.PieceType} to {_currentSelectedPieceDestination}");

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
        Piece occupyingPiece = _pieces[destination.x, destination.y];
        bool isEnemyPiecePresent = occupyingPiece != null && occupyingPiece.PlayerSide != piece.PlayerSide;

        // 移動アニメーション(startingPositionからtargetPositionへ線形補間で_pieceMoveDuration秒かけて移動)
        float elapsedTime = 0f;
        Vector3 startingPosition = piece.transform.position;
        while (elapsedTime < _pieceMoveDuration)
        {
            piece.transform.position = Vector3.Lerp(startingPosition, targetPosition, elapsedTime / _pieceMoveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // 最終的に正確な位置に設定
        piece.transform.position = targetPosition;

        // 移動先に相手の駒があれば、その駒を自分のサイドに書き換えて駒台に移動
        if(isEnemyPiecePresent)
        {
            Debug.Log($"Captured piece: {occupyingPiece.PieceType} at {destination}");

            // 王 or 玉なら試合終了
            if(occupyingPiece.PieceType == PieceType.OU || occupyingPiece.PieceType == PieceType.GYOKU)
            {
                // 勝利UIを表示
                _resultText.text = $"{piece.PlayerSide} win!";
                _resultUI.SetActive(true);
            }

            // 駒の状態を変更
            occupyingPiece.PlayerSide = piece.PlayerSide;
            occupyingPiece.IsPromoted = false;
            occupyingPiece.IsMainStagePiece = false;
            // ロジック座標を初期化
            occupyingPiece.LogicPos = new Vector2Int(-1, -1);

            // 自分のサイドの駒台に追加
            if(piece.PlayerSide == PlayerSide.BOTTOM)
            {
                _pieceStageBottomPieces.Add(occupyingPiece.gameObject);
            }
            else
            {
                _pieceStageTopPieces.Add(occupyingPiece.gameObject);
            }
            // 駒台の駒を配置し直す
            RearrangePieceStage(occupyingPiece.PlayerSide);
            // 駒の向きを変更
            occupyingPiece.transform.rotation = (piece.PlayerSide == PlayerSide.BOTTOM) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

            // 将棋盤配列から駒を削除
            _pieces[destination.x, destination.y] = null;
        }

        // 駒の成り判定
        Vector2Int previousLogicPos = WorldToLogicPosition(startingPosition); // 移動前のロジック座標
        Vector2Int pieceLogicPos = WorldToLogicPosition(piece.transform.position); // 移動後のロジック座標
        // 相手陣地に入っているかどうか
        bool isInPromotionZone = (piece.PlayerSide == PlayerSide.BOTTOM && pieceLogicPos.y >= 6) ||
                                 (piece.PlayerSide == PlayerSide.TOP && pieceLogicPos.y <= 2) ||
                                 (piece.PlayerSide == PlayerSide.BOTTOM && previousLogicPos.y >= 6) ||
                                 (piece.PlayerSide == PlayerSide.TOP && previousLogicPos.y <= 2);
        // 成れる駒かどうか
        bool isPromotablePiece = piece.PieceType == PieceType.FU ||
                                 piece.PieceType == PieceType.KYOSHA ||
                                 piece.PieceType == PieceType.KEIMA ||
                                 piece.PieceType == PieceType.GIN ||
                                 piece.PieceType == PieceType.KAKU ||
                                 piece.PieceType == PieceType.HISHA;
        // 移動の前後どちらかで相手陣地に入っていて、まだ成っていなくて、今ターンに盤上に出た駒でなければ成る
        if(isInPromotionZone && !piece.IsPromoted && piece.IsMainStagePiece && isPromotablePiece)
        {
            // 成れる状態

            // 1列目の歩, 香車, 2列目の桂馬は強制的に成る
            bool isForcedPromotion = ((piece.PieceType == PieceType.FU || piece.PieceType == PieceType.KYOSHA) &&
                                     ((piece.PlayerSide == PlayerSide.BOTTOM && pieceLogicPos.y == 8) ||
                                      (piece.PlayerSide == PlayerSide.TOP && pieceLogicPos.y == 0)))
                                     ||
                                     (piece.PieceType == PieceType.KEIMA &&
                                     ((piece.PlayerSide == PlayerSide.BOTTOM && pieceLogicPos.y >= 7) ||
                                      (piece.PlayerSide == PlayerSide.TOP && pieceLogicPos.y <= 1)));
            if (isForcedPromotion)
            {
                // 強制的に成る
                piece.IsPromoted = true;
                // 裏返す
                piece.transform.Rotate(0, 0, 180);
                Debug.Log($"Piece forced promoted: {piece.PieceType} at {pieceLogicPos}");
            }
            else
            {
                // 任意で成る場合
                // 成るか確認UIを表示
                _promotionConfirmUI.SetActive(true);
                // プレイヤーの入力待ち
                yield return new WaitUntil(() => _isPromotionConfirmClicked);
                // 成るが選択された場合
                if (_isPromotionConfirmYesClicked)
                {
                    // 成る処理を行う
                    piece.IsPromoted = true;
                    // 裏返す
                    piece.transform.Rotate(0, 0, 180);
                    Debug.Log($"Piece promoted: {piece.PieceType} at {pieceLogicPos}");
                }
                // 成るか確認UIを非表示にしてフラグをリセット
                _isPromotionConfirmClicked = false;
                _isPromotionConfirmYesClicked = false;
                _promotionConfirmUI.SetActive(false);
            }
        }

        // 駒が駒台から盤上に出た場合の処理
        if(!piece.IsMainStagePiece)
        {
            // 駒台から削除して配置を直す
            if(piece.PlayerSide == PlayerSide.BOTTOM)
            {
                _pieceStageBottomPieces.Remove(piece.gameObject);
                RearrangePieceStage(PlayerSide.BOTTOM);
            }
            else
            {
                _pieceStageTopPieces.Remove(piece.gameObject);
                RearrangePieceStage(PlayerSide.TOP);
            }
        }

        // 駒の移動完了後の処理
        piece.IsMainStagePiece = true;
        piece.LogicPos = destination;
        _isPieceMoving = false;
        _currentSelectedPiece = null;
        _currentSelectedPieceDestinationCandidates = null;
        _currentSelectedPieceDestination = new Vector2Int(-1, -1);

        // 将棋盤配列の更新
        _pieces[previousLogicPos.x, previousLogicPos.y] = null;
        _pieces[destination.x, destination.y] = piece;

        _currentSequence = IngameSequence.TurnEnded;
    }

    /// <summary>
    /// 駒台の配置を直す
    /// </summary>
    /// <param name="playerSide">駒台のプレイヤーサイド</param>
    private void RearrangePieceStage(PlayerSide playerSide)
    {
        // 駒を pieceStagePosition.x + _pieceStagePlaceStartOffsetX から _pieceStagePlaceSpanX 間隔で配置する
        List<GameObject> pieceStagePieces = (playerSide == PlayerSide.BOTTOM) ? _pieceStageBottomPieces : _pieceStageTopPieces;
        Vector3 pieceStagePosition = (playerSide == PlayerSide.BOTTOM) ? _pieceStageBottomPosition : _pieceStageTopPosition;

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
        _currentPlayerSide = (_currentPlayerSide == PlayerSide.BOTTOM) ? PlayerSide.TOP : PlayerSide.BOTTOM;
        
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

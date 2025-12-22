using R3;
using System.Collections;
using UnityEngine;

/// <summary>
/// プレイヤーを表すコンポーネント.
/// 自ターンの処理を担当
/// </summary>
public class Player : MonoBehaviour
{
    /// <summary>
    /// 自分のプレイヤーサイド
    /// </summary>
    [SerializeField] private PlayerSide _myPlayerSide;

    // 他オブジェクトの参照
    /// <summary>
    /// メインステージ
    /// </summary>
    [SerializeField] private MainStage _mainStage;
    /// <summary>
    /// 自分の駒台
    /// </summary>
    [SerializeField] private PieceStage _myPieceStage;
    /// <summary>
    /// 成るかの確認を問いかける橋渡しオブジェクト
    /// </summary>
    [SerializeField] private PromotionDecision _promotionDecision;

    /// <summary>
    /// Playerの状態管理を行うReactiveProperty
    /// </summary>
    private ReactiveProperty<PlayerTurnPhaseType> _currentPlayerTurnPhase = new(PlayerTurnPhaseType.IDLE); // 初期状態は待機状態
    /// <summary>
    /// Playerの状態管理を行うReadOnlyReactiveProperty
    /// </summary>
    public ReadOnlyReactiveProperty<PlayerTurnPhaseType> CurrentPlayerTurnPhase => _currentPlayerTurnPhase.ToReadOnlyReactiveProperty();
    /// <summary>
    /// 王の駒を取った際に発行するイベント
    /// </summary>
    private Subject<Unit> _onGottenKing = new();
    /// <summary>
    /// 王の駒を取った際に発行するイベント
    /// </summary>
    public Observable<Unit> OnGottenKing => _onGottenKing;

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
    /// 駒がクリックされたときの処理
    /// </summary>
    /// <param name="clickedPiece"></param>
    public void OnPieceClicked(Piece clickedPiece)
    {
        // 駒がクリックされた場合の処理
        Debug.Log($"Piece clicked: {clickedPiece.PieceType} at {clickedPiece.LogicPos}");
        
        switch (_currentPlayerTurnPhase.Value)
        {
            case PlayerTurnPhaseType.WAITING_PIECE_SELECT:
                // 駒選択待ちなら実行
                HandlePieceSelect(clickedPiece);
                break;
            case PlayerTurnPhaseType.PIECE_DESTINATION_SELECT:
                // 移動先選択待ちなら実行
                HandlePieceDestinationSelect(clickedPiece.LogicPos);
                break;
            default:
                // その他の状態なら無視
                break;

        }
    }

    /// <summary>
    /// マスがクリックされたときの処理
    /// </summary>
    /// <param name="clickedCell"></param>
    public void OnCellClicked(StageCell clickedCell)
    {
        // マスがクリックされた場合の処理
        Vector2Int logicPos = clickedCell.LogicPos;
        Debug.Log($"Cell clicked at logic position: {logicPos}");

        switch (_currentPlayerTurnPhase.Value)
        {
            case PlayerTurnPhaseType.PIECE_DESTINATION_SELECT:
                // 移動先選択待ちなら実行
                HandlePieceDestinationSelect(logicPos);
                break;
            default:
                // その他の状態なら無視
                break;
        }
    }

    /// <summary>
    /// 自分のターンを開始する処理
    /// </summary>
    /// <param name="mySide"></param>
    public void StartPlayerTurn()
    {
        Debug.Log($"{_myPlayerSide} Turn Started");
        EnterPieceSelect();
    }

    /// <summary>
    /// 駒選択待ちへ遷移する処理
    /// </summary>
    private void EnterPieceSelect()
    {
        // 駒選択待ちへ遷移
        _currentPlayerTurnPhase.Value = PlayerTurnPhaseType.WAITING_PIECE_SELECT;
        
        // 選択中の駒情報をリセット
        _currentSelectedPiece = null;
        // 選択中の駒の移動先候補をリセット
        _currentSelectedPieceDestinationCandidates = null;
        // 選択中の駒の移動先をリセット
        _currentSelectedPieceDestination = new Vector2Int(-1, -1);
    }

    /// <summary>
    /// 駒選択待ちの処理
    /// </summary>
    private void HandlePieceSelect(Piece selectedPiece)
    {
        // 自分の駒がクリックされた→移動先のクリック待ちへ遷移

        // 自分の駒がクリックされた場合, 処理する
        if(selectedPiece.PlayerSide == _myPlayerSide)
        {
            // 駒が選択された状態へ遷移
            _currentSelectedPiece = selectedPiece;
            Debug.Log($"Selected piece: {selectedPiece.PieceType} at {selectedPiece.LogicPos}");
            EnterPieceDestinationSelect();
        }
    }

    /// <summary>
    /// 駒の移動先選択待ちへ遷移する処理
    /// </summary>
    private void EnterPieceDestinationSelect()
    {
        // 駒の移動先選択待ちへ遷移
        _currentPlayerTurnPhase.Value = PlayerTurnPhaseType.PIECE_DESTINATION_SELECT;

        // 選択中の駒の移動先候補を取得
        _currentSelectedPieceDestinationCandidates = LogicFunction.GetMoveDestinationCandidates(_currentSelectedPiece, _mainStage);
        // 候補先がない場合、駒選択待ちへ遷移
        if(_currentSelectedPieceDestinationCandidates.Length == 0)
        {
            Debug.Log("移動先の候補がありません。駒選択待ちに戻ります。");
            EnterPieceSelect();
            return;
        }
        else
        {
            Debug.Log($"Move destinations for {_currentSelectedPiece.PieceType}: {string.Join(", ", _currentSelectedPieceDestinationCandidates)}");
            // 移動先候補のマスをハイライト表示
            foreach(var pos in _currentSelectedPieceDestinationCandidates)
            {
                _mainStage.ChangeCellColor(true, pos);
            }
        }
    }

    /// <summary>
    /// 駒の移動先選択待ちの処理
    /// </summary>
    private void HandlePieceDestinationSelect(Vector2Int selectedLogicPos)
    {
        // 移動先候補のマスのハイライトを元に戻す
        foreach(var pos in _currentSelectedPieceDestinationCandidates)
        {
            _mainStage.ChangeCellColor(false, pos);
        }

        // 選択された駒の移動先候補がクリックされたかどうかを判定
        bool isClickedOnDestination = false;
        // 移動先候補の中にselectedLogicPosが含まれているかどうかを確認
        isClickedOnDestination = System.Array.Exists(_currentSelectedPieceDestinationCandidates, pos => pos == selectedLogicPos);

        // 選択された駒の移動先候補がクリックされた→駒の移動フェーズへ遷移
        if (isClickedOnDestination)
        {
            // 駒の移動先を設定
            _currentSelectedPieceDestination = selectedLogicPos;

            // 駒の移動フェーズへ遷移
            Debug.Log($"Moving piece: {_currentSelectedPiece.PieceType} to {_currentSelectedPieceDestination}");
            EnterPieceMoving();
        }
        // 選択された駒の移動先以外がクリックされた→駒選択待ちへ遷移
        else
        {
            // 駒選択待ちへ遷移
            Debug.Log("移動できないマスがクリックされました。駒選択待ちに戻ります。");
            EnterPieceSelect();
        }
    }

    /// <summary>
    /// 駒の移動フェーズへ遷移する処理
    /// </summary>
    private void EnterPieceMoving()
    {
        // 駒の移動フェーズへ遷移
        _currentPlayerTurnPhase.Value = PlayerTurnPhaseType.PIECE_MOVING;

        // 駒の移動処理を開始
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
        // 移動先に相手の駒があるか確認
        Piece occupyingPiece = _mainStage.GetPieceAt(destination);
        bool isEnemyPiecePresent = occupyingPiece != null && occupyingPiece.PlayerSide != piece.PlayerSide;

        // 移動先に相手の駒があれば、その駒を自分のサイドに書き換えて駒台に移動
        if(isEnemyPiecePresent)
        {
            Debug.Log($"Captured piece: {occupyingPiece.PieceType} at {destination}");

            // 王 or 玉なら試合終了
            if(occupyingPiece.PieceType == PieceType.OU || occupyingPiece.PieceType == PieceType.GYOKU)
            {
                // 王を取ったことを通知
                _onGottenKing.OnNext(Unit.Default);

                // 行動を終了して待機状態に戻る
                _currentPlayerTurnPhase.Value = PlayerTurnPhaseType.IDLE;
                yield break; // コルーチンを終了
            }

            // 自分の駒台に移動
            _mainStage.PickupPiece(destination); // 盤上から駒を取り除く
            yield return _myPieceStage.AddPiece(occupyingPiece); // 駒台に駒を移動
        }

        // 移動前の座標を持っておく
        Vector2Int previousPos = piece.LogicPos;
        // 駒が移動前に将棋盤上にあったか
        bool isFromMainStage;

        // 駒を移動
        if (piece.IsMainStagePiece)
        {
            // 盤上の駒の場合、一旦盤上から取り除く        
            _mainStage.PickupPiece(piece.LogicPos);
            isFromMainStage = true;
        }
        else
        {
            // 駒台の駒の場合、駒台から取り除く
            _myPieceStage.RemovePiece(piece);
            isFromMainStage = false;
        }
        // 取り出した駒を目的地まで移動させる
        yield return _mainStage.PlacePiece(piece, destination);
        // 駒が成れるか確認し、成れる場合は成らせる
        var promoteResult = LogicFunction.CheckPiecePromotable(piece,isFromMainStage, previousPos, destination);
        switch (promoteResult)
        {
            case LogicFunction.PiecePromoteJudgementType.FORCE_PROMOTE:
                piece.ChangePromotionState(true);
                break;
            case LogicFunction.PiecePromoteJudgementType.SELECTABLE_PROMOTE:
                // 成るかどうか確認し、結果を受け取って処理する
                yield return _promotionDecision.DecidePromotionAsync(isYesSelected =>
                {
                    // 成るが選択された場合
                    if (isYesSelected)
                    {
                        // 成る処理を行う
                        piece.ChangePromotionState(true);
                        Debug.Log($"Piece promoted: {piece.PieceType} at {destination}");
                    }
                });
                break;
            case LogicFunction.PiecePromoteJudgementType.CANT_PROMOTE:
                // 何もしない
                break;
            default:
                // 何もしない
                break;
        }

        // 駒移動終了時の処理
        OnEndedMovePiece();
    }

    /// <summary>
    /// 駒移動が完了したときの処理
    /// </summary>
    private void OnEndedMovePiece()
    {   
        Debug.Log($"{_myPlayerSide} End Move Piece");

        // 各種変数のリセット
        // 選択中の駒情報をリセット
        _currentSelectedPiece = null;
        // 選択中の駒の移動先候補をリセット
        _currentSelectedPieceDestinationCandidates = null;
        // 選択中の駒の移動先をリセット
        _currentSelectedPieceDestination = new Vector2Int(-1, -1);

        // 待機状態へ遷移
        _currentPlayerTurnPhase.Value = PlayerTurnPhaseType.IDLE;
    }
}

/// <summary>
/// プレイヤーの自ターンフェーズ列挙型
/// </summary>
public enum PlayerTurnPhaseType
{
    /// <summary>
    /// 行動の許可待ち
    /// </summary>
    IDLE = 0,
    /// <summary>
    /// 駒選択待ち
    /// </summary>
    WAITING_PIECE_SELECT = 1,
    /// <summary>
    /// 駒の移動先選択待ち
    /// </summary>
    PIECE_DESTINATION_SELECT = 2,
    /// <summary>
    /// 駒移動フェーズ
    /// </summary>
    PIECE_MOVING = 3,
}

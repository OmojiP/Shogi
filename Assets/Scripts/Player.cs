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

    // 他オブジェくトの参照
    /// <summary>
    /// ゲームの進行を管理するマネージャー
    /// </summary>
    [SerializeField] private GameManager _gameManager;
    /// <summary>
    /// メインステージ
    /// </summary>
    [SerializeField] private MainStage _mainStage;
    /// <summary>
    /// 自分の駒台
    /// </summary>
    [SerializeField] private PieceStage _myPieceStage;
    
    /// <summary>
    /// インゲームのシーケンス管理
    /// </summary>
    private PlayerTurnPhaseType _currentPhase = PlayerTurnPhaseType.WaitingGameStart; // 初期状態はゲーム開始待ち
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

        switch (_currentPhase)
        {
            case PlayerTurnPhaseType.WaitingPieceSelect:
                HandlePieceSelect(clickedPiece);
                break;
            case PlayerTurnPhaseType.PieceDestinationSelect:
                HandlePieceDestinationSelect(clickedPiece.LogicPos);
                break;
            case PlayerTurnPhaseType.PieceMoving:
                // 駒の移動フェーズ中は無視
                break;
            case PlayerTurnPhaseType.TurnEnded:
                // ターン終了処理中は無視
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

        switch (_currentPhase)
        {
            case PlayerTurnPhaseType.WaitingPieceSelect:
                // 駒選択待ち中は無視
                break;
            case PlayerTurnPhaseType.PieceDestinationSelect:
                HandlePieceDestinationSelect(logicPos);
                break;
            case PlayerTurnPhaseType.PieceMoving:
                // 駒の移動フェーズ中は無視
                break;
            case PlayerTurnPhaseType.TurnEnded:
                // ターン終了処理中には無視
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
        _currentPhase = PlayerTurnPhaseType.WaitingPieceSelect;
        
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
        _currentPhase = PlayerTurnPhaseType.PieceDestinationSelect;

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
        _currentPhase = PlayerTurnPhaseType.PieceMoving;

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
                // ゲーム終了処理を実行
                _gameManager.OnGameEnded(piece.PlayerSide);
                yield break; // コルーチンを終了
            }

            // 自分の駒台に移動
            _mainStage.PickupPiece(destination); // 盤上から駒を取り除く
            yield return _myPieceStage.AddPiece(occupyingPiece); // 駒台に駒を移動
        }

        // 駒を移動
        if (piece.IsMainStagePiece)
        {
            // 盤上の駒の場合、一旦盤上から取り除く        
            _mainStage.PickupPiece(piece.LogicPos);
        }
        else
        {
            // 駒台の駒の場合、駒台から取り除く
            _myPieceStage.RemovePiece(piece);
        }
        // 取り出した駒を目的地まで移動させる
        yield return _mainStage.PlacePiece(piece, destination);

        // 移動したらターン終了フェーズへ遷移
        EnterTurnEnded();
    }

    /// <summary>
    /// ターン終了フェーズに入ったときの処理
    /// </summary>
    private void EnterTurnEnded()
    {
        // ターン終了を発行してシーケンスに通知
        
        Debug.Log($"{_myPlayerSide} Turn Ended");

        _currentPhase = PlayerTurnPhaseType.TurnEnded;

        // 各種変数のリセット
        // 選択中の駒情報をリセット
        _currentSelectedPiece = null;
        // 選択中の駒の移動先候補をリセット
        _currentSelectedPieceDestinationCandidates = null;
        // 選択中の駒の移動先をリセット
        _currentSelectedPieceDestination = new Vector2Int(-1, -1);

        // 相手のターン待ちへ遷移
        _currentPhase = PlayerTurnPhaseType.WaitingForOpponentTurn;

        // ゲームマネージャーにターン終了を通知
        _gameManager.OnPlayerTurnEnded(_myPlayerSide);
    }

    /// <summary>
    /// プレイヤーの自ターンフェーズ列挙型
    /// </summary>
    private enum PlayerTurnPhaseType
    {
        /// <summary>
        /// ゲーム開始待ち
        /// </summary>
        WaitingGameStart = 0,
        /// <summary>
        /// 駒選択待ち
        /// </summary>
        WaitingPieceSelect = 1,
        /// <summary>
        /// 駒の移動先選択待ち
        /// </summary>
        PieceDestinationSelect = 2,
        /// <summary>
        /// 駒移動フェーズ
        /// </summary>
        PieceMoving = 3,
        /// <summary>
        /// ターン終了処理
        /// </summary>
        TurnEnded = 4,
        /// <summary>
        /// 相手のターン待ち
        /// </summary>
        WaitingForOpponentTurn = 5,
        /// <summary>
        /// ゲーム終了
        /// </summary>
        GameEnded = 6,
    }
}

using System.Collections;
using UnityEngine;

/// <summary>
/// 駒を表すコンポーネント.
/// 駒の状態管理や移動処理を担当
/// </summary>
public class Piece : MonoBehaviour
{
    /// <summary>
    /// 駒の種類
    /// </summary>
    [SerializeField] private PieceType _pieceType;
    /// <summary>
    /// 駒の種類
    /// </summary>
    public PieceType PieceType => _pieceType;
    /// <summary>
    /// 成っているかどうか
    /// </summary>
    [SerializeField] private bool _isPromoted = false;
    /// <summary>
    /// 成っているかどうか
    /// </summary>
    public bool IsPromoted => _isPromoted;
    /// <summary>
    /// ロジック座標
    /// </summary>
    private Vector2Int _logicPos = new Vector2Int(-1, -1);
    /// <summary>
    /// ロジック座標
    /// </summary>
    public Vector2Int LogicPos
    {
        get => _logicPos;
        set => _logicPos = value;
    }
    /// <summary>
    /// メインステージの駒かどうか(持ち駒かどうか)
    /// </summary>
    [SerializeField] private bool _isMainStagePiece = true;
    /// <summary>
    /// メインステージの駒かどうか(持ち駒かどうか)
    /// </summary>
    public bool IsMainStagePiece => _isMainStagePiece;
    /// <summary>
    /// プレイヤーサイド
    /// </summary>
    [SerializeField] private PlayerSide _playerSide;
    /// <summary>
    /// プレイヤーサイド
    /// </summary>
    public PlayerSide PlayerSide => _playerSide;

    /// <summary>
    /// 駒を何秒で移動させるか
    /// </summary>
    private float _pieceMoveDuration = 0.5f;

    /// <summary>
    /// UIマネージャー
    /// </summary>
    private IngameUIManager _uiManager;
    /// <summary>
    /// UIマネージャー
    /// </summary>
    public IngameUIManager UIManager
    {
        get { return _uiManager; }
        set { _uiManager = value; }
    }

    /// <summary>
    /// プレイヤーサイドを変更する関数
    /// </summary>
    /// <param name="newSide">新しいプレイヤーサイド</param>
    public void ChangePlayerSide(PlayerSide newSide)
    {
        _playerSide = newSide;
        // 駒の向きを変更
        UpdateRotation();
    }
    /// <summary>
    /// 成る状態を変更する関数(なれるかどうかの判定は行わない)
    /// </summary>
    /// <param name="isPromoted">変更後の成る状態</param>
    public void ChangePromotionState(bool isPromoted)
    {
        _isPromoted = isPromoted;
        // 駒の向きを変更
        UpdateRotation();
    }

    /// <summary>
    /// 駒の状態に合わせて回転を更新する関数
    /// </summary>
    private void UpdateRotation()
    {
        this.transform.rotation = 
            Quaternion.Euler(
                0, 
                PlayerSide == PlayerSide.TOP ? 0 : 180, 
                IsPromoted ? 180 : 0
                );
    }

    /// <summary>
    /// 駒を初期化する関数
    /// </summary>
    /// <param name="logicPos">駒のロジック上の座標</param>
    /// <param name="playerSide">プレイヤーサイド</param>
    /// <param name="isPromoted">成っているかどうか</param>
    /// <param name="uiManager">UIマネージャー</param>
    public void Initialize(Vector2Int logicPos, PlayerSide playerSide, bool isPromoted, IngameUIManager uiManager)
    {
        _logicPos = logicPos;
        _uiManager = uiManager;

        ChangePlayerSide(playerSide);
        ChangePromotionState(isPromoted);
    }

    /// <summary>
    /// 将棋盤に駒を移動する関数
    /// </summary>
    public IEnumerator MoveToMainStage(Vector2Int destination, StageCell[,] stageCells)
    {
        // 駒を将棋盤に移動
        var previousLogicPos = _logicPos;
        _logicPos = destination;

        // 駒のワールド座標を更新
        Vector3 targetPosition = new Vector3(
            stageCells[destination.x, destination.y].transform.position.x,
            this.transform.position.y,
            stageCells[destination.x, destination.y].transform.position.z
        );

        // 取る処理はPlayerクラスで行うため、ここでは駒の移動のみを行う

        // 移動アニメーション(startingPositionからtargetPositionへ線形補間で_pieceMoveDuration秒かけて移動)
        yield return MoveAnimation(targetPosition);

        // 成る処理
        yield return TryPromote(previousLogicPos, destination);

        // 駒の移動完了後の処理
        _isMainStagePiece = true;
    }


    /// <summary>
    /// 駒台に駒を移動する関数
    /// </summary>
    public IEnumerator MoveToPieceStage(PlayerSide playerSide, Vector3 pieceStagePosition)
    {
        // 駒を将棋盤に移動
        _logicPos = new Vector2Int(-1, -1);

        // 駒のワールド座標を更新
        Vector3 targetPosition = new Vector3(
            pieceStagePosition.x,
            this.transform.position.y,
            pieceStagePosition.z
        );

        // 移動アニメーション(startingPositionからtargetPositionへ線形補間で_pieceMoveDuration秒かけて移動)
        yield return MoveAnimation(targetPosition);

        // 駒の向きを変更
        ChangePlayerSide(playerSide);
        // 成り解除
        ChangePromotionState(false);

        // 駒の移動完了後の処理
        _isMainStagePiece = false;
    }

    private IEnumerator MoveAnimation(Vector3 targetPosition)
    {
        float elapsedTime = 0f;
        Vector3 startingPosition = this.transform.position;
        while (elapsedTime < _pieceMoveDuration)
        {
            this.transform.position = Vector3.Lerp(startingPosition, targetPosition, elapsedTime / _pieceMoveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        this.transform.position = targetPosition;
    }

    /// <summary>
    /// 駒がなれるか確認し、なれる場合は成る処理を行う
    /// </summary>
    private IEnumerator TryPromote(Vector2Int previousLogicPos, Vector2Int destination)
    {
        // 相手陣地に入っているかどうか
        bool isInPromotionZone = (PlayerSide == PlayerSide.BOTTOM && destination.y >= 6) ||
                                 (PlayerSide == PlayerSide.TOP && destination.y <= 2) ||
                                 (PlayerSide == PlayerSide.BOTTOM && previousLogicPos.y >= 6) ||
                                 (PlayerSide == PlayerSide.TOP && previousLogicPos.y <= 2);
        // 成れる駒かどうか
        bool isPromotablePiece = PieceType == PieceType.FU ||
                                 PieceType == PieceType.KYOSHA ||
                                 PieceType == PieceType.KEIMA ||
                                 PieceType == PieceType.GIN ||
                                 PieceType == PieceType.KAKU ||
                                 PieceType == PieceType.HISHA;
        // 移動の前後どちらかで相手陣地に入っていて、まだ成っていなくて、今ターンに盤上に出た駒でなければ成る
        if(isInPromotionZone && !IsPromoted && IsMainStagePiece && isPromotablePiece)
        {
            // 成れる状態

            // 1列目の歩, 香車, 2列目の桂馬は強制的に成る
            bool isForcedPromotion = ((PieceType == PieceType.FU || PieceType == PieceType.KYOSHA) &&
                                     ((PlayerSide == PlayerSide.BOTTOM && destination.y == 8) ||
                                      (PlayerSide == PlayerSide.TOP && destination.y == 0)))
                                     ||
                                     (PieceType == PieceType.KEIMA &&
                                     ((PlayerSide == PlayerSide.BOTTOM && destination.y >= 7) ||
                                      (PlayerSide == PlayerSide.TOP && destination.y <= 1)));
            if (isForcedPromotion)
            {
                // 強制的に成る
                ChangePromotionState(true);
                Debug.Log($"Piece forced promoted: {PieceType} at {destination}");
            }
            else
            {
                // 任意で成る場合

                // 成るか確認UIを表示し、プレイヤーの選択を待つ
                yield return _uiManager.ShowPromotionConfirmUI(isYesSelected =>
                {
                    // 成るが選択された場合
                    if (isYesSelected)
                    {
                        // 成る処理を行う
                        ChangePromotionState(true);
                        Debug.Log($"Piece promoted: {PieceType} at {destination}");
                    }
                });
            }
        }
    }
}

/// <summary>
/// 駒の種類列挙型
/// </summary>
public enum PieceType
{
    /// <summary>
    /// 歩兵
    /// </summary>
    FU = 0,
    /// <summary>
    /// 香車
    /// </summary>
    KYOSHA = 1,
    /// <summary>
    /// 桂馬
    /// </summary>
    KEIMA = 2,
    /// <summary>
    /// 銀将
    /// </summary>
    GIN = 3,
    /// <summary>
    /// 金将
    /// </summary>
    KIN = 4,
    /// <summary>
    /// 角行
    /// </summary>
    KAKU = 5,
    /// <summary>
    /// 飛車
    /// </summary>
    HISHA = 6,
    /// <summary>
    /// 王将
    /// </summary>
    OU = 7,
    /// <summary>
    /// 玉将
    /// </summary>
    GYOKU = 8,
}

/// <summary>
/// プレイヤーのプレイサイド列挙型
/// </summary>
public enum PlayerSide
{
    /// <summary>
    /// 下側プレイヤー
    /// </summary>
    BOTTOM = 0,
    /// <summary>
    /// 上側プレイヤー
    /// </summary>
    TOP = 9,
}
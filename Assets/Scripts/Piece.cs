using UnityEngine;

/// <summary>
/// 駒を表すコンポーネント
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
    public PieceType PieceType
    {
        get => _pieceType;
        set => _pieceType = value;
    }
    /// <summary>
    /// 成っているかどうか
    /// </summary>
    [SerializeField] private bool _isPromoted = false;
    /// <summary>
    /// 成っているかどうか
    /// </summary>
    public bool IsPromoted
    {
        get => _isPromoted;
        set => _isPromoted = value;
    }
    /// <summary>
    /// メインステージの駒かどうか(持ち駒かどうか)
    /// </summary>
    [SerializeField] private bool _isMainStagePiece = true;
    /// <summary>
    /// メインステージの駒かどうか(持ち駒かどうか)
    /// </summary>
    public bool IsMainStagePiece
    {
        get => _isMainStagePiece;
        set => _isMainStagePiece = value;
    }
    /// <summary>
    /// プレイヤーサイド
    /// </summary>
    [SerializeField] private PlayerSide _playerSide;
    /// <summary>
    /// プレイヤーサイド
    /// </summary>
    public PlayerSide PlayerSide
    {
        get => _playerSide;
        set => _playerSide = value;
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
using UnityEngine;

/// <summary>
/// 駒を表すコンポーネント
/// </summary>
public class Piece : MonoBehaviour
{
    /// <summary>
    /// 駒の種類
    /// </summary>
    [SerializeField] public PieceType _pieceType;
    /// <summary>
    /// 成っているかどうか
    /// </summary>
    [SerializeField] public bool _isPromoted = false;
    /// <summary>
    /// メインステージの駒かどうか(持ち駒かどうか)
    /// </summary>
    [SerializeField] public bool _isMainStagePiece = true;
    /// <summary>
    /// プレイヤーサイド
    /// </summary>
    [SerializeField] public PlayerSide _playerSide;
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
    Bottom = 0,
    /// <summary>
    /// 上側プレイヤー
    /// </summary>
    Top = 9,
}
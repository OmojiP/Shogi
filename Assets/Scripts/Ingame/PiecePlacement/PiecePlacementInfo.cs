using Shogi.Piece;
using Shogi.Player;
using UnityEngine;

namespace Shogi.PiecePlacement
{
    /// <summary>
    /// １つの駒の配置情報を表すクラス.
    /// 駒の種類、プレイヤーサイド、ロジック座標、成り状態を保持
    /// </summary>
    public class PiecePlacementInfo
    {
        public PiecePlacementInfo(PieceType pieceType, PlayerSide playerSide, Vector2Int logicPosition, bool isPromoted)
        {
            PieceType = pieceType;
            PlayerSide = playerSide;
            LogicPosition = logicPosition;
            IsPromoted = isPromoted;
        }

        /// <summary>
        /// 駒の種類
        /// </summary>
        public PieceType PieceType { get; set; }
        /// <summary>
        /// 駒の所属するプレイヤーサイド
        /// </summary>
        public PlayerSide PlayerSide { get; set; }
        /// <summary>
        /// 駒のロジック上の座標
        /// </summary>
        public Vector2Int LogicPosition { get; set; }
        /// <summary>
        /// 成り状態
        /// </summary>
        public bool IsPromoted { get; set; }
    }
}
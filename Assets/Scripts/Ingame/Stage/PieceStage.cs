using Shogi.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shogi.Stage
{
    /// <summary>
    /// 駒台を表すコンポーネント.
    /// 駒台への駒の追加・削除と整列を担当
    /// </summary>
    public class PieceStage : MonoBehaviour
    {
        /// <summary>
        /// 駒の配置開始箇所のx座標オフセット
        /// </summary>
        [SerializeField] private float _pieceStagePlaceStartOffsetX = -0.4f;
        /// <summary>
        /// 駒の配置箇所のx座標間隔
        /// </summary>
        [SerializeField] private float _pieceStagePlaceSpanX = 0.1f;

        /// <summary>
        /// 駒台のプレイヤーサイド
        /// </summary>
        [SerializeField] private PlayerSide _playerSide;

        /// <summary>
        /// 置かれた駒のリスト
        /// </summary>
        List<Piece.Piece> _pieces = new List<Piece.Piece>();

        /// <summary>
        /// 駒を追加する
        /// </summary>
        /// <param name="piece"></param>
        public IEnumerator AddPiece(Piece.Piece piece)
        {
            // 駒をリストに追加し、整列させる
            _pieces.Add(piece);
            yield return piece.MoveToPieceStage(_playerSide, this.transform.position);
            RearrangePieces();
        }

        /// <summary>
        /// 駒を取り除く
        /// </summary>
        /// <param name="piece">取り除く駒</param>
        public void RemovePiece(Piece.Piece piece)
        {
            // 駒をリストから取り除き、整列させる
            _pieces.Remove(piece);
            RearrangePieces();
        }

        /// <summary>
        /// 駒台の駒を整列させる
        /// </summary>
        private void RearrangePieces()
        {
            // 駒を position.x + _pieceStagePlaceStartOffsetX から _pieceStagePlaceSpanX 間隔で配置する
            for (int i = 0; i < _pieces.Count; i++)
            {
                Piece.Piece p = _pieces[i];
                // 駒の配置場所
                var piecePlacePosX = this.transform.position.x + _pieceStagePlaceStartOffsetX + _pieceStagePlaceSpanX * i;
                Vector3 offBoardPosition = new Vector3(
                    piecePlacePosX,
                    p.transform.position.y,
                    this.transform.position.z
                );
                p.transform.position = offBoardPosition;
            }
        }
    }
}
using R3;
using UnityEngine;

namespace Shogi.Input
{
    /// <summary>
    /// 入力管理コンポーネント
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        /// <summary>
        /// クリック判定用レイキャスター
        /// </summary>
        [SerializeField] private ClickRaycaster _clickRaycaster;

        /// <summary>
        /// 駒がクリックされたときに発行するイベント
        /// </summary>
        private Subject<Piece.Piece> _onPieceClicked = new();
        /// <summary>
        /// 駒がクリックされたときに発行するイベント
        /// </summary>
        public Observable<Piece.Piece> OnPieceClicked => _onPieceClicked;
        /// <summary>
        /// マスがクリックされたときに発行するイベント
        /// </summary>
        private Subject<Stage.StageCell> _onStageCellClicked = new();
        /// <summary>
        /// マスがクリックされたときに発行するイベント
        /// </summary>
        public Observable<Stage.StageCell> OnStageCellClicked => _onStageCellClicked;

        void Update()
        {
            // 駒がクリックされたか判定
            if (_clickRaycaster.TryGetClickedPiece(out Piece.Piece clickedPiece))
            {
                // クリックされた駒でイベントを発行
                _onPieceClicked.OnNext(clickedPiece);
            }
            // 駒がクリックされなかった場合、マスがクリックされたか判定
            else if (_clickRaycaster.TryGetClickedStageCell(out Stage.StageCell clickedCell))
            {
                // クリックされたマスでイベントを発行
                _onStageCellClicked.OnNext(clickedCell);

            }
        }
    }
}
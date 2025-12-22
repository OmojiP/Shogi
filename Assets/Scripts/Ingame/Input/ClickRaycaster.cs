using UnityEngine;

namespace Shogi.Input
{
    /// <summary>
    /// クリック位置に駒やマスがあるか判定するコンポーネント
    /// </summary>
    public class ClickRaycaster : MonoBehaviour
    {
        /// <summary>
        /// クリックした位置に駒があればtrueを返し、その駒をclickedPieceに格納する関数
        /// </summary>
        /// <param name="clickedPiece">取得した駒</param>
        /// <returns></returns>
        public bool TryGetClickedPiece(out Piece.Piece clickedPiece)
        {
            // outパラメータの初期化
            clickedPiece = null;

            // そのフレームでクリックしてなければ, falseを返す
            if (!UnityEngine.Input.GetMouseButtonDown(0))
                return false;

            // Ray をマウス位置から飛ばし、全ての当たり判定を取得する
            Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                // Pieceタグのオブジェクトがあれば優先して返す
                if (hit.collider.CompareTag("Piece"))
                {
                    // Pieceコンポーネントを取得
                    clickedPiece = hit.collider.GetComponent<Piece.Piece>();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// クリックした位置に将棋盤のマスがあればtrueを返し、そのマスをclickedCellに格納する関数
        /// </summary>
        /// <param name="clickedCell">取得したマス</param>
        /// <returns></returns>
        public bool TryGetClickedStageCell(out Stage.StageCell clickedCell)
        {
            // outパラメータの初期化
            clickedCell = null;

            // そのフレームでクリックしてなければ, falseを返す
            if (!UnityEngine.Input.GetMouseButtonDown(0))
                return false;

            // Ray をマウス位置から飛ばし、全ての当たり判定を取得する
            Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                // MainStageCellタグのオブジェクトがあれば返す
                if (hit.collider.CompareTag("MainStageCell"))
                {
                    // StageCellコンポーネントを取得
                    clickedCell = hit.collider.GetComponent<Stage.StageCell>();
                    return true;
                }
            }

            return false;
        }
    }
}
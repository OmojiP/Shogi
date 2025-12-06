using UnityEngine;

/// <summary>
/// クリックしたオブジェクトの座標を取得するコンポーネント.
/// Piece, MainStageCell, PieceStage タグの順に優先して座標を返す.
/// </summary>
public class ClickRaycaster : MonoBehaviour
{
    /// <summary>
    /// クリックした位置に特定のタグのオブジェクトがあればtrueを返し、そのオブジェクトの座標を返す関数.
    /// タグが"Piece"の場合、clickedPieceを返す。取得できなければ null を返す.
    /// </summary>
    /// <param name="position">クリックした位置のワールド座標</param>
    /// <param name="clickedPiece">取得したPiece</param>
    /// <returns></returns>
    public bool TryGetClickedPosition(out Vector3 position, out Piece clickedPiece)
    {
        // outパラメータの初期化
        position = Vector3.zero;
        clickedPiece = null;

        // そのフレームでクリックしてなければ, falseを返す
        if (!Input.GetMouseButtonDown(0))
            return false;

        // Piece以外をクリックしたかどうかのフラグ(Pieceタグを優先して返すために他のタグを先に見つけた場合はフラグを立てる)
        bool isClickedMainStageCellOrPieceStage = false;

        // Ray をマウス位置から飛ばし、全ての当たり判定を取得する
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        for(int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            // Pieceタグのオブジェクトがあれば優先して返す
            if(hit.collider.CompareTag("Piece"))
            {
                // Pieceコンポーネントを取得
                clickedPiece = hit.collider.GetComponent<Piece>();
                position = hit.collider.transform.position;
                return true;
            }
            // MainStageCellタグかPieceStageタグのオブジェクトがあればフラグを立てて座標を保存して次のRaycastHitも確認する
            else if (hit.collider.CompareTag("MainStageCell") || hit.collider.CompareTag("PieceStage"))
            {
                // 他のRaycastHitも確認するためにフラグを立てる
                isClickedMainStageCellOrPieceStage = true;
                position = hit.collider.transform.position;
            }
        }

        return isClickedMainStageCellOrPieceStage;
    }
}

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
        position = Vector3.zero;
        clickedPiece = null;

        // クリックしてなければ false
        if (!Input.GetMouseButtonDown(0))
            return false;

        // Piece以外をクリックしたかどうかのフラグ
        bool isClickedMainStageCellOrPieceStage = false;

        // Ray を飛ばす
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        for(int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if(hit.collider.CompareTag("Piece"))
            {
                // Pieceコンポーネントを取得
                clickedPiece = hit.collider.GetComponent<Piece>();
                position = hit.collider.transform.position;
                return true;
            }
            else if (hit.collider.CompareTag("MainStageCell") || hit.collider.CompareTag("PieceStage"))
            {
                // 他のRaycastHitも確認するためにフラグを立てる
                isClickedMainStageCellOrPieceStage = true;
                position = hit.collider.transform.position;
            }
        }

        return isClickedMainStageCellOrPieceStage;
    }

    /// <summary>
    /// 指定ワールド座標にPieceがあればtrueを返し、そのPieceをclickedPieceに格納する関数
    /// </summary>
    /// <param name="position">Pieceの取得を試みるワールド座標</param>
    /// <param name="clickedPiece">取得したPiece</param>
    /// <returns></returns>
    public bool TryGetPiece(Vector3 position, out Piece clickedPiece)
    {
        clickedPiece = null;
        
        // Ray を飛ばす
        Ray ray = new Ray(position + Vector3.up * 10f, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        for(int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if(hit.collider.CompareTag("Piece"))
            {
                // Pieceコンポーネントを取得
                clickedPiece = hit.collider.GetComponent<Piece>();
                return true;
            }
        }

        return false;
    }
}

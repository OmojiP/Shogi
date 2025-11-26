using UnityEngine;

/// <summary>
/// クリックしたオブジェクトの座標を取得するコンポーネント.
/// Piece, MainStageCell, PieceStage タグの順に優先して座標を返す.
/// </summary>
public class ClickRaycaster : MonoBehaviour
{
    // タグに応じて座標を返す関数. clickedPriceは取得できなければ null を返す.
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

    // positionにPieceがあればそのPieceを返す関数
    public bool TryGetPiece(Vector3 position, out Piece clickedPiece)
    {
        clickedPiece = null;
        
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
                return true;
            }
        }

        return false;
    }
}

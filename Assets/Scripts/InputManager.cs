using UnityEngine;

/// <summary>
/// 入力管理コンポーネント
/// </summary>
public class InputManager : MonoBehaviour
{
    /// <summary>
    /// クリック判定用レイキャスター
    /// </summary>
    [SerializeField] private ClickRaycaster _clickRaycaster;

    [SerializeField] private Player[] _players;

    void Update()
    {
        // 駒がクリックされたか判定
        if(_clickRaycaster.TryGetClickedPiece(out Piece clickedPiece))
        {
            // 駒がクリックされた場合、Playerにそれを通知
            foreach(var player in _players)
            {
                player.OnPieceClicked(clickedPiece);
            }
        }
        // 駒がクリックされなかった場合、マスがクリックされたか判定
        else if(_clickRaycaster.TryGetClickedStageCell(out StageCell clickedCell))
        {
            // マスがクリックされた場合、Playerにそれを通知
            foreach(var player in _players)
            {
                player.OnCellClicked(clickedCell);
            }
        }
    }
}

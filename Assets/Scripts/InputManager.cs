using R3;
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

    /// <summary>
    /// 駒がクリックされたときに発行するイベント
    /// </summary>
    private Subject<Piece> _onPieceClicked = new();
    /// <summary>
    /// 駒がクリックされたときに発行するイベント
    /// </summary>
    public Observable<Piece> OnPieceClicked => _onPieceClicked;
    /// <summary>
    /// マスがクリックされたときに発行するイベント
    /// </summary>
    private Subject<StageCell> _onStageCellClicked = new();
    /// <summary>
    /// マスがクリックされたときに発行するイベント
    /// </summary>
    public Observable<StageCell> OnStageCellClicked => _onStageCellClicked;

    void Update()
    {
        // 駒がクリックされたか判定
        if(_clickRaycaster.TryGetClickedPiece(out Piece clickedPiece))
        {
            // クリックされた駒でイベントを発行
            _onPieceClicked.OnNext(clickedPiece);
        }
        // 駒がクリックされなかった場合、マスがクリックされたか判定
        else if(_clickRaycaster.TryGetClickedStageCell(out StageCell clickedCell))
        {
            // クリックされたマスでイベントを発行
            _onStageCellClicked.OnNext(clickedCell);

        }
    }
}

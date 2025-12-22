using R3;
using UnityEngine;

public class InputBinder : MonoBehaviour
{
    [SerializeField] private Player[] _players;
    [SerializeField] private InputManager _inputManager;

    private void Start()
    {
        // 入力が駒のクリックを検知したらそれをPlayerに流す
        _inputManager.OnPieceClicked.Subscribe(piece =>
        {
            foreach (var player in _players)
            {
                player.OnPieceClicked(piece);
            }
        }).AddTo(this);
        // 入力がマスのクリックを検知したらそれをPlayerに流す
        _inputManager.OnStageCellClicked.Subscribe(stageCell =>
        {
            foreach (var player in _players)
            {
                player.OnCellClicked(stageCell);
            }
        }).AddTo(this);
    }
}

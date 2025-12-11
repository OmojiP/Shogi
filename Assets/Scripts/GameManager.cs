using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// インゲーム全体を管理するコンポーネント.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// 駒の初期配置のCSVデータ
    /// "x座標, y座標, 駒名, Upper/Lower" の形式で記述
    /// </summary>
    [SerializeField] private TextAsset _initialPlacementTextAsset;

    // 他のコンポーネント参照
    [SerializeField] private Player _playerTop;
    [SerializeField] private Player _playerBottom;
    [SerializeField] private MainStage _mainStage;
    [SerializeField] private IngameUIManager _ingameUIManager;
    [SerializeField] private PiecePlacementReader _piecePlacementReader;

    /// <summary>
    /// 現在のターン数
    /// </summary>
    private int _turnCount = 0;

    public void Start()
    {
        // UIの初期化
        _ingameUIManager.InitializeUI();

        // 将棋盤のマスを生成
        _mainStage.GenerateStageCells();

        // 駒の初期配置データを解析
        var initialPlacementInfos = _piecePlacementReader.ParseInitialPlacementCSVData(_initialPlacementTextAsset);
        // 将棋盤に駒を生成
        _mainStage.SpawnPieces(initialPlacementInfos);

        // ゲーム開始
        // 現時点ではBottomプレイヤーから開始
        _playerBottom.StartPlayerTurn();
    }

    /// <summary>
    /// プレイヤーのターン終了時の処理
    /// </summary>
    /// <param name="endedPlayerSide"></param>
    public void OnPlayerTurnEnded(PlayerSide endedPlayerSide)
    {
        // ターン終了時の処理
        Debug.Log($"[GameManager] Player {endedPlayerSide} turn ended.");

        // ターン数を更新
        if (endedPlayerSide == PlayerSide.BOTTOM)
        {
            _turnCount++;
        }

        // 次のプレイヤーのターンを開始
        PlayerSide nextPlayerSide = (endedPlayerSide == PlayerSide.BOTTOM) ? PlayerSide.TOP : PlayerSide.BOTTOM;
        if (nextPlayerSide == PlayerSide.BOTTOM)
        {
            _playerBottom.StartPlayerTurn();
        }
        else
        {
            _playerTop.StartPlayerTurn();
        }
    }

    /// <summary>
    /// ゲーム終了時の処理
    /// </summary>
    /// <param name="winnerSide"></param>
    public void OnGameEnded(PlayerSide winnerSide)
    {
        // ゲーム終了時の処理
        Debug.Log($"[GameManager] Game ended. Winner: {winnerSide}");

        // 結果UIを表示
        _ingameUIManager.ShowResultUI(winnerSide);
    }
}
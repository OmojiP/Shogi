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

    [SerializeField] private Player _playerTop;
    [SerializeField] private Player _playerBottom;
    [SerializeField] private MainStage _mainStage;
    [SerializeField] private IngameUIManager _ingameUIManager;

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
        
        // 将棋盤に駒を生成
        var initialPlacementInfos = ParseInitialPlacementCSVData(_initialPlacementTextAsset);
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

    /// <summary>
    /// 初期配置CSVデータの解析
    /// </summary>
    /// <param name="initialPlacementCSV"></param>
    /// <returns></returns>
    private PiecePlacementInfo[] ParseInitialPlacementCSVData(TextAsset initialPlacementCSV)
    {
        List<PiecePlacementInfo> placementInfos = new List<PiecePlacementInfo>();

        // 初期配置データの解析
        string[] lines = initialPlacementCSV.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            // x座標, y座標, 駒名, Upper/Lower の形式で記述されていることを想定して解析
            
            // カンマで分割
            string[] tokens = line.Split(',');
            // 要素数が4でない場合はスキップ
            if (tokens.Length != 4) continue; // 不正な行はスキップ

            int x, y;
            string pieceName;
            string playerSide;
            PieceType pieceType;
            try
            {
                // x, y を整数に変換
                x = int.Parse(tokens[0]);
                y = int.Parse(tokens[1]);
                // 駒名とプレイヤーサイドを取得
                pieceName = tokens[2].Trim();
                playerSide = tokens[3].Trim();
            }
            catch (System.Exception e)
            {
                // 解析に失敗した場合は警告を出してスキップ
                Debug.LogWarning($"Failed to parse line: {line}. Exception: {e.Message}");
                continue;
            }

            // 駒タイプを決定 文字列からPieceType列挙型へ変換
            switch (pieceName)
            {
                case "FU":
                    pieceType = PieceType.FU;
                    break;
                case "KYOSHA":
                    pieceType = PieceType.KYOSHA;
                    break;
                case "KEIMA":
                    pieceType = PieceType.KEIMA;
                    break;
                case "GIN":
                    pieceType = PieceType.GIN;
                    break;
                case "KIN":
                    pieceType = PieceType.KIN;
                    break;
                case "KAKU":
                    pieceType = PieceType.KAKU;
                    break;
                case "HISHA":
                    pieceType = PieceType.HISHA;
                    break;
                case "OU":
                    pieceType = PieceType.OU;
                    break;
                case "GYOKU":
                    pieceType = PieceType.GYOKU;
                    break;
                default:
                    Debug.LogWarning($"Unknown piece name: {pieceName}");
                    continue; // 不明な駒名はスキップ
            }

            // 駒配置情報リストに追加
            placementInfos.Add(
                new PiecePlacementInfo(
                    pieceType,
                    (playerSide == "Upper") ? PlayerSide.TOP : PlayerSide.BOTTOM,
                    new Vector2Int(x, y),
                    false
                )
            );
        }

        return placementInfos.ToArray();
    }

}
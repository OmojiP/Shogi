using R3;
using Shogi.Player;
using UnityEngine;

namespace Shogi.Sequence
{
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
        [SerializeField] private Player.Player _playerTop;
        [SerializeField] private Player.Player _playerBottom;
        [SerializeField] private Stage.MainStage _mainStage;
        [SerializeField] private PiecePlacement.PiecePlacementReader _piecePlacementReader;

        /// <summary>
        /// ゲームが終了したタイミングで勝者を通知するイベント
        /// </summary>
        private Subject<PlayerSide> _onEndedGame = new();
        /// <summary>
        /// ゲームが終了したタイミングで勝者を通知するイベント
        /// </summary>
        public Observable<PlayerSide> OnEndedGame => _onEndedGame;

        /// <summary>
        /// 現在のターン数
        /// </summary>
        private int _turnCount = 0;

        public void Start()
        {
            // 将棋盤のマスを生成
            _mainStage.GenerateStageCells();

            // 駒の初期配置データを解析
            var initialPlacementInfos = _piecePlacementReader.ParseInitialPlacementCSVData(_initialPlacementTextAsset);
            // 将棋盤に駒を生成
            _mainStage.SpawnPieces(initialPlacementInfos);

            // イベントを登録
            RegisterEvents();

            // ゲーム開始
            // 現時点ではBottomプレイヤーから開始
            _playerBottom.StartPlayerTurn();
        }

        /// <summary>
        /// イベントを登録する
        /// </summary>
        private void RegisterEvents()
        {
            // Playerが駒を動かし終えたタイミングでターンを終了する処理を登録
            _playerBottom.CurrentPlayerTurnPhase
                .Pairwise()
                .Where(statePair =>
                    // 駒を動かし終わったら
                    statePair.Previous == PlayerTurnPhaseType.PIECE_MOVING &&
                    statePair.Current != PlayerTurnPhaseType.PIECE_MOVING)
                .Subscribe(_ =>
                {
                    // ターン終了として、次のPlayerにターンを回す
                    EndPlayerTurn(PlayerSide.BOTTOM);
                })
                .AddTo(this);
            // Playerが駒を動かし終えたタイミングでターンを終了する処理を登録
            _playerTop.CurrentPlayerTurnPhase
                .Pairwise()
                .Where(statePair =>
                    // 駒を動かし終わったら    
                    statePair.Previous == PlayerTurnPhaseType.PIECE_MOVING &&
                    statePair.Current != PlayerTurnPhaseType.PIECE_MOVING)
                .Subscribe(_ =>
                {
                    // ターン終了として、次のPlayerにターンを回す
                    EndPlayerTurn(PlayerSide.TOP);
                })
                .AddTo(this);

            // 下側のPlayerが王をとった時の処理を登録
            _playerBottom.OnGottenKing
                .Subscribe(_ =>
                {
                    // 下側のPlayerの勝利でゲーム終了
                    EndGame(PlayerSide.BOTTOM);
                })
                .AddTo(this);
            // 上側のPlayerが王をとった時の処理を登録
            _playerTop.OnGottenKing
                .Subscribe(_ =>
                {
                    // 上側のPlayerの勝利でゲーム終了
                    EndGame(PlayerSide.TOP);
                })
                .AddTo(this);
        }

        /// <summary>
        /// プレイヤーのターン終了時の処理
        /// </summary>
        /// <param name="endedPlayerSide"></param>
        public void EndPlayerTurn(PlayerSide endedPlayerSide)
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
        public void EndGame(PlayerSide winnerSide)
        {
            // ゲーム終了時の処理
            Debug.Log($"[GameManager] Game ended. Winner: {winnerSide}");

            // 勝者を通知
            _onEndedGame.OnNext(winnerSide);
        }
    }
}
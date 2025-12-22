using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shogi.Piece;
using Shogi.Player;

namespace Shogi.Logic
{
    /// <summary>
    /// ロジック定数群
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// 盤のサイズ
        /// </summary>
        public static readonly int BOARD_SIZE = 9;
    }

    /// <summary>
    /// ロジック関数群
    /// </summary>
    public static class Functions
    {
        /// <summary>
        /// 移動先候補を取得
        /// </summary>
        /// <param name="playerPiece">移動させる駒</param>
        /// <param name="playerSide">駒のプレイヤーサイド</param>
        /// <returns>移動先候補のロジック座標配列</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Vector2Int[] GetMoveDestinationCandidates(Piece.Piece playerPiece, Stage.MainStage mainStage)
        {
            Debug.Log($"Getting move destinations for piece: {playerPiece.PieceType} at {playerPiece.LogicPos}");

            // 移動先候補リスト
            List<Vector2Int> destinationCandidates = new List<Vector2Int>();

            // 持ち駒の場合
            if (!playerPiece.IsMainStagePiece)
            {
                // 基本的に全空きマスに移動可能
                // TODO: 打ち歩詰めは未対応
                for (int x = 0; x < Constants.BOARD_SIZE; x++)
                {
                    for (int y = 0; y < Constants.BOARD_SIZE; y++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);

                        // 歩, 香車は1列目, 桂馬は2列目には打てない
                        bool isInvalidDropPosition = (
                            (playerPiece.PieceType == PieceType.FU || playerPiece.PieceType == PieceType.KYOSHA) &&
                            ((playerPiece.PlayerSide == PlayerSide.BOTTOM && y == Constants.BOARD_SIZE - 1) || (playerPiece.PlayerSide == PlayerSide.TOP && y == 0))
                        ) || (
                            playerPiece.PieceType == PieceType.KEIMA &&
                            ((playerPiece.PlayerSide == PlayerSide.BOTTOM && y >= Constants.BOARD_SIZE - 2) || (playerPiece.PlayerSide == PlayerSide.TOP && y <= 1))
                        );

                        // 打てない場所でなく、そのマスに駒が存在しない場合のみ追加
                        if (!isInvalidDropPosition && !mainStage.TryGetPieceAt(pos, out var occupyingPiece))
                        {
                            destinationCandidates.Add(pos);
                        }
                    }
                }

                // 二歩となる座標を除外
                if (playerPiece.PieceType == PieceType.FU)
                {
                    foreach (var fu in mainStage.FuPieces)
                    {
                        // 同じプレイヤーサイドのと金でない歩のみ対象
                        if (fu.PlayerSide == playerPiece.PlayerSide && fu.IsMainStagePiece && !fu.IsPromoted)
                        {
                            // 歩のいる列を取得
                            int fuColumn = fu.LogicPos.x;
                            // その列のマスを移動先候補から削除
                            destinationCandidates.RemoveAll(pos => pos.x == fuColumn);
                        }
                    }
                }
            }
            // 盤上の駒の場合
            else
            {
                // 実際に扱う駒の種類を決定
                PieceType actualPieceType = playerPiece.PieceType;

                // 歩, 香車, 桂馬, 銀将 は成っていた場合, 金将として扱う
                if (playerPiece.IsPromoted)
                {
                    if (playerPiece.PieceType == PieceType.FU ||
                    playerPiece.PieceType == PieceType.KYOSHA ||
                    playerPiece.PieceType == PieceType.KEIMA ||
                    playerPiece.PieceType == PieceType.GIN)
                    {
                        // 金将として扱う
                        actualPieceType = PieceType.KIN;
                    }
                }

                // 駒の種類ごとに移動先候補を取得(範囲外チェックと角龍の成の扱いは後ろでまとめて行う)
                switch (actualPieceType)
                {
                    case PieceType.FU:
                        // 歩は1マス前進のみ
                        if (playerPiece.PlayerSide == PlayerSide.BOTTOM)
                        {
                            destinationCandidates = new List<Vector2Int>
                        {
                            new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y + 1)
                        };
                        }
                        else // PlayerSide.Top
                        {
                            destinationCandidates = new List<Vector2Int>
                        {
                            new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y - 1)
                        };
                        }
                        break;
                    case PieceType.KYOSHA:
                        // 香車は前方に何マスでも進める
                        if (playerPiece.PlayerSide == PlayerSide.BOTTOM)
                        {
                            for (int y = playerPiece.LogicPos.y + 1; y < Constants.BOARD_SIZE; y++)
                            {
                                // 候補に追加
                                destinationCandidates.Add(new Vector2Int(playerPiece.LogicPos.x, y));
                                // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                                if (mainStage.TryGetPieceAt(new Vector2Int(playerPiece.LogicPos.x, y), out var occupyingPiece))
                                {
                                    break;
                                }
                            }
                        }
                        else // PlayerSide.Top
                        {
                            for (int y = playerPiece.LogicPos.y - 1; y >= 0; y--)
                            {
                                // 候補に追加
                                destinationCandidates.Add(new Vector2Int(playerPiece.LogicPos.x, y));
                                // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                                if (mainStage.TryGetPieceAt(new Vector2Int(playerPiece.LogicPos.x, y), out var occupyingPiece))
                                {
                                    break;
                                }
                            }
                        }
                        break;
                    case PieceType.KEIMA:
                        // 桂馬は2マス前方に1マス左右
                        if (playerPiece.PlayerSide == PlayerSide.BOTTOM)
                        {
                            destinationCandidates = new List<Vector2Int>
                        {
                            new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y + 2),
                            new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y + 2)
                        };
                        }
                        else // PlayerSide.Top
                        {
                            destinationCandidates = new List<Vector2Int>
                        {
                            new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y - 2),
                            new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y - 2)
                        };
                        }
                        break;
                    case PieceType.GIN:
                        // 銀将は1マス前方と斜め前方、斜め後方に移動可能
                        if (playerPiece.PlayerSide == PlayerSide.BOTTOM)
                        {
                            destinationCandidates = new List<Vector2Int>
                        {
                            new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y + 1),
                            new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y + 1),
                            new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y + 1),
                            new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y - 1),
                            new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y - 1)
                        };
                        }
                        else // PlayerSide.Top
                        {
                            destinationCandidates = new List<Vector2Int>
                        {
                            new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y - 1),
                            new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y - 1),
                            new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y - 1),
                            new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y + 1),
                            new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y + 1)
                        };
                        }
                        break;
                    case PieceType.KIN:
                        // 金将は1マス前方、左右、前方斜めに移動可能
                        if (playerPiece.PlayerSide == PlayerSide.BOTTOM)
                        {
                            destinationCandidates = new List<Vector2Int>
                        {
                            new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y + 1),
                            new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y),
                            new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y),
                            new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y + 1),
                            new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y - 1),
                            new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y + 1)
                        };
                        }
                        else // PlayerSide.Top
                        {
                            destinationCandidates = new List<Vector2Int>
                        {
                            new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y - 1),
                            new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y),
                            new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y),
                            new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y - 1),
                            new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y + 1),
                            new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y - 1)
                        };
                        }
                        break;
                    case PieceType.KAKU:
                        // 角行は斜め方向に何マスでも移動可能
                        // 斜めに追加していき、味方がいる場合はそのマスの前まで、相手がいる場合はそのマスまで追加して止める
                        // 左上
                        for (int distance = 1; distance < Constants.BOARD_SIZE; distance++)
                        {
                            Vector2Int p0 = new Vector2Int(playerPiece.LogicPos.x + distance, playerPiece.LogicPos.y + distance);

                            // 候補に追加
                            destinationCandidates.Add(p0);
                            // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                            if (mainStage.TryGetPieceAt(p0, out var occupyingPiece))
                            {
                                break;
                            }
                        }
                        // 右上
                        for (int distance = 1; distance < Constants.BOARD_SIZE; distance++)
                        {
                            Vector2Int p1 = new Vector2Int(playerPiece.LogicPos.x - distance, playerPiece.LogicPos.y + distance);

                            // 候補に追加
                            destinationCandidates.Add(p1);
                            // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                            if (mainStage.TryGetPieceAt(p1, out var occupyingPiece))
                            {
                                break;
                            }
                        }
                        // 左下
                        for (int distance = 1; distance < Constants.BOARD_SIZE; distance++)
                        {
                            Vector2Int p2 = new Vector2Int(playerPiece.LogicPos.x + distance, playerPiece.LogicPos.y - distance);

                            // 候補に追加
                            destinationCandidates.Add(p2);
                            // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                            if (mainStage.TryGetPieceAt(p2, out var occupyingPiece))
                            {
                                break;
                            }
                        }
                        // 右下
                        for (int distance = 1; distance < Constants.BOARD_SIZE; distance++)
                        {
                            Vector2Int p3 = new Vector2Int(playerPiece.LogicPos.x - distance, playerPiece.LogicPos.y - distance);

                            // 候補に追加
                            destinationCandidates.Add(p3);
                            // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                            if (mainStage.TryGetPieceAt(p3, out var occupyingPiece))
                            {
                                break;
                            }
                        }
                        break;
                    case PieceType.HISHA:
                        // 飛車は縦横方向に何マスでも移動可能
                        // 縦横に追加していき、味方がいる場合はそのマスの前まで、相手がいる場合はそのマスまで追加して止める
                        // 上
                        for (int distance = 1; distance < Constants.BOARD_SIZE; distance++)
                        {
                            Vector2Int p0 = new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y + distance);

                            // 候補に追加
                            destinationCandidates.Add(p0);
                            // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                            if (mainStage.TryGetPieceAt(p0, out var occupyingPiece))
                            {
                                break;
                            }
                        }
                        // 下
                        for (int distance = 1; distance < Constants.BOARD_SIZE; distance++)
                        {
                            Vector2Int p1 = new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y - distance);

                            // 候補に追加
                            destinationCandidates.Add(p1);
                            // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                            if (mainStage.TryGetPieceAt(p1, out var occupyingPiece))
                            {
                                break;
                            }
                        }
                        // 左
                        for (int distance = 1; distance < Constants.BOARD_SIZE; distance++)
                        {
                            Vector2Int p2 = new Vector2Int(playerPiece.LogicPos.x - distance, playerPiece.LogicPos.y);

                            // 候補に追加
                            destinationCandidates.Add(p2);
                            // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                            if (mainStage.TryGetPieceAt(p2, out var occupyingPiece))
                            {
                                break;
                            }
                        }
                        // 右
                        for (int distance = 1; distance < Constants.BOARD_SIZE; distance++)
                        {
                            Vector2Int p3 = new Vector2Int(playerPiece.LogicPos.x + distance, playerPiece.LogicPos.y);

                            // 候補に追加
                            destinationCandidates.Add(p3);
                            // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                            if (mainStage.TryGetPieceAt(p3, out var occupyingPiece))
                            {
                                break;
                            }
                        }
                        break;
                    case PieceType.OU or PieceType.GYOKU:
                        // 王将・玉将は1マス八方に移動可能
                        destinationCandidates = new List<Vector2Int>
                    {
                        new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y),
                        new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y),
                        new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y - 1),
                        new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y + 1),
                        new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y - 1),
                        new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y - 1),
                        new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y + 1),
                        new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y + 1)
                    };
                        break;
                    default:
                        throw new System.NotImplementedException("未対応の駒タイプです");
                }

                // 角行・飛車は成っていた場合、候補に王将・玉将の動きを追加
                if (playerPiece.IsPromoted)
                {
                    if (actualPieceType == PieceType.KAKU ||
                    actualPieceType == PieceType.HISHA)
                    {
                        // 王将・玉将の動きを追加
                        destinationCandidates.AddRange(new List<Vector2Int>
                    {
                        new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y),
                        new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y),
                        new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y - 1),
                        new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y + 1),
                        new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y - 1),
                        new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y - 1),
                        new Vector2Int(playerPiece.LogicPos.x - 1, playerPiece.LogicPos.y + 1),
                        new Vector2Int(playerPiece.LogicPos.x + 1, playerPiece.LogicPos.y + 1)
                    });
                    }
                }

                // 将棋盤の範囲外の候補を削除
                destinationCandidates.RemoveAll(pos => pos.x < 0 || pos.x >= Constants.BOARD_SIZE || pos.y < 0 || pos.y >= Constants.BOARD_SIZE);

                // 味方の駒がいるマスの候補を削除
                destinationCandidates.RemoveAll(pos => mainStage.TryGetPieceAt(pos, out var occupyingPiece) && occupyingPiece.PlayerSide == playerPiece.PlayerSide);
            }

            Debug.Log($"候補座標: {string.Join(", ", destinationCandidates)}");

            return destinationCandidates.ToArray();
        }

        /// <summary>
        /// 駒が成れるかどうかを確認した結果の列挙型
        /// </summary>
        public enum PiecePromoteJudgementType
        {
            /// <summary>
            /// 成ることができない
            /// </summary>
            CANT_PROMOTE = 0,
            /// <summary>
            /// 強制的に成る必要がある
            /// </summary>
            FORCE_PROMOTE = 1,
            /// <summary>
            /// 成るか選択できる
            /// </summary>
            SELECTABLE_PROMOTE = 2,
        }

        /// <summary>
        /// 駒が成れるか確認する
        /// </summary>
        /// <param name="piece">移動する駒</param>
        /// <param name="isFromMainStage">移動前は将棋盤上にあったか</param>
        /// <param name="previousPos">移動前の座標</param>
        /// <param name="destinationPos">移動後の座標</param>
        /// <returns></returns>
        public static PiecePromoteJudgementType CheckPiecePromotable(Piece.Piece piece, bool isFromMainStage, Vector2Int previousPos, Vector2Int destinationPos)
        {
            // 相手陣地に入っているかどうか
            bool isInPromotionZone = (piece.PlayerSide == PlayerSide.BOTTOM && destinationPos.y >= 6) ||
                                     (piece.PlayerSide == PlayerSide.TOP && destinationPos.y <= 2) ||
                                     (piece.PlayerSide == PlayerSide.BOTTOM && previousPos.y >= 6) ||
                                     (piece.PlayerSide == PlayerSide.TOP && previousPos.y <= 2);
            // 成れる駒かどうか
            bool isPromotablePiece = piece.PieceType == PieceType.FU ||
                                     piece.PieceType == PieceType.KYOSHA ||
                                     piece.PieceType == PieceType.KEIMA ||
                                     piece.PieceType == PieceType.GIN ||
                                     piece.PieceType == PieceType.KAKU ||
                                     piece.PieceType == PieceType.HISHA;
            // 移動の前後どちらかで相手陣地に入っていて、まだ成っていなくて、今ターンに盤上に出た駒でなければ成る
            if (isInPromotionZone && !piece.IsPromoted && isFromMainStage && isPromotablePiece)
            {
                // 少なくとも成れる状態(強制か任意かを確認する)

                // 1列目の歩, 香車, 2列目の桂馬は強制的に成る
                bool isForcedPromotion = ((piece.PieceType == PieceType.FU || piece.PieceType == PieceType.KYOSHA) &&
                                         ((piece.PlayerSide == PlayerSide.BOTTOM && destinationPos.y == 8) ||
                                          (piece.PlayerSide == PlayerSide.TOP && destinationPos.y == 0)))
                                         ||
                                         (piece.PieceType == PieceType.KEIMA &&
                                         ((piece.PlayerSide == PlayerSide.BOTTOM && destinationPos.y >= 7) ||
                                          (piece.PlayerSide == PlayerSide.TOP && destinationPos.y <= 1)));
                if (isForcedPromotion)
                {
                    // 強制的に成る
                    Debug.Log($"Piece forced promoted: {piece.PieceType} at {destinationPos}");
                    return PiecePromoteJudgementType.FORCE_PROMOTE;
                }
                else
                {
                    // 任意で成れる
                    return PiecePromoteJudgementType.SELECTABLE_PROMOTE;
                }
            }

            // 成ることはできない
            return PiecePromoteJudgementType.CANT_PROMOTE;
        }
    }
}
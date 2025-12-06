using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ロジック関数群
/// </summary>
public static class LogicFunction
{
    /// <summary>
    /// 盤のサイズ
    /// </summary>
    public static readonly int BOARD_SIZE = 9;

    /// <summary>
    /// 指定したロジック座標に駒が存在するかどうかを判定する
    /// </summary>
    /// <param name="logicPos">ロジック座標</param>
    /// <param name="occupyingPiece">駒が存在する場合、その駒の参照</param>
    /// <returns>駒が存在する場合はtrue、存在しない場合はfalse</returns>
    public static bool IsCellOccupied(Vector2Int logicPos, Piece[,] pieces, out Piece occupyingPiece)
    {
        // 範囲外チェック
        if (logicPos.x < 0 || logicPos.x >= BOARD_SIZE || logicPos.y < 0 || logicPos.y >= BOARD_SIZE)
        {
            occupyingPiece = null;
            return false;
        }

        // 指定したロジック座標に駒が存在すれば取得(存在しない場合null)
        occupyingPiece = pieces[logicPos.x, logicPos.y];

        // 駒が存在する場合はtrue、存在しない場合はfalseを返す
        return occupyingPiece != null;
    }
    
    /// <summary>
    /// 移動先候補を取得
    /// </summary>
    /// <param name="playerPiece">移動させる駒</param>
    /// <param name="playerSide">駒のプレイヤーサイド</param>
    /// <returns>移動先候補のロジック座標配列</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public static Vector2Int[] GetMoveDestinationCandidates(Piece playerPiece, Piece[,] pieces, Piece[] fuPieces)
    {

        Debug.Log($"Getting move destinations for piece: {playerPiece.PieceType} at {playerPiece.LogicPos}");

        // 移動先候補リスト
        List<Vector2Int> destinationCandidates = new List<Vector2Int>();

        // 持ち駒の場合
        if (!playerPiece.IsMainStagePiece)
        {
            // 基本的に全空きマスに移動可能
            // TODO: 打ち歩詰めは未対応
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                for (int y = 0; y < BOARD_SIZE; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);

                    // 歩, 香車は1列目, 桂馬は2列目には打てない
                    bool isInvalidDropPosition = (
                        (playerPiece.PieceType == PieceType.FU || playerPiece.PieceType == PieceType.KYOSHA) && 
                        ((playerPiece.PlayerSide == PlayerSide.BOTTOM && y == BOARD_SIZE - 1) || (playerPiece.PlayerSide == PlayerSide.TOP && y == 0))
                    ) || (
                        playerPiece.PieceType == PieceType.KEIMA &&
                        ((playerPiece.PlayerSide == PlayerSide.BOTTOM && y >= BOARD_SIZE - 2) || (playerPiece.PlayerSide == PlayerSide.TOP && y <= 1))
                    );

                    // 打てない場所でなく、そのマスに駒が存在しない場合のみ追加
                    if(!isInvalidDropPosition && !IsCellOccupied(pos, pieces, out var occupyingPiece))
                    {
                        destinationCandidates.Add(pos);
                    }
                }
            }

            // 二歩となる座標を除外
            if(playerPiece.PieceType == PieceType.FU)
            {
                foreach(var fu in fuPieces)
                {
                    // 同じプレイヤーサイドのと金でない歩のみ対象
                    if(fu.PlayerSide == playerPiece.PlayerSide && fu.IsMainStagePiece && !fu.IsPromoted)
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
            if(playerPiece.IsPromoted)
            {
                if(playerPiece.PieceType == PieceType.FU ||
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
                    if(playerPiece.PlayerSide == PlayerSide.BOTTOM)
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
                    if(playerPiece.PlayerSide == PlayerSide.BOTTOM)
                    {
                        for(int y = playerPiece.LogicPos.y + 1; y < BOARD_SIZE; y++)
                        {
                            // 候補に追加
                            destinationCandidates.Add(new Vector2Int(playerPiece.LogicPos.x, y));
                            // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                            if(IsCellOccupied(new Vector2Int(playerPiece.LogicPos.x, y), pieces, out var occupyingPiece))
                            {
                                break;   
                            }
                        }
                    }
                    else // PlayerSide.Top
                    {
                        for(int y = playerPiece.LogicPos.y - 1; y >= 0; y--)
                        {
                            // 候補に追加
                            destinationCandidates.Add(new Vector2Int(playerPiece.LogicPos.x, y));
                            // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                            if(IsCellOccupied(new Vector2Int(playerPiece.LogicPos.x, y), pieces, out var occupyingPiece))
                            {
                                break;
                            }
                        }
                    }
                    break;
                case PieceType.KEIMA:
                    // 桂馬は2マス前方に1マス左右
                    if(playerPiece.PlayerSide == PlayerSide.BOTTOM)
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
                    if(playerPiece.PlayerSide == PlayerSide.BOTTOM)
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
                    if(playerPiece.PlayerSide == PlayerSide.BOTTOM)
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
                    for(int  distance = 1; distance < BOARD_SIZE; distance++)
                    {
                        Vector2Int p0 = new Vector2Int(playerPiece.LogicPos.x + distance, playerPiece.LogicPos.y + distance);

                        // 候補に追加
                        destinationCandidates.Add(p0);
                        // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                        if(IsCellOccupied(p0, pieces, out var occupyingPiece))
                        {
                            break; 
                        }
                    }
                    // 右上
                    for(int  distance = 1; distance < BOARD_SIZE; distance++)
                    {
                        Vector2Int p1 = new Vector2Int(playerPiece.LogicPos.x - distance, playerPiece.LogicPos.y + distance);
                        
                        // 候補に追加
                        destinationCandidates.Add(p1);
                        // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                        if(IsCellOccupied(p1, pieces, out var occupyingPiece))
                        {
                            break;
                        }
                    }
                    // 左下
                    for(int  distance = 1; distance < BOARD_SIZE; distance++)
                    {
                        Vector2Int p2 = new Vector2Int(playerPiece.LogicPos.x + distance, playerPiece.LogicPos.y - distance);

                        // 候補に追加
                        destinationCandidates.Add(p2);
                        // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                        if(IsCellOccupied(p2, pieces, out var occupyingPiece))
                        {
                            break;
                        }
                    }
                    // 右下
                    for(int  distance = 1; distance < BOARD_SIZE; distance++)
                    {
                        Vector2Int p3 = new Vector2Int(playerPiece.LogicPos.x - distance, playerPiece.LogicPos.y - distance);

                        // 候補に追加
                        destinationCandidates.Add(p3);
                        // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                        if(IsCellOccupied(p3, pieces, out var occupyingPiece))
                        {
                            break;
                        }
                    }
                    break;
                case PieceType.HISHA:
                    // 飛車は縦横方向に何マスでも移動可能
                    // 縦横に追加していき、味方がいる場合はそのマスの前まで、相手がいる場合はそのマスまで追加して止める
                    // 上
                    for(int  distance = 1; distance < BOARD_SIZE; distance++)
                    {
                        Vector2Int p0 = new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y + distance);

                        // 候補に追加
                        destinationCandidates.Add(p0);
                        // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                        if(IsCellOccupied(p0, pieces, out var occupyingPiece))
                        {
                            break;
                        }
                    }
                    // 下
                    for(int  distance = 1; distance < BOARD_SIZE; distance++)
                    {
                        Vector2Int p1 = new Vector2Int(playerPiece.LogicPos.x, playerPiece.LogicPos.y - distance);

                        // 候補に追加
                        destinationCandidates.Add(p1);
                        // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                        if(IsCellOccupied(p1, pieces, out var occupyingPiece))
                        {
                            break;
                        }
                    }
                    // 左
                    for(int  distance = 1; distance < BOARD_SIZE; distance++)
                    {
                        Vector2Int p2 = new Vector2Int(playerPiece.LogicPos.x - distance, playerPiece.LogicPos.y);

                        // 候補に追加
                        destinationCandidates.Add(p2);
                        // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                        if(IsCellOccupied(p2, pieces, out var occupyingPiece))
                        {
                            break;
                        }
                    }
                    // 右
                    for(int  distance = 1; distance < BOARD_SIZE; distance++)
                    {
                        Vector2Int p3 = new Vector2Int(playerPiece.LogicPos.x + distance, playerPiece.LogicPos.y);

                        // 候補に追加
                        destinationCandidates.Add(p3);
                        // 駒があるならそのマスまで(味方駒の場合は後でまとめて取り除く)
                        if(IsCellOccupied(p3, pieces, out var occupyingPiece))
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
            if(playerPiece.IsPromoted)
            {
                if(actualPieceType == PieceType.KAKU ||
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
            destinationCandidates.RemoveAll(pos => pos.x < 0 || pos.x >= BOARD_SIZE || pos.y < 0 || pos.y >= BOARD_SIZE);

            // 味方の駒がいるマスの候補を削除
            destinationCandidates.RemoveAll(pos => IsCellOccupied(pos, pieces, out var occupyingPiece) && occupyingPiece.PlayerSide == playerPiece.PlayerSide);
        }

        Debug.Log($"候補座標: {string.Join(", ", destinationCandidates)}");

        return destinationCandidates.ToArray();
    }
}
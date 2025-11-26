using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 盤データ
    [SerializeField] private Vector3 _cellCenterPosition = new Vector3(0, 0, 0);
    [SerializeField] private float _cellSpacing = 0.2f;

    // 将棋駒のプレハブ配列
    // 0: 歩, 1: 香車, 2: 桂馬, 3: 銀将, 4: 金将, 5: 角行, 6: 飛車, 7: 王将, 8: 玉将
    [SerializeField] private GameObject[] _piecePrefabs;
    // 駒の親オブジェクト
    [SerializeField] private Transform _pieceParentTransform;

    // 駒の初期配置のテキストデータ
    // 歩: 0, 香車: 1, 桂馬: 2, 銀将: 3, 金将: 4, 角行: 5, 飛車: 6, 王将: 7, 玉将: 8
    // 下側: +0, 上側: +9
    // 空マス: -1
    [SerializeField] private TextAsset _initialPlacementTextAsset;
    
    private readonly int BOARD_SIZE = 9;

    void Start()
    {
        // 初期配置データの読み込み
        var initialPlacement = LoadInitialPlacement();

        // 将棋盤に駒を生成
        SpawnPieces(initialPlacement);

        // 
    }

    // 初期配置データを読み込む
    private int[,] LoadInitialPlacement()
    {
        // テキストデータを行ごとに分割して解析
        string[] lines = _initialPlacementTextAsset.text.Split('\n');
        int[,] initialPlacement = new int[BOARD_SIZE, BOARD_SIZE];
        for (int y = 0; y < BOARD_SIZE; y++)
        {
            string[] tokens = lines[y].Split(',');
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                // トークンの前後の空白を削除してから解析
                string token = tokens[x].Trim();
                initialPlacement[x, y] = int.Parse(token);
            }
        }
        return initialPlacement;
    }

    // 駒を初期配置データに基づいて生成する
    private void SpawnPieces(int[,] initialPlacement)
    {
        // 駒の生成ロジックをここに実装
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                int pieceCode = initialPlacement[x, y];
                if (pieceCode != -1)
                {
                    int pieceIndex = pieceCode % (int)PlayerSide.Top;
                    GameObject piecePrefab = _piecePrefabs[pieceIndex];
                    
                    Vector3 spawnPosition = new Vector3(
                        _cellCenterPosition.x + (x - BOARD_SIZE / 2) * _cellSpacing,
                        _cellCenterPosition.y,
                        _cellCenterPosition.z + (y - BOARD_SIZE / 2) * _cellSpacing
                    );
                    Quaternion spawnRotation = (pieceCode < 9) ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
                    Instantiate(piecePrefab, spawnPosition, spawnRotation);
                }
            }
        }
    }

}

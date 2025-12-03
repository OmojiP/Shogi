using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面の管理コンポーネント
/// </summary>
public class TitleManager : MonoBehaviour
{
    /// <summary>
    /// スタートボタン
    /// </summary>
    [SerializeField] private Button _startButton;

    void Start()
    {
        // スタートボタンが押されたときの処理を登録
        _startButton.onClick.AddListener(() =>
        {
            // インゲームシーンに遷移する
            UnityEngine.SceneManagement.SceneManager.LoadScene("Ingame");
        });
    }
}

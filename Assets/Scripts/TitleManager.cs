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
        _startButton.onClick.AddListener(() =>
        {
            // ゲーム開始処理
            UnityEngine.SceneManagement.SceneManager.LoadScene("Ingame");
        });
    }
}

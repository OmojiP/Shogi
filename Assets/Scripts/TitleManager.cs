using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
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

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// インゲームUIを管理するコンポーネント.
/// 成り確認UIや結果表示UIの管理を担当
/// </summary>
public class IngameUIManager : MonoBehaviour
{
    // 成るか確認するUI
    /// <summary>
    /// 成るか確認UIのパネルオブジェクト
    /// </summary>
    [SerializeField] private GameObject _promotionConfirmUI;
    /// <summary>
    /// 成るボタン
    /// </summary>
    [SerializeField] private Button _promotionYesButton;
    /// <summary>
    /// 成らないボタン
    /// </summary>
    [SerializeField] private Button _promotionNoButton;
    /// <summary>
    /// 成るかの確認をクリックした際にtrueにする(確認したらリセットする)
    /// </summary>
    private bool _isPromotionConfirmClicked;
    /// <summary>
    /// 成るかの確認のどちらをクリックしたか
    /// </summary>
    private bool _isPromotionConfirmYesClicked;

    // 結果画面UI
    /// <summary>
    /// 結果表示用UIのパネルオブジェクト
    /// </summary>
    [SerializeField] private GameObject _resultUI;
    /// <summary>
    /// 結果表示用テキスト
    /// </summary>
    [SerializeField] private TextMeshProUGUI _resultText;
    /// <summary>
    /// タイトルに戻るボタン
    /// </summary>
    [SerializeField] private Button _backToTitleButton;
    /// <summary>
    /// もう一度遊ぶボタン
    /// </summary>
    [SerializeField] private Button _restartButton;

    public void InitializeUI()
    {
        // 成り確認UIの設定
        // UIを非表示にする
        _promotionConfirmUI.SetActive(false);
        // ボタンに処理を登録
        _promotionYesButton.onClick.AddListener(() =>
        {
            // 成るボタンがクリックされたら、クリックフラグとYesフラグを立てる
            _isPromotionConfirmClicked = true;
            _isPromotionConfirmYesClicked = true;
        });
        _promotionNoButton.onClick.AddListener(() =>
        {
            // 成らないボタンがクリックされたら、クリックフラグとNoフラグを立てる
            _isPromotionConfirmClicked = true;
            _isPromotionConfirmYesClicked = false;
        });

        // 結果画面UIの設定
        // UIを非表示にする
        _resultUI.SetActive(false);
        _backToTitleButton.onClick.AddListener(() =>
        {
            // タイトルに戻るボタンがクリックされたら、タイトルシーンに遷移する
            SceneManager.LoadScene("Title");
        });
        _restartButton.onClick.AddListener(() =>
        {
            // もう一度ボタンがクリックされたら、インゲームシーンに遷移する
            SceneManager.LoadScene("Ingame");
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="winnerSide"></param>
    public void ShowResultUI(PlayerSide winnerSide)
    {
        _resultText.text = $"{winnerSide} win!";
        _resultUI.SetActive(true);
    }

    public IEnumerator ShowPromotionConfirmUI(Action<bool> callbackIsYesClicked)
    {
        // 成るか確認UIを表示
        _promotionConfirmUI.SetActive(true);
        // プレイヤーの入力待ち
        yield return new WaitUntil(() => _isPromotionConfirmClicked);
        // 入力の結果を受け取る
        bool isYesClicked = _isPromotionConfirmYesClicked;

        // フラグをリセット
        _isPromotionConfirmClicked = false;
        _isPromotionConfirmYesClicked = false;

        // 成るか確認UIを非表示にする
        _promotionConfirmUI.SetActive(false);

        // コールバックで結果を返す
        callbackIsYesClicked?.Invoke(isYesClicked);
    }
}

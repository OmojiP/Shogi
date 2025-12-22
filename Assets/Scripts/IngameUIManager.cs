using R3;
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
    // 他オブジェクトの参照
    /// <summary>
    /// ゲームの進行状態を監視してUIを変更するためのゲーム進行管理者の参照
    /// </summary>
    [SerializeField] GameManager _gameManager;

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

    private void Start()
    {
        // イベントを登録
        RegisterEvents();

        // UIの初期化処理を行う
        InitializeUI();
    }

    /// <summary>
    /// イベントを登録する
    /// </summary>
    private void RegisterEvents()
    {
        // 成り確認UIの設定
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
        // ボタンに処理を登録
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

        // GameManagerの登録
        // ゲームが終了したら結果UIを表示する
        _gameManager.OnEndedGame
            .Subscribe(winnerSide =>
            {
                // 結果UIを表示
                ShowResultUI(winnerSide);
            })
            .AddTo(this);
    }

    /// <summary>
    /// UIの初期化処理
    /// </summary>
    private void InitializeUI()
    {
        // 成り確認UIの設定
        // UIを非表示にする
        _promotionConfirmUI.SetActive(false);

        // 結果画面UIの設定
        // UIを非表示にする
        _resultUI.SetActive(false);
    }

    /// <summary>
    /// 結果表示UIを表示する処理
    /// </summary>
    /// <param name="winnerSide">勝者のプレイヤーサイド</param>
    private void ShowResultUI(PlayerSide winnerSide)
    {
        _resultText.text = $"{winnerSide} win!";
        _resultUI.SetActive(true);
    }

    /// <summary>
    /// 成るか確認UIを表示し、プレイヤーの入力を待つコルーチン
    /// </summary>
    /// <param name="callbackIsYesClicked">成るか確認の結果を受け取るコールバック</param>
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

using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// PlayerからUIに成るかの確認を投げる橋渡しクラス
/// </summary>
public class PromotionDecision : MonoBehaviour
{
    /// <summary>
    /// UIマネージャー(確認UIの表示に使用)
    /// </summary>
    [SerializeField] private IngameUIManager _ingameUIManager;

    /// <summary>
    /// 成るかどうかの決定を問い合わせる
    /// </summary>
    /// <param name="callbackIsYesSelected">問い合わせ結果を利用するコールバック</param>
    /// <returns></returns>
    public IEnumerator DecidePromotionAsync(Action<bool> callbackIsYesSelected)
    {
        // UIに問い合わせてコールバックに結果を受け取るまで待機する
        yield return _ingameUIManager.ShowPromotionConfirmUI(callbackIsYesSelected);
    }
}
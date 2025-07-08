using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// Đảm bảo file này không có namespace để các file khác truy cập NopeManager trực tiếp
public class NopeManager : MonoBehaviourPunCallbacks
{
    public static NopeManager Instance;

    // Trạng thái có thể đánh Nope
    public static bool IsCanPlayNope = false;
    // Không cho phép Nope khi exploding
    public static bool IsExploding = false;

    // Stack lưu các effect đang bị Nope (để xử lý Nope vào Nope)
    private Stack<NopeEffectData> nopeStack = new Stack<NopeEffectData>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    // Gọi khi bắt đầu 1 effect có thể bị Nope
    public void StartNopeWindow(string effectType, object effectData)
    {
        if (IsExploding) return;
        IsCanPlayNope = true;
        nopeStack.Push(new NopeEffectData(effectType, effectData));
    }

    // Gọi khi effect kết thúc hoặc có người rút bài
    public void EndNopeWindow()
    {
        IsCanPlayNope = false;
        nopeStack.Clear();
    }

    // Gọi khi có người chơi đánh Nope
    public void PlayNope(int playerId)
    {
        if (!IsCanPlayNope || IsExploding) return;
        photonView.RPC("RPC_ShowNopeEffect", RpcTarget.All, playerId);
        HandleNopeLogic(playerId);
    }

    // Xử lý logic Nope (bao gồm Nope vào Nope)
    private void HandleNopeLogic(int nopePlayerId)
    {
        if (nopeStack.Count == 0) return;
        var lastEffect = nopeStack.Peek();

        // Nếu Nope vào Nope chính mình
        if (lastEffect.effectType == "Nope" && lastEffect.nopePlayerId == nopePlayerId)
        {
            // Phục hồi effect trước đó
            nopeStack.Pop();
            if (nopeStack.Count > 0)
            {
                var prevEffect = nopeStack.Pop();
                ResumeEffect(prevEffect);
            }
            EndNopeWindow();
            return;
        }

        // Nếu Nope vào Nope của người khác
        if (lastEffect.effectType == "Nope" && lastEffect.nopePlayerId != nopePlayerId)
        {
            // Hủy Nope trước đó, phục hồi effect bị Nope
            nopeStack.Pop();
            if (nopeStack.Count > 0)
            {
                var prevEffect = nopeStack.Pop();
                ResumeEffect(prevEffect);
            }
            EndNopeWindow();
            return;
        }

        // Nope vào effect thường
        // Đẩy effect Nope vào stack
        nopeStack.Push(new NopeEffectData("Nope", null, nopePlayerId));
        // Vô hiệu hóa effect hiện tại
        CancelEffect(lastEffect);
        EndNopeWindow();
    }

    // Hiển thị hiệu ứng Nope cho tất cả
    [PunRPC]
    private void RPC_ShowNopeEffect(int playerId)
    {
        Debug.Log($"Player {playerId} played Nope! Show effect.");
        ShowNopePopup();
    }

    // Hiệu ứng pop-up Nope (Canvas tạm thời)
    private void ShowNopePopup()
    {
        GameObject tempNope = new GameObject("NopePopup");
        tempNope.transform.SetParent(transform);
        Canvas canvas = tempNope.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 2000;
        CanvasGroup cg = tempNope.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        UnityEngine.UI.Image img = tempNope.AddComponent<UnityEngine.UI.Image>();
        // Nếu có sprite Nope, gán vào đây, nếu không thì màu đỏ
        img.color = Color.red;
        RectTransform rect = tempNope.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 150);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        // Text
        GameObject textObj = new GameObject("NopeText");
        textObj.transform.SetParent(tempNope.transform);
        var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "NOPE!";
        text.fontSize = 60;
        text.color = Color.white;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(300, 150);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        // Tự hủy sau 0.5s
        Destroy(tempNope, 0.5f);
    }

    // Hủy effect hiện tại khi bị Nope
    private void CancelEffect(NopeEffectData effect)
    {
        Debug.Log($"Effect {effect.effectType} bị Nope!");
        switch (effect.effectType)
        {
            case "Skip":
                CardEffectManager.Instance?.CancelSkipEffect((int)effect.effectData);
                break;
            case "Attack":
                CardEffectManager.Instance?.CancelAttackEffect((int)effect.effectData);
                break;
            case "Favor":
                CardEffectManager.Instance?.CancelFavorEffect((int)effect.effectData);
                break;
            case "Combo":
                CardEffectManager.Instance?.CancelComboEffect(effect.effectData);
                break;
            case "Shuffle":
                CardEffectManager.Instance?.CancelShuffleEffect((int)effect.effectData);
                break;
            case "SeeTheFuture":
                CardEffectManager.Instance?.CancelSeeTheFutureEffect((int)effect.effectData);
                break;
            default:
                Debug.LogWarning($"Không xác định loại effect để Cancel: {effect.effectType}");
                break;
        }
    }

    // Phục hồi effect bị Nope nếu Nope vào Nope chính mình
    private void ResumeEffect(NopeEffectData effect)
    {
        Debug.Log($"Effect {effect.effectType} được phục hồi do Nope vào Nope chính mình!");
        switch (effect.effectType)
        {
            case "Skip":
                CardEffectManager.Instance?.ResumeSkipEffect((int)effect.effectData);
                break;
            case "Attack":
                CardEffectManager.Instance?.ResumeAttackEffect((int)effect.effectData);
                break;
            case "Favor":
                CardEffectManager.Instance?.ResumeFavorEffect((int)effect.effectData);
                break;
            case "Combo":
                CardEffectManager.Instance?.ResumeComboEffect(effect.effectData);
                break;
            case "Shuffle":
                CardEffectManager.Instance?.ResumeShuffleEffect((int)effect.effectData);
                break;
            case "SeeTheFuture":
                CardEffectManager.Instance?.ResumeSeeTheFutureEffect((int)effect.effectData);
                break;
            default:
                Debug.LogWarning($"Không xác định loại effect để Resume: {effect.effectType}");
                break;
        }
    }

    // Gọi khi có người rút bài (reset trạng thái Nope)
    public void OnPlayerDrawCard()
    {
        EndNopeWindow();
    }

    // Dữ liệu effect bị Nope
    private class NopeEffectData
    {
        public string effectType;
        public object effectData;
        public int nopePlayerId; // Nếu là Nope thì lưu ai là người Nope
        public NopeEffectData(string type, object data, int nopePlayerId = -1)
        {
            this.effectType = type;
            this.effectData = data;
            this.nopePlayerId = nopePlayerId;
        }
    }
} 
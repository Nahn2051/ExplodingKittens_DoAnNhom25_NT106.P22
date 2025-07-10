using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

public class NopeManager : MonoBehaviourPunCallbacks
{
    public static NopeManager Instance;
    
    // Nope System Variables
    public static bool IsCanPlayNope = false;
    private Stack<NopeEffectData> nopeStack = new Stack<NopeEffectData>();
    
    [Header("Nope UI")]
    [SerializeField] public GameObject nopePopupPanel;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    public void StartNopeWindow(string effectType, object effectData)
    {
        Debug.Log($"StartNopeWindow called for {effectType}");
        
        if (CardEffectManager.IsExplodingInProgress) 
        {
            Debug.Log("Cannot start Nope window - exploding in progress");
            return;
        }
        
        if (effectType == "Attack" || effectType == "Skip")
        {
            IsCanPlayNope = true;
            nopeStack.Push(new NopeEffectData(effectType, effectData));
            Debug.Log($"Nope window opened for {effectType} - remains open until someone draws a card");
            return;
        }
        
        IsCanPlayNope = true;
        nopeStack.Push(new NopeEffectData(effectType, effectData));
        Debug.Log($"Nope window opened for {effectType} - 5 seconds to play Nope");
    }

    public void EndNopeWindow()
    {
        Debug.Log("EndNopeWindow called");
        IsCanPlayNope = false;
        nopeStack.Clear();
    }
    
    public bool IsEffectNoped(string effectType, object effectData)
    {
        // Nếu stack rỗng, không có gì bị Nope
        if (nopeStack.Count == 0)
        {
            Debug.Log($"Effect {effectType} was NOT noped - empty stack");
            return false;
        }
        
        // Kiểm tra xem có effect với type này trong stack và effect cuối có phải là Nope không
        bool hasOriginalEffect = false;
        bool lastIsNope = false;
        
        foreach (var data in nopeStack)
        {
            if (data.effectType == effectType)
            {
                // So sánh data đơn giản
                if ((effectData == null && data.effectData == null) ||
                    (effectData != null && data.effectData != null && effectData.ToString() == data.effectData.ToString()))
                {
                    hasOriginalEffect = true;
                }
            }
        }
        
        // Kiểm tra effect cuối cùng
        var lastEffect = nopeStack.Peek();
        lastIsNope = (lastEffect.effectType == "Nope");
        
        if (hasOriginalEffect && lastIsNope)
        {
            Debug.Log($"Effect {effectType} was NOPED!");
            return true;
        }
        
        Debug.Log($"Effect {effectType} was NOT noped - hasOriginalEffect: {hasOriginalEffect}, lastIsNope: {lastIsNope}");
        return false;
    }
    
    public bool HasNopedEffects()
    {
        return nopeStack.Count > 0 && nopeStack.Peek().effectType == "Nope";
    }

    public void PlayNopeCard(int playerId)
    {
        if (!IsCanPlayNope || CardEffectManager.IsExplodingInProgress) 
        {
            Debug.Log("Cannot play Nope - window closed or exploding in progress");
            return;
        }
        
        Debug.Log($"Player {playerId} played Nope!");
        photonView.RPC("RPC_ShowNopeEffect", RpcTarget.All, playerId);
        HandleNopeLogic(playerId);
    }

    private void HandleNopeLogic(int nopePlayerId)
    {
        Debug.Log($"HandleNopeLogic: Player {nopePlayerId} played Nope, stack count: {nopeStack.Count}");
        
        if (nopeStack.Count == 0) 
        {
            Debug.LogWarning("HandleNopeLogic: No effects in stack to nope!");
            return;
        }
        
        var lastEffect = nopeStack.Peek();
        Debug.Log($"HandleNopeLogic: Last effect in stack: {lastEffect.effectType}");

        if (lastEffect.effectType == "Nope" && lastEffect.nopePlayerId == nopePlayerId)
        {
            Debug.Log("HandleNopeLogic: Nope-ing own Nope - restoring previous effect");
            nopeStack.Pop();
            if (nopeStack.Count > 0)
            {
                var prevEffect = nopeStack.Pop();
                ResumeEffect(prevEffect);
            }
            EndNopeWindow();
            return;
        }

        if (lastEffect.effectType == "Nope" && lastEffect.nopePlayerId != nopePlayerId)
        {
            Debug.Log("HandleNopeLogic: Nope-ing someone else's Nope - restoring original effect");
            nopeStack.Pop();
            if (nopeStack.Count > 0)
            {
                var prevEffect = nopeStack.Pop();
                ResumeEffect(prevEffect);
            }
            EndNopeWindow();
            return;
        }

        Debug.Log($"HandleNopeLogic: Nope-ing regular effect {lastEffect.effectType}");
        nopeStack.Push(new NopeEffectData("Nope", null, nopePlayerId));
        CancelEffect(lastEffect);
        EndNopeWindow();
    }

    [PunRPC]
    private void RPC_ShowNopeEffect(int playerId)
    {
        Debug.Log($"Player {playerId} played Nope! Show effect.");
        ShowNopePopup();
    }

    private void ShowNopePopup()
    {
        if (nopePopupPanel != null)
        {
            // Hiển thị panel Nope
            nopePopupPanel.SetActive(true);
            
            // Tự ẩn panel sau 0.5 giây
            StartCoroutine(HideNopePopupAfterDelay(0.5f));
        }
        else
        {
            Debug.LogWarning("NopePopupPanel chưa được gán trong Inspector!");
        }
    }
    
    private System.Collections.IEnumerator HideNopePopupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (nopePopupPanel != null)
        {
            nopePopupPanel.SetActive(false);
        }
    }

    // ==== CÁC HÀM BỔ SUNG ĐỂ XỬ LÝ EFFECT CANCELLATION/RESUMPTION ====
    
    public void CancelSkipEffect(int playerId)
    {
        Debug.Log($"Skip của player {playerId} bị Nope! (Skip đã kích hoạt - Nope không có tác dụng)");
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.HideEffect();
        }
    }
    
    public void ResumeSkipEffect(int playerId)
    {
        Debug.Log($"Skip của player {playerId} được phục hồi! (Skip đã kích hoạt - không cần phục hồi)");
    }
    
    public void CancelAttackEffect(int playerId)
    {
        Debug.Log($"Attack của player {playerId} bị Nope! (Attack đã kích hoạt - Nope không có tác dụng)");
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.HideEffect();
        }
    }
    
    public void ResumeAttackEffect(int playerId)
    {
        Debug.Log($"Attack của player {playerId} được phục hồi! (Attack đã kích hoạt - không cần phục hồi)");
    }
    
    public void CancelFavorEffect(int playerId)
    {
        Debug.Log($"Favor của player {playerId} bị Nope!");
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.HideEffect();
        }
        
        if (FavorTargetSelectUI.Instance != null)
        {
            FavorTargetSelectUI.Instance.gameObject.SetActive(false);
        }
    }
    
    public void ResumeFavorEffect(int playerId)
    {
        Debug.Log($"Favor của player {playerId} được phục hồi!");
    }
    
    public void CancelComboEffect(string comboKey)
    {
        Debug.Log($"Combo {comboKey} bị Nope!");
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.HideEffect();
            CardEffectManager.Instance.HideAllComboPanels();
        }
    }
    
    public void ResumeComboEffect(string comboKey)
    {
        Debug.Log($"Combo {comboKey} được phục hồi!");
    }
    
    public void CancelShuffleEffect(int playerId)
    {
        Debug.Log($"Shuffle của player {playerId} bị Nope!");
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.HideEffect();
        }
    }
    
    public void ResumeShuffleEffect(int playerId)
    {
        Debug.Log($"Shuffle của player {playerId} được phục hồi!");
    }
    
    public void CancelSeeTheFutureEffect(int playerId)
    {
        Debug.Log($"SeeTheFuture của player {playerId} bị Nope!");
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.HideEffect();
        }
    }
    
    public void ResumeSeeTheFutureEffect(int playerId)
    {
        Debug.Log($"SeeTheFuture của player {playerId} được phục hồi!");
    }

    // Coroutines để xử lý Nope timing cho các effect
    
    public void StartFavorNopeWindow(int playerId)
    {
        StartNopeWindow("Favor", playerId);
        StartCoroutine(ProcessFavorAfterNopeWindow(playerId));
    }
    
    public void StartShuffleNopeWindow(int playerId)
    {
        StartNopeWindow("Shuffle", playerId);
        StartCoroutine(ProcessShuffleAfterNopeWindow(playerId));
    }
    
    public void StartSeeTheFutureNopeWindow(int playerId)
    {
        StartNopeWindow("SeeTheFuture", playerId);
        StartCoroutine(ProcessSeeTheFutureAfterNopeWindow(playerId));
    }
    
    public void StartComboNopeWindow(List<Card> comboCards, string comboKey)
    {
        StartNopeWindow("Combo", comboKey);
        StartCoroutine(ProcessComboAfterNopeWindow(comboCards, comboKey));
    }
    
    private IEnumerator ProcessFavorAfterNopeWindow(int playerId)
    {
        // Chờ 5 giây để người chơi có thể dùng Nope
        yield return new WaitForSeconds(5f);
        
        // Kiểm tra xem effect có bị Nope hay không TRƯỚC khi kết thúc Nope window
        bool wasNoped = IsEffectNoped("Favor", playerId);
        EndNopeWindow();
        
        // CHỈ thực hiện effect nếu KHÔNG bị Nope
        if (!wasNoped)
        {
            // Ẩn effect text sau khi hết thời gian Nope
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.HideEffect();
            }
            
            // Thực hiện effect nếu không bị Nope và là người chơi đúng
            // Favor: Người chơi tiếp tục lượt sau khi nhận được bài
            if (PhotonNetwork.LocalPlayer.ActorNumber == playerId)
            {
                if (FavorTargetSelectUI.Instance != null)
                {
                    FavorTargetSelectUI.Instance.Show(
                        GameManager.Instance.playerList,
                        PhotonNetwork.LocalPlayer.ActorNumber,
                        (targetPlayerId) =>
                        {
                            Debug.Log("[Favor] Đã chọn người chơi có ID: " + targetPlayerId);
                            CardEffectManager.Instance.photonView.RPC("RPC_RequestFavorCard", RpcTarget.All, playerId, targetPlayerId);
                        }
                    );
                }
            }
        }
        else
        {
            Debug.Log("[Favor] Effect was NOPED - not executing");
            // Ẩn effect text ngay khi bị Nope
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.HideEffect();
            }
        }
        
        // Force reset UI state
        yield return new WaitForSeconds(0.2f);
        ForceResetUIState();
        
        Debug.Log("Favor process completed - UI should be interactive again");
    }
    
    private IEnumerator ProcessShuffleAfterNopeWindow(int playerId)
    {
        // Chờ 5 giây để người chơi có thể dùng Nope
        yield return new WaitForSeconds(5f);
        
        // Kiểm tra xem effect có bị Nope hay không TRƯỚC khi kết thúc Nope window
        bool wasNoped = IsEffectNoped("Shuffle", playerId);
        EndNopeWindow();
        
        // CHỈ thực hiện effect nếu KHÔNG bị Nope
        if (!wasNoped)
        {
            // Ẩn effect text sau khi hết thời gian Nope
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.HideEffect();
            }
            
            // Thực hiện effect nếu không bị Nope
            // Shuffle: Người chơi tiếp tục lượt sau khi shuffle
            CardManager.Instance.PhotonView.RPC("RPC_RequestShuffle", RpcTarget.MasterClient);
            Debug.Log("Shuffle completed - player continues their turn");
        }
        else
        {
            Debug.Log("[Shuffle] Effect was NOPED - not executing");
            // Ẩn effect text ngay khi bị Nope
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.HideEffect();
            }
        }
        
        // Force reset UI state
        yield return new WaitForSeconds(0.2f);
        ForceResetUIState();
        
        Debug.Log("Shuffle process completed - UI should be interactive again");
    }
    
    private IEnumerator ProcessSeeTheFutureAfterNopeWindow(int playerId)
    {
        // Chờ 5 giây để người chơi có thể dùng Nope
        yield return new WaitForSeconds(5f);
        
        // Kiểm tra xem effect có bị Nope hay không TRƯỚC khi kết thúc Nope window
        bool wasNoped = IsEffectNoped("SeeTheFuture", playerId);
        EndNopeWindow();
        Debug.Log("SeeTheFuture: Nope window ended");
        
        // CHỈ thực hiện effect nếu KHÔNG bị Nope
        if (!wasNoped)
        {
            // Ẩn effect text sau khi hết thời gian Nope
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.HideEffect();
            }
            
            // Thực hiện effect nếu không bị Nope
            if (CardManager.Instance != null)
            {
                Debug.Log("Processing SeeTheFuture effect - showing top 3 cards");
                // Request master client to show cards
                CardManager.Instance.PhotonView.RPC("RPC_RequestSeeTheFuture", RpcTarget.MasterClient, playerId);
                
                // SeeTheFuture KHÔNG kết thúc lượt - người chơi tiếp tục lượt và có thể chơi thêm bài hoặc rút bài
                // Lượt chỉ kết thúc khi người chơi rút bài (draw card)
                Debug.Log("SeeTheFuture completed - player continues their turn");
            }
        }
        else
        {
            Debug.Log("[SeeTheFuture] Effect was NOPED - not executing");
            // Ẩn effect text ngay khi bị Nope
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.HideEffect();
            }
        }
        
        // Chờ một chút để SeeTheFuture UI hiển thị hoàn thành
        yield return new WaitForSeconds(0.5f);
        
        // Force reset UI state để đảm bảo không bị đứng
        ForceResetUIState();
        
        // Đảm bảo UI hoàn toàn được reset và người chơi có thể tương tác tiếp
        yield return new WaitForSeconds(0.1f);
        Debug.Log("SeeTheFuture process completed - UI should be interactive again");
    }
    
    private IEnumerator ProcessComboAfterNopeWindow(List<Card> comboCards, string comboKey)
    {
        // Chờ 5 giây để người chơi có thể dùng Nope
        yield return new WaitForSeconds(5f);
        
        // Kiểm tra xem effect có bị Nope hay không TRƯỚC khi kết thúc Nope window
        bool wasNoped = IsEffectNoped("Combo", comboKey);
        EndNopeWindow();
        
        // CHỈ thực hiện effect nếu KHÔNG bị Nope
        if (!wasNoped)
        {
            // Ẩn effect text sau khi hết thời gian Nope
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.HideEffect();
            }
            
            // Thực hiện combo effect nếu không bị Nope
            // Combo: Người chơi tiếp tục lượt sau khi nhận được bài từ combo
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.ExecuteCombo(comboCards);
            }
            Debug.Log("Combo completed - player continues their turn");
        }
        else
        {
            Debug.Log("[Combo] Effect was NOPED - not executing");
            // Ẩn effect text ngay khi bị Nope
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.HideEffect();
            }
        }
        
        // Force reset UI state
        yield return new WaitForSeconds(0.2f);
        ForceResetUIState();
        
        Debug.Log("Combo process completed - UI should be interactive again");
    }

    // Method để force reset UI state khi có vấn đề UI bị đứng
    private void ForceResetUIState()
    {
        Debug.Log("NopeManager.ForceResetUIState: Resetting all UI states");
        
        // Reset Nope state
        EndNopeWindow();
        
        // Reset effect text
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.HideEffect();
        }
        
        // Đảm bảo tất cả UI panels được reset
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.HideAllComboPanels();
            CardEffectManager.Instance.HideExplodingPanels();
        }
        
        // Reset SeeTheFuture UI nếu có
        if (SeeTheFutureUI.Instance != null)
        {
            SeeTheFutureUI.Instance.gameObject.SetActive(false);
        }
        
        // Reset Favor UI nếu có
        if (FavorTargetSelectUI.Instance != null)
        {
            FavorTargetSelectUI.Instance.gameObject.SetActive(false);
        }
        
        // Đảm bảo NopeManager được reset
        IsCanPlayNope = false;
        
        Debug.Log("NopeManager.ForceResetUIState completed");
    }

    public void OnPlayerDrawCard()
    {
        Debug.Log("Someone drew a card - ending Nope window");
        EndNopeWindow();
    }

    private void CancelEffect(NopeEffectData effect)
    {
        Debug.Log($"Effect {effect.effectType} bị Nope!");
        
        switch (effect.effectType)
        {
            case "Skip":
                CancelSkipEffect((int)effect.effectData);
                break;
            case "Attack":
                CancelAttackEffect((int)effect.effectData);
                break;
            case "Favor":
                CancelFavorEffect((int)effect.effectData);
                break;
            case "Combo":
                CancelComboEffect((string)effect.effectData);
                break;
            case "Shuffle":
                CancelShuffleEffect((int)effect.effectData);
                break;
            case "SeeTheFuture":
                CancelSeeTheFutureEffect((int)effect.effectData);
                break;
            default:
                Debug.LogWarning($"Không xác định loại effect để Cancel: {effect.effectType}");
                break;
        }
    }

    private void ResumeEffect(NopeEffectData effect)
    {
        Debug.Log($"Effect {effect.effectType} được phục hồi do Nope vào Nope chính mình!");
        
        switch (effect.effectType)
        {
            case "Skip":
                ResumeSkipEffect((int)effect.effectData);
                break;
            case "Attack":
                ResumeAttackEffect((int)effect.effectData);
                break;
            case "Favor":
                ResumeFavorEffect((int)effect.effectData);
                break;
            case "Combo":
                ResumeComboEffect((string)effect.effectData);
                break;
            case "Shuffle":
                ResumeShuffleEffect((int)effect.effectData);
                break;
            case "SeeTheFuture":
                ResumeSeeTheFutureEffect((int)effect.effectData);
                break;
            default:
                Debug.LogWarning($"Không xác định loại effect để Resume: {effect.effectType}");
                break;
        }
    }

    [System.Serializable]
    public class NopeEffectData
    {
        public string effectType;
        public object effectData;
        public int nopePlayerId;
        
        public NopeEffectData(string type, object data, int nopePlayerId = -1)
        {
            this.effectType = type;
            this.effectData = data;
            this.nopePlayerId = nopePlayerId;
        }
    }
}

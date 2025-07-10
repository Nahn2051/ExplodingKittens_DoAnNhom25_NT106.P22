using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class NormalCardComboUI : MonoBehaviourPunCallbacks
{
    [Header("Normal Card Combo UI")]
    [SerializeField] private GameObject playerSelectionPanel;
    [SerializeField] private GameObject cardSelectionPanel;
    [SerializeField] private Transform playerButtonContainer;
    [SerializeField] private Transform cardButtonContainer;
    [SerializeField] private Button playerButtonPrefab;
    [SerializeField] private Button cardButtonPrefab;
    [SerializeField] private TMP_Text comboDescriptionText;
    
    // Biến cho Normal Card Combo
    private List<Card> pendingComboCards = new List<Card>();
    private int selectedTargetPlayer = -1;
    private string selectedCardType = "";
    
    // Events
    public System.Action<int, int> OnTwoCardComboExecuted;
    public System.Action<int, int, string> OnThreeCardComboExecuted;
    
    private void Start()
    {
        // Ẩn tất cả UI panels ban đầu
        if (playerSelectionPanel != null) 
        {
            playerSelectionPanel.SetActive(false);
            Debug.Log("NormalCardComboUI: playerSelectionPanel disabled");
        }
        if (cardSelectionPanel != null) 
        {
            cardSelectionPanel.SetActive(false);
            Debug.Log("NormalCardComboUI: cardSelectionPanel disabled");
        }
        
        // Đảm bảo text component không tự động resize
        if (comboDescriptionText != null)
        {
            comboDescriptionText.enableAutoSizing = false;
            Debug.Log("NormalCardComboUI: Auto-sizing disabled for combo description text");
        }
    }
    
    // Method to show helpful message when player tries to play normal card individually
    public void ShowComboHelpMessage()
    {
        if (comboDescriptionText != null)
        {
            comboDescriptionText.text = "Normal cards must be played in combos! Click 2-3 cards of the same type to select them for combo.";
            // Show message for a few seconds then hide
            StartCoroutine(HideHelpMessageAfterDelay(5f));
        }
    }
    
    // Method to show current combo selection status
    public void ShowComboSelectionStatus(int selectedCount, string cardType)
    {
        if (comboDescriptionText != null)
        {
            if (selectedCount == 0)
            {
                comboDescriptionText.text = "Click normal cards to select them for combo (2-3 cards of same type)";
            }
            else if (selectedCount == 1)
            {
                comboDescriptionText.text = $"Selected 1 {cardType} card. Select 1-2 more {cardType} cards to create combo.";
            }
            else if (selectedCount == 2)
            {
                comboDescriptionText.text = $"2-card combo ready! Auto-executing in 1 second or select 1 more {cardType} for 3-card combo.";
            }
            else if (selectedCount == 3)
            {
                comboDescriptionText.text = $"3-card combo ready! Auto-executing in 1 second.";
            }
        }
    }
    
    private IEnumerator HideHelpMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (comboDescriptionText != null && pendingComboCards.Count == 0)
        {
            comboDescriptionText.text = "";
        }
    }
    
    // Public method để xử lý combo normal cards
    public void HandleNormalCardCombo(List<Card> comboCards)
    {
        Debug.Log($"NormalCardComboUI.HandleNormalCardCombo: Received combo with {comboCards.Count} cards");
        
        if (comboCards.Count < 2 || comboCards.Count > 3)
        {
            Debug.LogWarning("Combo must have 2 or 3 cards!");
            return;
        }
        
        // Kiểm tra tất cả các lá bài có cùng loại không
        string cardType = comboCards[0].data.effect;
        Debug.Log($"NormalCardComboUI: Checking combo of type {cardType}");
        
        foreach (Card card in comboCards)
        {
            if (card.data.effect != cardType)
            {
                Debug.LogWarning($"All cards in combo must be of the same type! Found {card.data.effect} in {cardType} combo");
                return;
            }
        }
        
        // Kiểm tra có phải normal card không
        if (!IsNormalCard(cardType))
        {
            Debug.LogWarning($"Only Normal cards can create combos! {cardType} is not a normal card");
            return;
        }
        
        // Lưu combo để xử lý
        pendingComboCards = new List<Card>(comboCards);
        Debug.Log($"NormalCardComboUI: Stored {pendingComboCards.Count} cards for {cardType} combo");
        
        if (comboCards.Count == 2)
        {
            // Combo 2 lá: không cần delay vì không còn Nope
            Debug.Log($"NormalCardComboUI: Starting 2-card combo process for {cardType}");
            StartTwoCardCombo(cardType); // Thực hiện ngay lập tức
        }
        else if (comboCards.Count == 3)
        {
            // Combo 3 lá: không cần delay vì không còn Nope
            Debug.Log($"NormalCardComboUI: Starting 3-card combo process for {cardType}");
            StartThreeCardCombo(cardType); // Thực hiện ngay lập tức
        }
    }
    
    private IEnumerator DelayedTwoCardCombo(string cardType, float delay)
    {
        Debug.Log($"NormalCardComboUI: Starting 2-card combo delay ({delay}s) for {cardType}");
        yield return new WaitForSeconds(delay);
        
        // Kiểm tra xem combo có bị Nope không
        if (pendingComboCards.Count > 0) // Nếu combo chưa bị reset bởi Nope
        {
            Debug.Log($"NormalCardComboUI: Delay completed, showing 2-card combo UI for {cardType}");
            StartTwoCardCombo(cardType);
        }
        else
        {
            Debug.Log("NormalCardComboUI: 2-card combo was cancelled (likely by Nope)");
        }
    }
    
    private IEnumerator DelayedThreeCardCombo(string cardType, float delay)
    {
        Debug.Log($"NormalCardComboUI: Starting 3-card combo delay ({delay}s) for {cardType}");
        yield return new WaitForSeconds(delay);
        
        // Kiểm tra xem combo có bị Nope không
        if (pendingComboCards.Count > 0) // Nếu combo chưa bị reset bởi Nope
        {
            Debug.Log($"NormalCardComboUI: Delay completed, showing 3-card combo UI for {cardType}");
            StartThreeCardCombo(cardType);
        }
        else
        {
            Debug.Log("NormalCardComboUI: 3-card combo was cancelled (likely by Nope)");
        }
    }
    
    private bool IsNormalCard(string cardType)
    {
        return cardType == "HairyPotatoCat" || cardType == "BeardCat" || 
               cardType == "Cattermelon" || cardType == "Tacocat" || 
               cardType == "RainbowRalphingCat";
    }
    
    private void StartTwoCardCombo(string cardType)
    {
        if (comboDescriptionText != null)
        {
            comboDescriptionText.text = $"2-Card Combo ({cardType}): Select a player to steal 1 random card";
            // Đảm bảo text không bị tự động resize
            comboDescriptionText.enableAutoSizing = false;
        }
        
        ShowPlayerSelection();
    }
    
    private void StartThreeCardCombo(string cardType)
    {
        if (comboDescriptionText != null)
        {
            comboDescriptionText.text = $"3-Card Combo ({cardType}): Select a player to steal a specific card";
            // Đảm bảo text không bị tự động resize
            comboDescriptionText.enableAutoSizing = false;
        }
        
        ShowPlayerSelection();
    }
    
    // Coroutine to ensure UI interactions are properly restored after combo setup
    private IEnumerator EnsureUIInteractionsAfterCombo()
    {
        yield return new WaitForSeconds(0.2f);
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ForceEnableAllUIInteractions();
            Debug.Log("NormalCardComboUI: Forced ALL UI interaction restoration after combo setup");
        }
        
        // Additional wait and second attempt if needed
        yield return new WaitForSeconds(0.3f);
        
        // Force enable all buttons in active panels again
        if (playerSelectionPanel != null && playerSelectionPanel.activeInHierarchy)
        {
            Button[] buttons = playerSelectionPanel.GetComponentsInChildren<Button>();
            foreach (Button btn in buttons)
            {
                btn.interactable = true;
            }
            Debug.Log($"NormalCardComboUI: Force enabled {buttons.Length} player selection buttons (second pass)");
        }
        
        if (cardSelectionPanel != null && cardSelectionPanel.activeInHierarchy)
        {
            Button[] buttons = cardSelectionPanel.GetComponentsInChildren<Button>();
            foreach (Button btn in buttons)
            {
                btn.interactable = true;
            }
            Debug.Log($"NormalCardComboUI: Force enabled {buttons.Length} card selection buttons (second pass)");
        }
    }
    
    private void ShowPlayerSelection()
    {
        Debug.Log("NormalCardComboUI.ShowPlayerSelection: About to show player selection panel");
        
        if (playerSelectionPanel != null)
        {
            playerSelectionPanel.SetActive(true);
            playerSelectionPanel.transform.SetAsLastSibling(); // Ensure panel is shown on top
            Debug.Log("NormalCardComboUI: playerSelectionPanel activated and brought to front");
        }
        else
        {
            Debug.LogWarning("NormalCardComboUI: playerSelectionPanel is null!");
        }
        
        // Ensure UI interactions are enabled for the combo panel
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnableUIInteractionsOnly();
            Debug.Log("NormalCardComboUI: UI interactions enabled for player selection");
        }
        
        // Xóa các button cũ
        foreach (Transform child in playerButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Tạo button cho mỗi người chơi (trừ local player)
        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                Button playerBtn = Instantiate(playerButtonPrefab, playerButtonContainer);
                playerBtn.GetComponentInChildren<TMP_Text>().text = player.NickName;
                int playerId = player.ActorNumber;
                playerBtn.onClick.AddListener(() => OnPlayerSelected(playerId));
                
                // Explicitly ensure the button is interactable
                playerBtn.interactable = true;
                Debug.Log($"NormalCardComboUI: Created interactable button for player {player.NickName}");
            }
        }
        
        Debug.Log("NormalCardComboUI: Player selection setup completed with UI interactions enabled");
        
        // Immediate UI force enable
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ForceEnableAllUIInteractions();
            Debug.Log("NormalCardComboUI: Immediate force enable of all UI interactions");
        }
        
        // Start coroutine to ensure UI interactions work properly
        StartCoroutine(EnsureUIInteractionsAfterCombo());
    }
    
    private void OnPlayerSelected(int playerId)
    {
        Debug.Log($"NormalCardComboUI.OnPlayerSelected: Player {playerId} selected for combo");
        selectedTargetPlayer = playerId;
        
        if (pendingComboCards.Count == 2)
        {
            // Combo 2 lá: thực hiện ngay lập tức
            Debug.Log("NormalCardComboUI: Executing 2-card combo immediately");
            ExecuteTwoCardCombo();
        }
        else if (pendingComboCards.Count == 3)
        {
            // Combo 3 lá: hiển thị menu chọn loại bài
            Debug.Log("NormalCardComboUI: Showing card type selection for 3-card combo");
            ShowCardTypeSelection();
        }
    }
    
    private void ShowCardTypeSelection()
    {
        if (playerSelectionPanel != null)
        {
            playerSelectionPanel.SetActive(false);
        }
        
        if (cardSelectionPanel != null)
        {
            cardSelectionPanel.SetActive(true);
            cardSelectionPanel.transform.SetAsLastSibling(); // Ensure panel is shown on top
            Debug.Log("NormalCardComboUI: cardSelectionPanel activated and brought to front");
        }
        else
        {
            Debug.LogWarning("NormalCardComboUI: cardSelectionPanel is null!");
        }
        
        // Ensure UI interactions are enabled for the card selection panel
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnableUIInteractionsOnly();
            Debug.Log("NormalCardComboUI: UI interactions enabled for card type selection");
        }
        
        // Xóa các button cũ
        foreach (Transform child in cardButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Danh sách các loại bài có thể chọn (đã loại bỏ Nope vì đã xóa khỏi game)
        string[] cardTypes = {"Favor", "Shuffle", "Skip", "SeeTheFuture", "Defuse",
                              "HairyPotatoCat", "BeardCat", "Cattermelon", "Tacocat", "RainbowRalphingCat"};
        
        foreach (string cardType in cardTypes)
        {
            Button cardBtn = Instantiate(cardButtonPrefab, cardButtonContainer);
            cardBtn.GetComponentInChildren<TMP_Text>().text = cardType;
            string type = cardType; // Capture for lambda
            cardBtn.onClick.AddListener(() => OnCardTypeSelected(type));
            
            // Explicitly ensure the button is interactable
            cardBtn.interactable = true;
            Debug.Log($"NormalCardComboUI: Created interactable card type button for {cardType}");
        }
        
        Debug.Log("NormalCardComboUI: Card type selection setup completed with UI interactions enabled");
        
        // Start coroutine to ensure UI interactions work properly
        StartCoroutine(EnsureUIInteractionsAfterCombo());
    }
    
    private void OnCardTypeSelected(string cardType)
    {
        Debug.Log($"NormalCardComboUI.OnCardTypeSelected: Card type {cardType} selected for 3-card combo");
        selectedCardType = cardType;
        
        // Automatically execute three card combo after selection
        ExecuteThreeCardCombo();
    }
    
    private void ResetComboUI()
    {
        // Reset và ẩn UI
        pendingComboCards.Clear();
        selectedTargetPlayer = -1;
        selectedCardType = "";
        
        if (playerSelectionPanel != null)
            playerSelectionPanel.SetActive(false);
        if (cardSelectionPanel != null)
            cardSelectionPanel.SetActive(false);
    }
    
    private void ExecuteTwoCardCombo()
    {
        Debug.Log($"NormalCardComboUI.ExecuteTwoCardCombo: Executing 2-card combo for player {PhotonNetwork.LocalPlayer.ActorNumber} targeting player {selectedTargetPlayer}");
        
        // Trigger event cho CardEffectManager
        OnTwoCardComboExecuted?.Invoke(PhotonNetwork.LocalPlayer.ActorNumber, selectedTargetPlayer);
        
        // Reset UI
        ResetComboUI();
        
        // Don't restore UI here - it will be done after the RPC completes
        Debug.Log("NormalCardComboUI: 2-card combo triggered, UI will be restored after RPC completion");
    }
    
    private void ExecuteThreeCardCombo()
    {
        Debug.Log($"NormalCardComboUI.ExecuteThreeCardCombo: Executing 3-card combo for player {PhotonNetwork.LocalPlayer.ActorNumber} targeting player {selectedTargetPlayer} for card type {selectedCardType}");
        
        // Trigger event cho CardEffectManager
        OnThreeCardComboExecuted?.Invoke(PhotonNetwork.LocalPlayer.ActorNumber, selectedTargetPlayer, selectedCardType);
        
        // Reset UI
        ResetComboUI();
        
        // Don't restore UI here - it will be done after the RPC completes
        Debug.Log("NormalCardComboUI: 3-card combo triggered, UI will be restored after RPC completion");
    }
    
    // Method to cancel pending combo (called when Nope is used)
    public void CancelPendingCombo()
    {
        Debug.Log("NormalCardComboUI: Pending combo cancelled by Nope");
        StopAllCoroutines(); // Stop any delayed combo coroutines
        ResetComboUI();
    }
    
    // Method to check if there's a pending combo
    public bool HasPendingCombo()
    {
        return pendingComboCards.Count > 0;
    }
    
    public void HideAllPanels()
    {
        ResetComboUI();
    }
}

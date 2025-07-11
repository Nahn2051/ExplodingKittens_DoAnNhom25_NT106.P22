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
    
    // Biến lưu trữ combo đang xử lý
    private List<Card> pendingComboCards = new List<Card>();
    private int selectedTargetPlayer = -1;
    private string selectedCardType = "";
    
    // Events để thông báo cho CardEffectManager
    public System.Action<int, int> OnTwoCardComboExecuted;
    public System.Action<int, int, string> OnThreeCardComboExecuted;
    
    private void Start()
    {
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
        
        if (comboDescriptionText != null)
        {
            comboDescriptionText.enableAutoSizing = false;
            Debug.Log("NormalCardComboUI: Auto-sizing disabled for combo description text");
        }
    }
    
    public void ShowComboHelpMessage()
    {
        if (comboDescriptionText != null)
        {
            comboDescriptionText.text = "Normal cards must be played in combos! Click 2-3 cards of the same type to select them for combo.";
            StartCoroutine(HideHelpMessageAfterDelay(5f));
        }
    }
    
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
    
    public void HandleNormalCardCombo(List<Card> comboCards)
    {
        Debug.Log($"NormalCardComboUI.HandleNormalCardCombo: Received combo with {comboCards.Count} cards");
        
        if (comboCards.Count < 2 || comboCards.Count > 3)
        {
            Debug.LogWarning("Combo must have 2 or 3 cards!");
            return;
        }
        
        // Kiểm tra tất cả cards cùng loại
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
        
        if (!IsNormalCard(cardType))
        {
            Debug.LogWarning($"Only Normal cards can create combos! {cardType} is not a normal card");
            return;
        }
        
        pendingComboCards = new List<Card>(comboCards);
        Debug.Log($"NormalCardComboUI: Stored {pendingComboCards.Count} cards for {cardType} combo");
        
        if (comboCards.Count == 2)
        {
            Debug.Log($"NormalCardComboUI: Starting 2-card combo process for {cardType}");
            StartTwoCardCombo(cardType);
        }
        else if (comboCards.Count == 3)
        {
            Debug.Log($"NormalCardComboUI: Starting 3-card combo process for {cardType}");
            StartThreeCardCombo(cardType);
        }
    }
    
    private IEnumerator DelayedTwoCardCombo(string cardType, float delay)
    {
        Debug.Log($"NormalCardComboUI: Starting 2-card combo delay ({delay}s) for {cardType}");
        yield return new WaitForSeconds(delay);
        
        if (pendingComboCards.Count > 0)
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
        
        if (pendingComboCards.Count > 0)
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
            comboDescriptionText.enableAutoSizing = false;
        }
        
        ShowPlayerSelection();
    }
    
    private void StartThreeCardCombo(string cardType)
    {
        if (comboDescriptionText != null)
        {
            comboDescriptionText.text = $"3-Card Combo ({cardType}): Select a player to steal a specific card";
            comboDescriptionText.enableAutoSizing = false;
        }
        
        ShowPlayerSelection();
    }
    
    private IEnumerator EnsureUIInteractionsAfterCombo()
    {
        yield return new WaitForSeconds(0.2f);
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ForceEnableAllUIInteractions();
            Debug.Log("NormalCardComboUI: Forced ALL UI interaction restoration after combo setup");
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // Force enable buttons trong active panels
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
            playerSelectionPanel.transform.SetAsLastSibling();
            Debug.Log("NormalCardComboUI: playerSelectionPanel activated and brought to front");
        }
        else
        {
            Debug.LogWarning("NormalCardComboUI: playerSelectionPanel is null!");
        }
        
        // Enable UI interactions cho combo panel
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnableUIInteractionsOnly();
            Debug.Log("NormalCardComboUI: UI interactions enabled for player selection");
        }
        
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
                
                playerBtn.interactable = true;
                Debug.Log($"NormalCardComboUI: Created interactable button for player {player.NickName}");
            }
        }
        
        Debug.Log("NormalCardComboUI: Player selection setup completed with UI interactions enabled");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ForceEnableAllUIInteractions();
            Debug.Log("NormalCardComboUI: Immediate force enable of all UI interactions");
        }
        
        StartCoroutine(EnsureUIInteractionsAfterCombo());
    }
    
    private void OnPlayerSelected(int playerId)
    {
        Debug.Log($"NormalCardComboUI.OnPlayerSelected: Player {playerId} selected for combo");
        selectedTargetPlayer = playerId;
        
        if (pendingComboCards.Count == 2)
        {
            Debug.Log("NormalCardComboUI: Executing 2-card combo immediately");
            ExecuteTwoCardCombo();
        }
        else if (pendingComboCards.Count == 3)
        {
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
            cardSelectionPanel.transform.SetAsLastSibling();
            Debug.Log("NormalCardComboUI: cardSelectionPanel activated and brought to front");
        }
        else
        {
            Debug.LogWarning("NormalCardComboUI: cardSelectionPanel is null!");
        }
        
        // Enable UI interactions cho card selection panel
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnableUIInteractionsOnly();
            Debug.Log("NormalCardComboUI: UI interactions enabled for card type selection");
        }
        
        foreach (Transform child in cardButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Danh sách card types có thể chọn
        string[] cardTypes = {"Favor", "Shuffle", "Skip", "SeeTheFuture", "Defuse",
                              "HairyPotatoCat", "BeardCat", "Cattermelon", "Tacocat", "RainbowRalphingCat"};
        
        foreach (string cardType in cardTypes)
        {
            Button cardBtn = Instantiate(cardButtonPrefab, cardButtonContainer);
            cardBtn.GetComponentInChildren<TMP_Text>().text = cardType;
            string type = cardType;
            cardBtn.onClick.AddListener(() => OnCardTypeSelected(type));
            
            cardBtn.interactable = true;
            Debug.Log($"NormalCardComboUI: Created interactable card type button for {cardType}");
        }
        
        Debug.Log("NormalCardComboUI: Card type selection setup completed with UI interactions enabled");
        
        StartCoroutine(EnsureUIInteractionsAfterCombo());
    }
    
    private void OnCardTypeSelected(string cardType)
    {
        Debug.Log($"NormalCardComboUI.OnCardTypeSelected: Card type {cardType} selected for 3-card combo");
        selectedCardType = cardType;
        
        ExecuteThreeCardCombo();
    }
    
    private void ResetComboUI()
    {
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
        
        OnTwoCardComboExecuted?.Invoke(PhotonNetwork.LocalPlayer.ActorNumber, selectedTargetPlayer);
        
        ResetComboUI();
        
        Debug.Log("NormalCardComboUI: 2-card combo triggered, UI will be restored after RPC completion");
    }
    
    private void ExecuteThreeCardCombo()
    {
        Debug.Log($"NormalCardComboUI.ExecuteThreeCardCombo: Executing 3-card combo for player {PhotonNetwork.LocalPlayer.ActorNumber} targeting player {selectedTargetPlayer} for card type {selectedCardType}");
        
        OnThreeCardComboExecuted?.Invoke(PhotonNetwork.LocalPlayer.ActorNumber, selectedTargetPlayer, selectedCardType);
        
        ResetComboUI();
        
        Debug.Log("NormalCardComboUI: 3-card combo triggered, UI will be restored after RPC completion");
    }
    
    public void CancelPendingCombo()
    {
        Debug.Log("NormalCardComboUI: Pending combo cancelled by Nope");
        StopAllCoroutines();
        ResetComboUI();
    }
    
    public bool HasPendingCombo()
    {
        return pendingComboCards.Count > 0;
    }
    
    public void HideAllPanels()
    {
        ResetComboUI();
    }
}

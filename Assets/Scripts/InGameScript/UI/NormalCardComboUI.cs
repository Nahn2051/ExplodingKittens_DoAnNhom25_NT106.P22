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
    }
    
    // Public method để xử lý combo normal cards
    public void HandleNormalCardCombo(List<Card> comboCards)
    {
        if (comboCards.Count < 2 || comboCards.Count > 3)
        {
            Debug.LogWarning("Combo must have 2 or 3 cards!");
            return;
        }
        
        // Kiểm tra tất cả các lá bài có cùng loại không
        string cardType = comboCards[0].data.effect;
        foreach (Card card in comboCards)
        {
            if (card.data.effect != cardType)
            {
                Debug.LogWarning("All cards in combo must be of the same type!");
                return;
            }
        }
        
        // Kiểm tra có phải normal card không
        if (!IsNormalCard(cardType))
        {
            Debug.LogWarning("Only Normal cards can create combos!");
            return;
        }
        
        // Lưu combo để xử lý
        pendingComboCards = new List<Card>(comboCards);
        
        if (comboCards.Count == 2)
        {
            // Combo 2 lá: chọn người chơi để lấy 1 lá bài ngẫu nhiên
            StartTwoCardCombo(cardType);
        }
        else if (comboCards.Count == 3)
        {
            // Combo 3 lá: chọn người chơi và chọn loại bài cụ thể
            StartThreeCardCombo(cardType);
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
        }
        
        ShowPlayerSelection();
    }
    
    private void StartThreeCardCombo(string cardType)
    {
        if (comboDescriptionText != null)
        {
            comboDescriptionText.text = $"3-Card Combo ({cardType}): Select a player to steal a specific card";
        }
        
        ShowPlayerSelection();
    }
    
    private void ShowPlayerSelection()
    {
        Debug.Log("NormalCardComboUI.ShowPlayerSelection: About to show player selection panel");
        
        if (playerSelectionPanel != null)
        {
            playerSelectionPanel.SetActive(true);
            Debug.Log("NormalCardComboUI: playerSelectionPanel activated - this might block UI interaction!");
        }
        else
        {
            Debug.LogWarning("NormalCardComboUI: playerSelectionPanel is null!");
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
            }
        }
    }
    
    private void OnPlayerSelected(int playerId)
    {
        selectedTargetPlayer = playerId;
        
        if (pendingComboCards.Count == 2)
        {
            // Combo 2 lá: thực hiện ngay lập tức
            ExecuteTwoCardCombo();
        }
        else if (pendingComboCards.Count == 3)
        {
            // Combo 3 lá: hiển thị menu chọn loại bài
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
        }
        
        // Xóa các button cũ
        foreach (Transform child in cardButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Danh sách các loại bài có thể chọn
        string[] cardTypes = {"Favor", "Nope", "Shuffle", "Skip", "SeeTheFuture", "Defuse",
                              "HairyPotatoCat", "BeardCat", "Cattermelon", "Tacocat", "RainbowRalphingCat"};
        
        foreach (string cardType in cardTypes)
        {
            Button cardBtn = Instantiate(cardButtonPrefab, cardButtonContainer);
            cardBtn.GetComponentInChildren<TMP_Text>().text = cardType;
            string type = cardType; // Capture for lambda
            cardBtn.onClick.AddListener(() => OnCardTypeSelected(type));
        }
    }
    
    private void OnCardTypeSelected(string cardType)
    {
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
        // Trigger event cho CardEffectManager
        OnTwoCardComboExecuted?.Invoke(PhotonNetwork.LocalPlayer.ActorNumber, selectedTargetPlayer);
        
        // Reset UI
        ResetComboUI();
    }
    
    private void ExecuteThreeCardCombo()
    {
        // Trigger event cho CardEffectManager
        OnThreeCardComboExecuted?.Invoke(PhotonNetwork.LocalPlayer.ActorNumber, selectedTargetPlayer, selectedCardType);
        
        // Reset UI
        ResetComboUI();
    }
    
    public void HideAllPanels()
    {
        ResetComboUI();
    }
}

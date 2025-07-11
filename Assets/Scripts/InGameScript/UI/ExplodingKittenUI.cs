using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System;

public class ExplodingKittenUI : MonoBehaviour
{
    [Header("Exploding Kitten UI")]
    [SerializeField] private GameObject explodingKittenPanel;
    [SerializeField] private Image explodingKittenCardImage;
    [SerializeField] private GameObject defuseZone;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private GameObject positionInputPanel;
    [SerializeField] private TMP_InputField positionInputField;
    [SerializeField] private Button confirmPositionButton;
    
    private bool hasDefuseInZone = false;
    private Coroutine countdownCoroutine;
    
    // Events
    public event Action<int> OnDefuseConfirmed;
    public event Action OnPlayerEliminated;
    public event Action<Card> OnDefuseCardDropped;
    
    private void Start()
    {
        // Ẩn UI panels ban đầu
        if (explodingKittenPanel != null) explodingKittenPanel.SetActive(false);
        if (positionInputPanel != null) positionInputPanel.SetActive(false);
        
        // Thiết lập button events
        if (confirmPositionButton != null)
            confirmPositionButton.onClick.AddListener(OnConfirmPositionClicked);
    }
    
    public void StartExplodingKittenSequence()
    {
        Debug.Log("Starting exploding kitten sequence!");
        
        // Reset trạng thái trước khi bắt đầu
        hasDefuseInZone = false;
        
        // Dừng mọi countdown đang chạy trước đó
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            Debug.Log("Stopped previous countdown before starting new sequence");
        }
        
        // Hiển thị panel exploding kitten
        if (explodingKittenPanel != null)
        {
            explodingKittenPanel.SetActive(true);
            Debug.Log("Exploding kitten panel activated");
            
            // Hiển thị hình ảnh lá bài exploding
            if (explodingKittenCardImage != null && CardManager.Instance != null)
            {
                // Tìm sprite của exploding kitten trong CardManager
                for (int i = 0; i < CardManager.Instance.allCardSprites.Length; i++)
                {
                    if (CardManager.Instance.allCardSprites[i].name.Contains("Exploding"))
                    {
                        explodingKittenCardImage.sprite = CardManager.Instance.allCardSprites[i];
                        Debug.Log("Exploding card image set");
                        break;
                    }
                }
            }
        }
        else
        {
            Debug.LogError("explodingKittenPanel is null!");
        }
        
        // Hiển thị defuse zone
        if (defuseZone != null)
        {
            defuseZone.SetActive(true);
            Debug.Log("Defuse zone activated");
        }
        else
        {
            Debug.LogError("defuseZone is null!");
        }
        
        // Bắt đầu countdown 10 giây
        countdownCoroutine = StartCoroutine(CountdownTimer(10f));
        Debug.Log("Started new 10-second countdown");
    }
    
    private IEnumerator CountdownTimer(float duration)
    {
        float timeRemaining = duration;
        
        while (timeRemaining > 0)
        {
            // Check if defuse was used during countdown
            if (hasDefuseInZone)
            {
                Debug.Log("Defuse detected during countdown, stopping timer");
                yield break; // Exit countdown if defuse was used
            }
            
            if (countdownText != null)
                countdownText.text = $"{timeRemaining:F1}s";
                
            yield return new WaitForSeconds(0.1f);
            timeRemaining -= 0.1f;
        }
        
        // Kiểm tra lại hasDefuseInZone trước khi eliminate - đây là double check cuối cùng
        if (hasDefuseInZone)
        {
            Debug.Log("[ExplodingKittenUI] Defuse was used during final check, player survives");
            yield break;
        }
        
        // Hết thời gian
        if (countdownText != null)
            countdownText.text = "0s";
            
        Debug.Log($"[ExplodingKittenUI] Countdown finished. hasDefuseInZone: {hasDefuseInZone}");
        
        // Kiểm tra có defuse không - chỉ eliminate nếu thực sự không có defuse
        if (!hasDefuseInZone)
        {
            // Không có defuse -> player bị loại
            Debug.Log("[ExplodingKittenUI] No defuse provided, eliminating player");
            
            // Hide the exploding panel first
            if (explodingKittenPanel != null)
                explodingKittenPanel.SetActive(false);
            
            // Trigger elimination
            OnPlayerEliminated?.Invoke();
            
            // Start a backup elimination timer in case the primary elimination fails
            StartCoroutine(BackupEliminationCheck());
        }
        else
        {
            Debug.Log("[ExplodingKittenUI] Defuse was provided, player survives");
        }
    }
    
    private IEnumerator BackupEliminationCheck()
    {
        // Wait 2 seconds to see if elimination was processed
        yield return new WaitForSeconds(2f);
        
        // If we're still in exploding state, force elimination again
        if (CardEffectManager.IsExplodingInProgress)
        {
            Debug.LogWarning("[ExplodingKittenUI] Backup elimination check: Still in exploding state, forcing elimination again");
            OnPlayerEliminated?.Invoke();
        }
    }
    
    public void HandleDefuseCardDropped(Card defuseCard)
    {
        // Kiểm tra nếu card được thả vào defuse zone
        if (defuseCard.data.effect == "Defuse")
        {
            Debug.Log($"[ExplodingKittenUI] Defuse card {defuseCard.data.cardName} dropped in zone");
            
            // Đặt hasDefuseInZone TRƯỚC KHI dừng countdown để tránh race condition
            hasDefuseInZone = true;
            
            // Dừng countdown ngay lập tức
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
                Debug.Log("[ExplodingKittenUI] Countdown stopped - defuse successful");
            }
            
            // Double-check để đảm bảo card được xóa khỏi CardHolder (fallback protection)
            if (CardManager.Instance != null && CardManager.Instance.cardHolder != null)
            {
                // Kiểm tra xem card có còn trong CardHolder không
                if (CardManager.Instance.cardHolder.Cards.Contains(defuseCard))
                {
                    Debug.Log("[ExplodingKittenUI] Fallback: Removing defuse card from CardHolder");
                    CardManager.Instance.cardHolder.RemoveCard(defuseCard);
                    
                    // Cập nhật số lượng card
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.UpdatePlayerCardCount();
                    }
                }
                else
                {
                    Debug.Log("[ExplodingKittenUI] Defuse card already removed from CardHolder");
                }
            }
            
            // Ẩn exploding panel
            if (explodingKittenPanel != null)
                explodingKittenPanel.SetActive(false);
                
            // Hiển thị input để chọn vị trí đặt lại exploding card
            if (positionInputPanel != null)
            {
                positionInputPanel.SetActive(true);
                
                // Cập nhật placeholder text để hiển thị range hợp lệ
                UpdatePlaceholderText();
            }
                
            OnDefuseCardDropped?.Invoke(defuseCard);
        }
    }
    
    private void OnConfirmPositionClicked()
    {
        if (positionInputField != null)
        {
            string input = positionInputField.text;
            if (int.TryParse(input, out int position))
            {
                // Kiểm tra vị trí hợp lệ
                int deckCount = CardManager.Instance.GetDeckCount();
                if (position >= 1 && position <= deckCount + 1)
                {
                    // Ẩn position input panel
                    positionInputPanel.SetActive(false);
                    
                    // Gọi event để CardEffectManager xử lý
                    OnDefuseConfirmed?.Invoke(position - 1);
                }
                else
                {
                    Debug.LogWarning($"Invalid position! Please enter from 1 to {deckCount + 1}");
                }
            }
            else
            {
                Debug.LogWarning("Please enter a valid number!");
            }
        }
    }
    
    public void HideExplodingPanel()
    {
        // Dừng countdown nếu đang chạy
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            Debug.Log("[ExplodingKittenUI] Countdown stopped due to panel hide");
        }
        
        if (explodingKittenPanel != null)
            explodingKittenPanel.SetActive(false);
    }
    
    public void HidePositionInputPanel()
    {
        if (positionInputPanel != null)
        {
            positionInputPanel.SetActive(false);
            
            // Clear input field khi ẩn panel
            if (positionInputField != null)
            {
                positionInputField.text = "";
            }
        }
    }
    
    // Force elimination method for when automatic elimination fails
    public void ForcePlayerElimination()
    {
        Debug.Log("Force eliminating player due to exploding without defuse");
        OnPlayerEliminated?.Invoke();
    }
    
    // Method to check if countdown is running
    public bool IsCountdownActive()
    {
        return countdownCoroutine != null;
    }
    
    // Method to manually trigger elimination for testing
    public void TestElimination()
    {
        Debug.Log("Test elimination triggered");
        OnPlayerEliminated?.Invoke();
    }
    
    // Update placeholder text with current valid range
    private void UpdatePlaceholderText()
    {
        if (positionInputField != null && positionInputField.placeholder != null && CardManager.Instance != null)
        {
            try
            {
                var placeholderText = positionInputField.placeholder.GetComponent<TMP_Text>();
                if (placeholderText != null)
                {
                    int deckCount = CardManager.Instance.GetDeckCount();
                    placeholderText.text = $"Enter position (1-{deckCount + 1})";
                    Debug.Log($"Updated placeholder text: Enter position (1-{deckCount + 1})");
                }
                else
                {
                    Debug.LogWarning("Placeholder TMP_Text component not found!");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error updating placeholder text: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Cannot update placeholder - missing components!");
        }
    }
    
    // Debug method to check defuse card status
    [ContextMenu("Debug Defuse Cards")]
    public void DebugDefuseCards()
    {
        Debug.Log("=== DEFUSE CARD DEBUG ===");
        
        if (CardManager.Instance != null && CardManager.Instance.cardHolder != null)
        {
            int defuseCount = 0;
            foreach (Card card in CardManager.Instance.cardHolder.Cards)
            {
                if (card.data.effect == "Defuse")
                {
                    defuseCount++;
                    Debug.Log($"Defuse card found: {card.data.cardName}, IsPlayed: {card.isPlayed}");
                }
            }
            
            Debug.Log($"Total defuse cards in hand: {defuseCount}");
            Debug.Log($"Total cards in hand: {CardManager.Instance.cardHolder.Cards.Count}");
        }
        else
        {
            Debug.LogError("CardManager or CardHolder is null!");
        }
        
        Debug.Log("========================");
    }
}

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
            
        // Reset trạng thái
        hasDefuseInZone = false;
        
        // Bắt đầu countdown 10 giây
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);
        countdownCoroutine = StartCoroutine(CountdownTimer(10f));
    }
    
    private IEnumerator CountdownTimer(float duration)
    {
        float timeRemaining = duration;
        
        while (timeRemaining > 0)
        {
            if (countdownText != null)
                countdownText.text = $"{timeRemaining:F1}s";
                
            yield return new WaitForSeconds(0.1f);
            timeRemaining -= 0.1f;
        }
        
        // Hết thời gian
        if (countdownText != null)
            countdownText.text = "0s";
            
        // Kiểm tra có defuse không
        if (!hasDefuseInZone)
        {
            // Không có defuse -> player bị loại
            OnPlayerEliminated?.Invoke();
        }
    }
    
    public void HandleDefuseCardDropped(Card defuseCard)
    {
        // Kiểm tra nếu card được thả vào defuse zone
        if (defuseCard.data.effect == "Defuse")
        {
            hasDefuseInZone = true;
            
            // Dừng countdown ngay lập tức
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }
            
            // Ẩn exploding panel
            if (explodingKittenPanel != null)
                explodingKittenPanel.SetActive(false);
                
            // Hiển thị input để chọn vị trí đặt lại exploding card
            if (positionInputPanel != null)
                positionInputPanel.SetActive(true);
                
            OnDefuseCardDropped?.Invoke(defuseCard);
        }
    }
    
    private void OnConfirmPositionClicked()
    {
        if (positionInputField != null)
        {
            positionInputField.placeholder.GetComponent<TMP_Text>().text = "1 - " + (CardManager.Instance.GetDeckCount() + 1);

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
        }
    }
    
    public void HideExplodingPanel()
    {
        if (explodingKittenPanel != null)
            explodingKittenPanel.SetActive(false);
    }
    
    public void HidePositionInputPanel()
    {
        if (positionInputPanel != null)
            positionInputPanel.SetActive(false);
    }
}

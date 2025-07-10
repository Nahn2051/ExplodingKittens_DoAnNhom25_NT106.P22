// SeeTheFutureUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SeeTheFutureUI : MonoBehaviour
{
    // Singleton pattern để dễ dàng truy cập từ các script khác
    public static SeeTheFutureUI Instance;

    [Header("UI References")]
    [SerializeField] private GameObject seeTheFuturePanel;
    [SerializeField] private List<Image> cardDisplayImages;
    [SerializeField] private Button closeButton; // Optional close button reference

    [Header("Settings")]
    [SerializeField] private float displayDuration = 3.0f; // Reducing from 4.0 to 3.0 seconds

    // Event to signal when the effect is complete
    public System.Action OnSeeTheFutureComplete;
    
    // Track whether the panel is currently active
    private bool isPanelActive = false;
    private Coroutine activeCoroutine = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Đảm bảo panel được ẩn khi bắt đầu
        if (seeTheFuturePanel != null)
        {
            seeTheFuturePanel.SetActive(false);
            isPanelActive = false;
        }
        
        // Set up close button if available
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => {
                ForceClosePanel();
                Debug.Log("Close button clicked on SeeTheFuture panel");
            });
        }
    }

    // Hàm này sẽ được gọi bởi CardEffectManager
    public void ShowFutureCards(int[] spriteIndexes)
    {
        // If there's an active coroutine, stop it first
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
        
        // Lấy danh sách tất cả các sprite từ CardManager
        Sprite[] allSprites = CardManager.Instance.allCardSprites;

        // Kiểm tra null để tránh lỗi
        if (allSprites == null || allSprites.Length == 0)
        {
            Debug.LogError("allCardSprites is not set or empty in CardManager!");
            return;
        }

        // Cập nhật hình ảnh cho các UI Image
        for (int i = 0; i < cardDisplayImages.Count; i++)
        {
            if (i < spriteIndexes.Length)
            {
                int spriteIndex = spriteIndexes[i];
                if (spriteIndex >= 0 && spriteIndex < allSprites.Length)
                {
                    cardDisplayImages[i].sprite = allSprites[spriteIndex];
                    cardDisplayImages[i].gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"Invalid spriteIndex {spriteIndex} received.");
                    cardDisplayImages[i].gameObject.SetActive(false);
                }
            }
            else
            {
                // Ẩn các Image không dùng đến (nếu bộ bài còn ít hơn 3 lá)
                cardDisplayImages[i].gameObject.SetActive(false);
            }
        }

        // Bắt đầu Coroutine để hiển thị và tự động ẩn panel
        activeCoroutine = StartCoroutine(ShowAndHidePanel());
    }

    private IEnumerator ShowAndHidePanel()
    {
        if (seeTheFuturePanel != null)
        {
            // Show panel
            seeTheFuturePanel.SetActive(true);
            isPanelActive = true;
            
            Debug.Log("SeeTheFuture panel shown");
            yield return new WaitForSeconds(displayDuration);
            
            // Hide panel and notify that effect is complete
            ClosePanel();
        }
        
        activeCoroutine = null;
    }
    
    // Close the panel and invoke completion event
    private void ClosePanel()
    {
        if (seeTheFuturePanel != null)
        {
            seeTheFuturePanel.SetActive(false);
            isPanelActive = false;
            
            Debug.Log("SeeTheFuture panel hidden");
            
            // Notify any listeners that the effect is complete
            OnSeeTheFutureComplete?.Invoke();
            
            // Re-enable interactions via GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EnablePlayerInteractions();
            }
        }
    }
    
    // Public method to force-close the panel if needed
    public void ForceClosePanel()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
        
        ClosePanel();
    }
    
    // Check if the panel is currently active
    public bool IsPanelActive()
    {
        return isPanelActive;
    }
}
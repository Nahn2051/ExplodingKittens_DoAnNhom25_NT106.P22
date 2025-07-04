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

    [Header("Settings")]
    [SerializeField] private float displayDuration = 4.0f; // Thời gian hiển thị panel

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
        }
    }

    // Hàm này sẽ được gọi bởi CardEffectManager
    public void ShowFutureCards(int[] spriteIndexes)
    {
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
        StartCoroutine(ShowAndHidePanel());
    }

    private IEnumerator ShowAndHidePanel()
    {
        if (seeTheFuturePanel != null)
        {
            seeTheFuturePanel.SetActive(true);
            yield return new WaitForSeconds(displayDuration);
            seeTheFuturePanel.SetActive(false);
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FavorGiveCardUI : MonoBehaviour
{
    public static FavorGiveCardUI Instance;

    [SerializeField] private Transform contentParent;
    private GameObject cardButtonPrefab; // ✅ prefab button hình lá bài
    private Action<string> onCardSelected;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        cardButtonPrefab = Resources.Load<GameObject>("Prefabs/CardButton");
        if (cardButtonPrefab == null)
            Debug.LogError("❌ Không tìm thấy prefab CardButton!");

        gameObject.SetActive(false);
    }

    public void Show(List<CardData> cards, Action<string> onSelected)
    {
        onCardSelected = onSelected;
        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // Ensure panel is shown on top
        
        Debug.Log($"FavorGiveCardUI: Showing panel with {cards.Count} cards");

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var card in cards)
        {
            GameObject btn = Instantiate(cardButtonPrefab, contentParent);

            // ✅ Set sprite cho Image
            Image img = btn.GetComponentInChildren<Image>();
            if (img != null) img.sprite = card.sprite;

            // ✅ Gắn sự kiện
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                Debug.Log("🎴 Chọn lá bài: " + card.cardName);
                gameObject.SetActive(false);
                onCardSelected?.Invoke(card.cardName); // Gửi về tên bài

                // Ensure UI interactions are restored
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.EnablePlayerInteractions();
                }
            });
        }
    }
}

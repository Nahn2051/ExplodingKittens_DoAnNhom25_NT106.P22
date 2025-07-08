using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FavorGiveCardUI : MonoBehaviour
{
    public static FavorGiveCardUI Instance;

    [SerializeField] private Transform contentParent;
    private GameObject buttonPrefab;
    private Action<string> onCardSelected;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Load prefab nút
        buttonPrefab = Resources.Load<GameObject>("Prefabs/CardSelectionButton");
        if (buttonPrefab == null)
            Debug.LogError("❌ Không tìm thấy prefab CardSelectButton");

        gameObject.SetActive(false); // ẩn từ đầu
    }

    public void Show(List<CardData> cards, Action<string> onSelected)
    {
        onCardSelected = onSelected;
        gameObject.SetActive(true);

        // Xoá nút cũ
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var card in cards)
        {
            GameObject btn = Instantiate(buttonPrefab, contentParent);
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = card.cardName;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                Debug.Log("🎴 Chọn lá: " + card.cardName);
                gameObject.SetActive(false);
                onCardSelected?.Invoke(card.cardName);
            });
        }
    }
}

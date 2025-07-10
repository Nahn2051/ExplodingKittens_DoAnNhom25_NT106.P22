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
        if (Instance == null) 
        {
            Instance = this;
            Debug.Log("FavorGiveCardUI: Instance set in Awake");
        }
        else 
        {
            Debug.LogWarning("FavorGiveCardUI: Duplicate instance found, destroying");
            Destroy(gameObject);
            return;
        }

        cardButtonPrefab = Resources.Load<GameObject>("Prefabs/CardButton");
        if (cardButtonPrefab == null)
        {
            Debug.LogError("❌ Không tìm thấy prefab CardButton!");
        }
        else
        {
            Debug.Log("FavorGiveCardUI: Card button prefab loaded successfully");
        }

        // Ensure this UI has proper components for interaction
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("FavorGiveCardUI: No Canvas component found, adding one");
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.enabled = true;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 200;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        GraphicRaycaster raycaster = GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster == null)
        {
            Debug.LogWarning("FavorGiveCardUI: No GraphicRaycaster component found, adding one");
            raycaster = gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        raycaster.enabled = true;
        
        // Ensure CanvasGroup exists for proper interaction control
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Debug.Log("FavorGiveCardUI: Initialization completed with all required components");

        gameObject.SetActive(false);
    }

    public void Show(List<CardData> cards, Action<string> onSelected)
    {
        Debug.Log($"FavorGiveCardUI.Show called with {cards.Count} cards");
        
        onCardSelected = onSelected;
        
        // Force the game object to be active first
        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // Ensure panel is shown on top
        
        // Force enable canvas and UI components with aggressive settings
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200; // Very high priority
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log($"FavorGiveCardUI: Canvas configured - enabled: {canvas.enabled}, sortingOrder: {canvas.sortingOrder}");
        }
        
        UnityEngine.UI.GraphicRaycaster raycaster = GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = true;
            Debug.Log($"FavorGiveCardUI: GraphicRaycaster enabled: {raycaster.enabled}");
        }
        
        // Force disable any blocking UI elements
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Debug.Log("FavorGiveCardUI: CanvasGroup configured for full interaction");
        }
        
        Debug.Log($"FavorGiveCardUI: Panel active: {gameObject.activeInHierarchy}, canvas enabled: {canvas?.enabled}, raycaster enabled: {raycaster?.enabled}");

        // Clear existing buttons
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        Debug.Log($"FavorGiveCardUI: Creating {cards.Count} card buttons");

        foreach (var card in cards)
        {
            GameObject btn = Instantiate(cardButtonPrefab, contentParent);

            // ✅ Set sprite cho Image
            Image img = btn.GetComponentInChildren<Image>();
            if (img != null) 
            {
                img.sprite = card.sprite;
                Debug.Log($"FavorGiveCardUI: Set sprite for card {card.cardName}");
            }

            // ✅ Ensure button is interactable
            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = true;
                
                // Force button to be visible and clickable
                CanvasGroup btnCanvasGroup = btn.GetComponent<CanvasGroup>();
                if (btnCanvasGroup != null)
                {
                    btnCanvasGroup.alpha = 1f;
                    btnCanvasGroup.interactable = true;
                    btnCanvasGroup.blocksRaycasts = true;
                }
                
                // ✅ Gắn sự kiện
                string cardName = card.cardName; // Capture for closure
                button.onClick.AddListener(() =>
                {
                    Debug.Log($"🎴 FavorGiveCardUI: Player clicked card: {cardName}");
                    gameObject.SetActive(false);
                    onCardSelected?.Invoke(cardName); // Gửi về tên bài

                    // Ensure UI interactions are restored
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.EnablePlayerInteractions();
                    }
                });
                
                Debug.Log($"FavorGiveCardUI: Button for {card.cardName} created and configured");
            }
            else
            {
                Debug.LogError("FavorGiveCardUI: Button component not found on card button!");
            }
        }
        
        // Force layout rebuild
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent.GetComponent<RectTransform>());
        
        Debug.Log("FavorGiveCardUI: All card selection buttons created and should be clickable. UI should be fully visible now.");
    }
}

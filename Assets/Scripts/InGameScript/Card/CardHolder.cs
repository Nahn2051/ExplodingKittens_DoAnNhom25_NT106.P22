using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;
using System;
using System.Collections;

public class CardHolder : MonoBehaviour
{
    private RectTransform Rect;

    [HideInInspector] public List<Card> Cards = new List<Card>();

    private Card selectedCard;
    private Card hoveredCard;
    private bool isCrossing = false;
    [SerializeField] private bool tweenCardReturn = true;

    private void Start()
    {
        Rect = GetComponent<RectTransform>();
    }

    public void DrawCard(GameObject cardPrefab, CardData data)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("cardPrefab is null!");
            return;
        }

        if (data == null || data.sprite == null)
        {
            Debug.LogError("Card data or sprite is null!");
            return;
        }
        
        if (Cards.Count <= 6 && Cards.Count != 0)
        {
            Rect.sizeDelta = new Vector2(Rect.sizeDelta.x + 204, Rect.sizeDelta.y);
        }
        
        GameObject cardSlotObj = Instantiate(cardPrefab, transform);
        Card cardComponent = cardSlotObj.GetComponentInChildren<Card>();
        if (cardComponent == null)
        {
            Debug.LogError("Cannot find Card component in CardSlot prefab!");
            return;
        }

        cardComponent.Setup(data);
        cardSlotObj.transform.SetParent(transform, false);
        Cards.Add(cardComponent);
        
        cardSlotObj.transform.localPosition = Vector3.zero;
        cardSlotObj.transform.localScale = Vector3.one;
        
        RegisterCardEvents(cardComponent);
        
        Debug.Log($"Added card '{data.cardName}' to player's hand. Current card count: {Cards.Count}");
        
        ArrangeCards();
    }
    
    private void ArrangeCards()
    {
        // Clean up null references first
        Cards.RemoveAll(card => card == null);
        
        if (Cards.Count == 0) return;
        
        float spacing = 100f;
        float offset = (Cards.Count - 1) * spacing / 2f;
        
        for (int i = 0; i < Cards.Count; i++)
        {
            Card card = Cards[i];
            if (card != null && card.transform != null && card.transform.parent != null)
            {
                Vector3 position = new Vector3(i * spacing - offset, 0, 0);
                card.transform.parent.localPosition = position;
            }
        }
    }

    private void RegisterCardEvents(Card card)
    {
        card.PointerEnterEvent.AddListener(CardPointerEnter);
        card.PointerExitEvent.AddListener(CardPointerExit);
        card.BeginDragEvent.AddListener(BeginDrag);
        card.EndDragEvent.AddListener(EndDrag);
    }
    public void RemoveCard(Card card)
    {
        if (card == null || !Cards.Contains(card)) return;
        
        // Clean up references first
        if (selectedCard == card)
            selectedCard = null;
        if (hoveredCard == card)
            hoveredCard = null;
            
        Cards.Remove(card);
        
        // Safe destroy with null check
        if (card.transform != null && card.transform.parent != null)
        {
            Destroy(card.transform.parent.gameObject);
        }
        
        if (Cards.Count <= 6 && Cards.Count != 0)
        {
            Rect.sizeDelta = new Vector2(Rect.sizeDelta.x - 204, Rect.sizeDelta.y);
        }
        
        // Rearrange remaining cards
        ArrangeCards();
        
        Debug.Log($"Removed card. Remaining cards: {Cards.Count}");
    }
    private void BeginDrag(Card card)
    {
        selectedCard = card;
    }


    void EndDrag(Card card)
    {
        if (selectedCard == null)
            return;

        selectedCard.transform.DOLocalMove(selectedCard.selected ? new Vector3(0, selectedCard.selectionOffset, 0) : Vector3.zero, tweenCardReturn ? .15f : 0).SetEase(Ease.OutBack);

        Rect.sizeDelta += Vector2.right;
        Rect.sizeDelta -= Vector2.right;

        selectedCard = null;

    }

    void CardPointerEnter(Card card)
    {
        hoveredCard = card;
    }

    void CardPointerExit(Card card)
    {
        hoveredCard = null;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            // Clean up null references first
            Cards.RemoveAll(card => card == null);
            
            foreach (Card card in Cards)
            {
                if (card != null)
                {
                    card.Deselect();
                }
            }
        }

        if (selectedCard == null)
            return;

        if (isCrossing)
            return;

        // Clean up null references before accessing
        Cards.RemoveAll(card => card == null);

        for (int i = 0; i < Cards.Count; i++)
        {
            if (Cards[i] == null) continue;

            if (selectedCard.transform.position.x > Cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() < Cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }

            if (selectedCard.transform.position.x < Cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() > Cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }
        }
    }

    void Swap(int index)
    {
        if (index < 0 || index >= Cards.Count || Cards[index] == null || selectedCard == null)
            return;
            
        isCrossing = true;

        Transform focusedParent = selectedCard.transform.parent;
        Transform crossedParent = Cards[index].transform.parent;

        Cards[index].transform.SetParent(focusedParent);
        Cards[index].transform.localPosition = Cards[index].selected ? new Vector3(0, Cards[index].selectionOffset, 0) : Vector3.zero;
        selectedCard.transform.SetParent(crossedParent);

        isCrossing = false;
    }
    public void ShuffleButton()
    {
        // Clean up null references first
        Cards.RemoveAll(card => card == null);
        
        Cards = Cards.OrderBy(a => UnityEngine.Random.value).ToList();
        for (int i = 0; i < Cards.Count; i++)
        {
            if (Cards[i] != null && Cards[i].transform.parent != null)
            {
                Cards[i].transform.parent.SetSiblingIndex(i);
            }
        }
    }

    // Method để clean up null references trong danh sách Cards
    public void CleanupNullReferences()
    {
        Cards.RemoveAll(card => card == null);
        
        // Also clear selected/hovered if they are null
        if (selectedCard == null || selectedCard.Equals(null))
            selectedCard = null;
        if (hoveredCard == null || hoveredCard.Equals(null))
            hoveredCard = null;
    }

    // Method để check card status an toàn
    public bool ContainsCard(Card card)
    {
        if (card == null) return false;
        CleanupNullReferences();
        return Cards.Contains(card);
    }
}

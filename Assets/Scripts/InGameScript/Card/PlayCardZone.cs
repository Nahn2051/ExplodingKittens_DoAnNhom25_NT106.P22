using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using System.Linq;

public class PlayCardZone : MonoBehaviour, IDropHandler
{
    public List<Card> playedCards = new List<Card>();
    public CardHolder handHolder;
    public NormalCardComboUI normalCardComboUI; // Reference to combo UI
    
    // Biến để lưu trữ các thẻ được chọn cho combo
    private List<Card> selectedComboCards = new List<Card>();

    // Safety flag để tránh duplicate processing
    private bool isProcessingCard = false;
    
    private void OnEnable()
    {
        Debug.Log("PlayCardZone OnEnable - Ready to receive cards");
    }

    private void OnDisable()
    {
        Debug.Log("PlayCardZone OnDisable - Stopping card processing");
        isProcessingCard = false;
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        // Safety check để tránh duplicate processing
        if (isProcessingCard) 
        {
            return;
        }

        // Kiểm tra nếu exploding đang diễn ra
        if (CardEffectManager.IsExplodingInProgress)
        {
            return;
        }

        if (!GameManager.Instance.IsLocalPlayerTurn())
        {
            return;
        }

        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject != null)
        {
            Card draggedCard = draggedObject.GetComponent<Card>();
            if (draggedCard != null)
            {
                // VALIDATE CARD FIRST - TRƯỚC KHI SET PROCESSING FLAG
                if (!IsCardValidForPlayZone(draggedCard))
                {
                    return;
                }

                isProcessingCard = true; // Set flag AFTER validation
                
                try 
                {
                    // CRITICAL: Cleanup deselected cards FIRST
                    CleanupDeselectedCards();
                    
                    // Lấy tất cả cards đã selected AFTER cleanup
                    List<Card> allSelectedCards = GetAllSelectedCards();
                    
                    // Kiểm tra xem có combo đang được chọn không
                    if (allSelectedCards.Count > 0)
                    {
                        // Nếu drag card không phải là part of combo, thêm vào combo
                        if (!selectedComboCards.Contains(draggedCard))
                        {
                            // Kiểm tra xem có thể thêm vào combo không
                            if (CanAddToCombo(draggedCard))
                            {
                                selectedComboCards.Add(draggedCard);
                                draggedCard.selected = true;
                            }
                            else
                            {
                                return;
                            }
                        }
                        
                        // Final validation sau khi add card
                        CleanupDeselectedCards();
                        
                        // Kiểm tra và thực hiện combo nếu đủ điều kiện
                        if (selectedComboCards.Count >= 2 && IsValidCombo(selectedComboCards))
                        {
                            // Chỉ thực hiện combo nếu đủ 2-3 lá cùng loại
                            HandleComboPlay();
                        }
                        else if (selectedComboCards.Count > 3)
                        {
                            ResetComboSelection();
                        }
                    }
                    else
                    {
                        // Single card - check loại card
                        if (IsActionCard(draggedCard.data.effect))
                        {
                            // Action cards có thể chơi đơn lẻ
                            PlaySingleCard(draggedCard);
                        }
                        else if (IsNormalCard(draggedCard.data.effect))
                        {
                            // Normal cards bắt đầu combo selection
                            selectedComboCards.Add(draggedCard);
                            draggedCard.selected = true;
                            
                            // Show helpful UI message
                            if (CardEffectManager.Instance != null)
                            {
                                CardEffectManager.Instance.ShowComboSelectionStatus(selectedComboCards.Count, draggedCard.data.effect);
                            }
                        }
                    }
                }
                finally
                {
                    // Always reset flag
                    StartCoroutine(ResetProcessingFlag());
                }
            }
        }
    }

    public void PlayCard(Card card)
    {
        // Safety check để tránh duplicate processing
        if (isProcessingCard) 
        {
            Debug.Log("Already processing a card, ignoring PlayCard");
            return;
        }
        
        // Method này bây giờ chỉ để select combo cards
        if (card == null || card.Equals(null)) return;
        
        // Clean up null references first
        selectedComboCards.RemoveAll(c => c == null || c.Equals(null));
        
        if (playedCards.Contains(card)) return;
        
        // Kiểm tra nếu exploding đang diễn ra
        if (CardEffectManager.IsExplodingInProgress)
        {
            Debug.Log("Cannot play cards while someone is handling Exploding!");
            return;
        }

        if (GameManager.Instance != null && (GameManager.Instance.IsLocalPlayerTurn() || (card != null && card.data.effect == "Nope")))
        {
            // Kiểm tra nếu là normal card
            if (IsNormalCard(card.data.effect))
            {
                // Normal cards chỉ được select cho combo, không được chơi đơn
                // Và chỉ được chơi khi đến lượt
                if (!GameManager.Instance.IsLocalPlayerTurn())
                {
                    Debug.LogWarning("Normal cards can only be played during your turn!");
                    return;
                }
                
                // Toggle selection cho combo
                if (selectedComboCards.Contains(card))
                {
                    // Unselect card
                    selectedComboCards.Remove(card);
                    card.selected = false;
                    Debug.Log($"Unselected card {card.data.effect}. Remaining: {selectedComboCards.Count}");
                    
                    // Update status display
                    if (CardEffectManager.Instance != null)
                    {
                        if (selectedComboCards.Count > 0)
                        {
                            CardEffectManager.Instance.ShowComboSelectionStatus(selectedComboCards.Count, selectedComboCards[0].data.effect);
                        }
                        else
                        {
                            CardEffectManager.Instance.ShowComboSelectionStatus(0, "");
                        }
                    }
                }
                else
                {
                    // Check if we can add this card to the combo
                    if (selectedComboCards.Count > 0 && selectedComboCards[0].data.effect != card.data.effect)
                    {
                        Debug.LogWarning($"Cannot mix different normal card types in combo! Selected: {selectedComboCards[0].data.effect}, Trying to add: {card.data.effect}");
                        return;
                    }
                    
                    if (selectedComboCards.Count >= 3)
                    {
                        Debug.LogWarning("Cannot select more than 3 cards for combo!");
                        return;
                    }
                    
                    // Select card for combo
                    selectedComboCards.Add(card);
                    card.selected = true;
                    Debug.Log($"Selected card {card.data.effect} for combo. Total: {selectedComboCards.Count}");
                    
                    // Update status display
                    if (CardEffectManager.Instance != null)
                    {
                        CardEffectManager.Instance.ShowComboSelectionStatus(selectedComboCards.Count, card.data.effect);
                    }
                    
                    // Auto-execute combo if it's complete and valid (ít nhất 2 thẻ)
                    if (selectedComboCards.Count >= 2 && IsValidCombo(selectedComboCards))
                    {
                        Debug.Log($"Valid combo detected with {selectedComboCards.Count} cards, ready to execute");
                        // Có thể auto-execute hoặc chờ player kéo thêm lá thứ 3
                        if (selectedComboCards.Count == 3)
                        {
                            // 3 lá - execute ngay
                            StartCoroutine(AutoExecuteComboAfterDelay(0.5f));
                        }
                        else
                        {
                            // 2 lá - có thể execute hoặc chờ thêm lá thứ 3
                            // Hiển thị thông báo cho player
                            Debug.Log("2-card combo ready. You can play another card of the same type for 3-card combo, or wait for auto-execution.");
                            StartCoroutine(AutoExecuteComboAfterDelay(3f)); // Chờ 3 giây để player quyết định
                        }
                    }
                }
                return;
            }
            
            // VALIDATE before processing action cards
            if (!IsCardValidForPlayZone(card))
            {
                Debug.LogWarning($"Card {card.data.cardName} is not valid for PlayZone!");
                return;
            }
            
            // Kiểm tra xem có phải lượt của người chơi không
            if (!GameManager.Instance.IsLocalPlayerTurn())
            {
                Debug.LogWarning("Cannot play action cards - not your turn!");
                return;
            }
            
            // Set processing flag
            isProcessingCard = true;
            
            try 
            {
                // Chơi action card ngay lập tức
                PlaySingleCard(card);
            }
            finally
            {
                StartCoroutine(ResetProcessingFlag());
            }
        }
        else
        {
            Debug.Log("Cannot play cards - not your turn!");
        }
    }
    
    private void HandleComboPlay()
    {
        // Validate combo
        if (!IsValidCombo(selectedComboCards))
        {
            ShowComboErrorMessage();
            ResetComboSelection();
            return;
        }
        
        // Send combo to CardEffectManager for UI handling
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.HandleNormalCardCombo(new List<Card>(selectedComboCards));
        }
        
        // Remove combo cards from hand through CardManager
        List<Card> cardsToRemove = new List<Card>(selectedComboCards);
        ResetComboSelection(); // Reset first to prevent conflicts
        
        // Remove cards from hand without triggering individual effects
        foreach (Card card in cardsToRemove)
        {
            if (CardManager.Instance != null && CardManager.Instance.cardHolder != null)
            {
                CardManager.Instance.cardHolder.RemoveCard(card);
            }
        }
    }

    private bool IsValidCombo(List<Card> cards)
    {
        if (cards.Count < 2 || cards.Count > 3) return false;
        
        // Kiểm tra tất cả cards có cùng loại không
        string firstCardType = cards[0].data.effect;
        foreach (Card card in cards)
        {
            if (card.data.effect != firstCardType || !IsNormalCard(card.data.effect))
            {
                return false;
            }
        }
        
        return true;
    }

    private void ShowComboErrorMessage()
    {
        // Hiển thị thông báo lỗi combo - reduced redundancy
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.ShowComboHelpMessage();
        }
    }

    private bool IsNormalCard(string cardType)
    {
        return cardType == "HairyPotatoCat" || cardType == "BeardCat" || 
               cardType == "Cattermelon" || cardType == "Tacocat" || 
               cardType == "RainbowRalphingCat";
    }

    private void CheckCombo()
    {
        if (selectedComboCards.Count < 2) return;
        
        // Kiểm tra tất cả các thẻ có cùng loại không
        string firstCardType = selectedComboCards[0].data.effect;
        bool allSameType = selectedComboCards.All(card => card.data.effect == firstCardType);
        
        if (allSameType && (selectedComboCards.Count == 2 || selectedComboCards.Count == 3))
        {
            // Combo hợp lệ - thực hiện
            HandleComboPlay();
        }
        else if (selectedComboCards.Count > 3)
        {
            ResetComboSelection();
        }
        else if (!allSameType)
        {
            ResetComboSelection();
        }
    }
    
    private void ResetComboSelection()
    {
        foreach (Card card in selectedComboCards)
        {
            if (card != null)
            {
                card.selected = false;
                // Reset visual position
                card.transform.localPosition = Vector3.zero;
            }
        }
        selectedComboCards.Clear();
        
        // Update UI
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.ShowComboSelectionStatus(0, "");
        }
    }
    
    private void PlaySingleCard(Card card)
    {
        if (card == null) return;
        
        // Kiểm tra card đã được played chưa
        if (card.isPlayed || playedCards.Contains(card)) 
        {
            Debug.Log($"Card {card.data.cardName} already played, skipping");
            return;
        }
        
        // CHECK: Chỉ Action cards hoặc combo cards được phép play
        if (!IsActionCard(card.data.effect) && !IsNormalCard(card.data.effect))
        {
            Debug.LogWarning($"Card {card.data.effect} cannot be played!");
            return;
        }
        
        Debug.Log($"Playing single card: {card.data.cardName}");
        
        // Send to CardManager for processing (CardManager sẽ handle removal và marking)
        if (CardManager.Instance != null)
        {
            CardManager.Instance.PlayCard(card, Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    public void AddPlayedCard(Card card, int playerActorNumber)
    {
        Debug.Log($"PlayCardZone.AddPlayedCard: Adding {card.data.cardName} for player {playerActorNumber}");
        
        if (card == null) return;
        
        // Prevent duplicate additions
        if (playedCards.Contains(card)) return;

        playedCards.Add(card);
        card.isDragging = false;
        card.isHovering = false;
        card.selected = false;
        card.isPlayed = true;

        float randomAngle = Random.Range(-20f, 20f);

        card.transform.SetParent(transform);
        card.transform.SetAsLastSibling();
        card.transform.localScale = Vector3.one * 0.4f;

        card.transform.DOLocalMove(Vector3.zero, 0.25f).SetEase(Ease.OutBack);
        card.transform.DORotate(new Vector3(0, 0, randomAngle), 0.25f);

        card.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        card.GetComponent<CanvasGroup>().blocksRaycasts = false;
        
        card.GetComponent<UnityEngine.UI.Image>().enabled = true;

        Debug.Log($"Player {playerActorNumber} played card {card.data.cardName}: {card.data.effect}");
        Debug.Log($"PlayCardZone.AddPlayedCard: Visual setup complete, starting effect animation");

        StartCoroutine(ShowCardEffectAnimation(card.data.effect));
    }

    private IEnumerator ShowCardEffectAnimation(string effectType)
    {
        Debug.Log($"PlayCardZone.ShowCardEffectAnimation: Starting animation for {effectType}");
        
        // Wait for animation to complete
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"PlayCardZone.ShowCardEffectAnimation: Animation completed for {effectType}. UI restoration is handled by CardEffectManager.");
    }

    public void ClearPlayZone()
    {
        foreach (Card card in playedCards)
        {
            Destroy(card.transform.parent.gameObject);
        }
        playedCards.Clear();
    }

    // Method để reset combo selection từ bên ngoài
    public void ResetComboSelectionPublic()
    {
        ResetComboSelection();
    }
    
    // Method để kiểm tra có combo đang được chọn không
    public bool HasSelectedCombo()
    {
        return selectedComboCards.Count > 0;
    }
    
    // Method để lấy số lượng thẻ đã chọn cho combo
    public int GetSelectedComboCount()
    {
        return selectedComboCards.Count;
    }

    // Public method để execute combo từ UI (khi player đã chọn target)
    public void ExecuteSelectedCombo()
    {
        if (selectedComboCards.Count >= 2 && selectedComboCards.Count <= 3)
        {
            HandleComboPlay();
        }
        else
        {
            Debug.LogWarning("Invalid combo size for execution!");
            ResetComboSelection();
        }
    }
    
    // Public method để cancel combo selection
    public void CancelComboSelection()
    {
        Debug.Log("Canceling combo selection");
        ResetComboSelection();
    }

    // Public method để force cleanup từ bên ngoài
    public void ForceCleanupSelection()
    {
        // Cleanup null references
        CleanupNullReferences();
        
        // Cleanup deselected cards 
        CleanupDeselectedCards();
        
        // Double check - remove any cards from selectedComboCards that are not actually selected
        for (int i = selectedComboCards.Count - 1; i >= 0; i--)
        {
            Card card = selectedComboCards[i];
            if (card == null || !card.selected)
            {
                selectedComboCards.RemoveAt(i);
            }
        }
        
        // Update UI to reflect current state
        if (CardEffectManager.Instance != null)
        {
            if (selectedComboCards.Count > 0)
            {
                CardEffectManager.Instance.ShowComboSelectionStatus(selectedComboCards.Count, selectedComboCards[0].data.effect);
            }
            else
            {
                CardEffectManager.Instance.ShowComboSelectionStatus(0, "");
            }
        }
    }

    private void Update()
    {
        // Clean up null references periodically (less frequent)
        if (Time.frameCount % 60 == 0) // Only every 60 frames
        {
            CleanupNullReferences();
        }
        
        // Right-click để hủy combo selection
        if (Input.GetMouseButtonDown(1) && selectedComboCards.Count > 0)
        {
            ResetComboSelection();
        }
    }

    // Method để cleanup null references an toàn
    public void CleanupNullReferences()
    {
        selectedComboCards.RemoveAll(card => card == null || card.Equals(null));
        playedCards.RemoveAll(card => card == null || card.Equals(null));
    }

    private IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.1f); // Small delay to prevent conflicts
        isProcessingCard = false;
    }
    
    private IEnumerator AutoExecuteComboAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Check if combo is still valid and not cancelled
        if (selectedComboCards.Count >= 2 && IsValidCombo(selectedComboCards))
        {
            Debug.Log($"Auto-executing combo after {delay}s delay");
            HandleComboPlay();
        }
        else
        {
            Debug.Log("Combo no longer valid for auto-execution");
        }
    }
    
    // Method để validate xem card có thể được chơi vào PlayZone không
    private bool IsCardValidForPlayZone(Card card)
    {
        if (card == null || card.data == null) return false;
        
        string cardEffect = card.data.effect;
        
        // 1. Defuse chỉ được dùng để gỡ bom, KHÔNG được kéo vào PlayZone
        if (cardEffect == "Defuse")
        {
            Debug.LogWarning("Defuse cards can only be used to defuse bombs, not played in PlayZone!");
            return false;
        }
        
        // 2. Exploding Kitten KHÔNG BAO GIỜ được chơi
        if (cardEffect == "Exploding")
        {
            Debug.LogWarning("Exploding Kitten cannot be played!");
            return false;
        }
        
        // 3. Normal cards - cần special handling
        if (IsNormalCard(cardEffect))
        {
            // Normal cards chỉ được phép nếu đang có combo selection
            if (selectedComboCards.Count > 0)
            {
                // Đang có combo - check xem card này có cùng loại với combo không
                string comboType = selectedComboCards[0].data.effect;
                if (cardEffect != comboType)
                {
                    Debug.LogWarning($"Card {cardEffect} doesn't match combo type {comboType}!");
                    return false;
                }
                
                // Check số lượng combo không vượt quá 3
                if (selectedComboCards.Count >= 3 && !selectedComboCards.Contains(card))
                {
                    Debug.LogWarning("Combo cannot have more than 3 cards!");
                    return false;
                }
                
                return true;
            }
            else
            {
                // Cleanup deselected cards trước khi kiểm tra
                CleanupDeselectedCards();
                
                // Kiểm tra có cards đã selected không (từ clicked cards)
                List<Card> allSelectedCards = GetAllSelectedCards();
                
                if (allSelectedCards.Count > 0)
                {
                    // Có cards đã selected - kiểm tra compatibility 
                    string comboType = allSelectedCards[0].data.effect;
                    if (cardEffect != comboType)
                    {
                        Debug.LogWarning($"Card {cardEffect} doesn't match combo type {comboType}!");
                        return false;
                    }
                    
                    Debug.Log($"Normal card {cardEffect} valid for combo with {allSelectedCards.Count} selected cards");
                    return true;
                }
                else
                {
                    // Không có cards đã selected - cho phép start combo mới
                    Debug.Log($"Normal card {cardEffect} can start new combo");
                    return true;
                }
            }
        }
        
        // 4. Action cards - có thể chơi đơn lẻ
        if (IsActionCard(cardEffect))
        {
            return true;
        }
        
        // 5. Unknown card type
        Debug.LogWarning($"Unknown card type: {cardEffect}");
        return false;
    }
    
    private bool CanAddToCombo(Card card)
    {
        if (selectedComboCards.Count == 0) 
        {
            return true; // No combo yet
        }
        
        // Check same card type, max combo size, not already in combo, and is normal card
        return card.data.effect == selectedComboCards[0].data.effect && 
               selectedComboCards.Count < 3 && 
               !selectedComboCards.Contains(card) &&
               IsNormalCard(card.data.effect);
    }

    // Method để check xem có phải action card không
    private bool IsActionCard(string cardType)
    {
        return cardType == "Favor" || cardType == "Nope" || cardType == "Shuffle" || 
               cardType == "Skip" || cardType == "SeeTheFuture" || cardType == "Attack";
    }

    // Method để lấy tất cả cards đã selected (từ selectedComboCards + clicked cards)
    private List<Card> GetAllSelectedCards()
    {
        List<Card> allSelected = new List<Card>();
        
        // Cleanup null references và deselected cards trước tiên
        CleanupDeselectedCards();
        
        // Tìm các cards đã selected bằng cách click (card.selected = true)
        if (CardManager.Instance != null && CardManager.Instance.cardHolder != null)
        {
            foreach (Card card in CardManager.Instance.cardHolder.Cards)
            {
                if (card != null && card.selected && !allSelected.Contains(card))
                {
                    allSelected.Add(card);
                }
            }
        }
        
        // Đảm bảo selectedComboCards chỉ chứa cards thực sự selected
        for (int i = selectedComboCards.Count - 1; i >= 0; i--)
        {
            Card card = selectedComboCards[i];
            if (card == null || !card.selected)
            {
                selectedComboCards.RemoveAt(i);
            }
            else if (!allSelected.Contains(card))
            {
                allSelected.Add(card);
            }
        }
        
        // Đồng bộ selectedComboCards với allSelected (chỉ normal cards)
        selectedComboCards.Clear();
        foreach (Card card in allSelected)
        {
            if (IsNormalCard(card.data.effect))
            {
                selectedComboCards.Add(card);
            }
        }
        
        return allSelected;
    }

    // Method để cleanup các cards đã bị deselect
    private void CleanupDeselectedCards()
    {
        // Loại bỏ cards không còn selected hoặc null khỏi selectedComboCards
        int originalCount = selectedComboCards.Count;
        selectedComboCards.RemoveAll(card => card == null || !card.selected);
        
        if (originalCount != selectedComboCards.Count)
        {
            // Update effect status display
            if (CardEffectManager.Instance != null)
            {
                if (selectedComboCards.Count > 0)
                {
                    CardEffectManager.Instance.ShowComboSelectionStatus(selectedComboCards.Count, selectedComboCards[0].data.effect);
                }
                else
                {
                    CardEffectManager.Instance.ShowComboSelectionStatus(0, "");
                }
            }
        }
    }
}

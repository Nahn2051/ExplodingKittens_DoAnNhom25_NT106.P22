using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DefuseZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // Lấy card đang được kéo
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject != null)
        {
            Card card = draggedObject.GetComponent<Card>();
            if (card != null && card.data.effect == "Defuse")
            {
                Debug.Log($"[DefuseZone] Defuse card {card.data.cardName} dropped in defuse zone!");
                
                // Kiểm tra nếu card đã được played để tránh duplicate processing
                if (card.isPlayed)
                {
                    Debug.LogWarning($"[DefuseZone] Card {card.data.cardName} already played, ignoring");
                    return;
                }
                
                // Đánh dấu card đã được played
                card.isPlayed = true;
                
                // Đảm bảo card được remove khỏi CardHolder trước khi thông báo CardEffectManager
                if (CardManager.Instance != null && CardManager.Instance.cardHolder != null)
                {
                    // Kiểm tra xem card có trong CardHolder không trước khi remove
                    if (CardManager.Instance.cardHolder.Cards.Contains(card))
                    {
                        Debug.Log($"[DefuseZone] Removing defuse card {card.data.cardName} from CardHolder");
                        CardManager.Instance.cardHolder.RemoveCard(card);
                        
                        // Cập nhật số lượng card
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.UpdatePlayerCardCount();
                        }
                    }
                    else
                    {
                        Debug.Log($"[DefuseZone] Card {card.data.cardName} not found in CardHolder (may already be removed)");
                    }
                }
                
                // Thông báo cho CardEffectManager để xử lý logic exploding
                if (CardEffectManager.Instance != null)
                {
                    CardEffectManager.Instance.OnDefuseCardDropped(card);
                }
                
                // Destroy visual object
                StartCoroutine(DestroyCardAfterDelay(card.transform.parent.gameObject, 0.2f));
            }
        }
    }
    
    private System.Collections.IEnumerator DestroyCardAfterDelay(GameObject cardObject, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (cardObject != null)
        {
            Debug.Log("[DefuseZone] Destroying used defuse card visual object");
            Destroy(cardObject);
        }
    }
}

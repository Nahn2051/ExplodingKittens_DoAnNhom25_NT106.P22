using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DefuseZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject != null)
        {
            Card card = draggedObject.GetComponent<Card>();
            if (card != null && card.data.effect == "Defuse")
            {
                Debug.Log($"[DefuseZone] Defuse card {card.data.cardName} dropped in defuse zone!");
                
                // Tránh xử lý card đã được played
                if (card.isPlayed)
                {
                    Debug.LogWarning($"[DefuseZone] Card {card.data.cardName} already played, ignoring");
                    return;
                }
                
                card.isPlayed = true;
                
                if (CardManager.Instance != null && CardManager.Instance.cardHolder != null)
                {
                    if (CardManager.Instance.cardHolder.Cards.Contains(card))
                    {
                        Debug.Log($"[DefuseZone] Removing defuse card {card.data.cardName} from CardHolder");
                        CardManager.Instance.cardHolder.RemoveCard(card);
                        
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
                
                // Thông báo cho CardEffectManager xử lý logic exploding
                if (CardEffectManager.Instance != null)
                {
                    CardEffectManager.Instance.OnDefuseCardDropped(card);
                }
                
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

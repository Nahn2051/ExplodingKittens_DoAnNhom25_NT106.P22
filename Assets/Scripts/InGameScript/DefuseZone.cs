using UnityEngine;
using UnityEngine.EventSystems;

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
                Debug.Log("Defuse card dropped in defuse zone!");
                
                // Thông báo cho CardEffectManager
                if (CardEffectManager.Instance != null)
                {
                    CardEffectManager.Instance.OnDefuseCardDropped(card);
                }
                
                // Xóa card khỏi tay người chơi
                if (CardManager.Instance != null && CardManager.Instance.cardHolder != null)
                {
                    CardManager.Instance.cardHolder.RemoveCard(card);
                }
                
                // Destroy card object vì đã sử dụng
                Destroy(card.transform.parent.gameObject);
            }
        }
    }
}

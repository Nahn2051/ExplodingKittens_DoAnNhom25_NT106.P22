using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableInputNotice : MonoBehaviour, IPointerDownHandler
{
    public GameObject noticePanel; // Gán 1 panel hiện thông báo

    public void OnPointerDown(PointerEventData eventData)
    {
        if (noticePanel != null)
        {
            noticePanel.SetActive(true);
            // Ẩn sau 2 giây
            CancelInvoke();
            Invoke("HideNotice", 2f);
        }
    }

    void HideNotice()
    {
        if (noticePanel != null)
            noticePanel.SetActive(false);
    }
}

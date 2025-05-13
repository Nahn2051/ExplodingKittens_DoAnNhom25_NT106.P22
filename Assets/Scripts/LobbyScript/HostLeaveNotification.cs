using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HostLeaveNotification : MonoBehaviour
{
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private float displayTime = 3f;
    
    private void Awake()
    {
        // Make sure panel is hidden by default
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
    
    public void ShowHostLeftNotification()
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = "Host đã rời phòng. Bạn sẽ trở về màn hình tham gia...";
            notificationPanel.SetActive(true);
            
            // Automatically hide after delay
            StartCoroutine(HideAfterDelay());
        }
    }
    
    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);
        
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
} 
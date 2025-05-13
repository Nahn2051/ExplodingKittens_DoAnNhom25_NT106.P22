using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class HostLeaveNotification : MonoBehaviour
{
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private float displayTime = 3f;
    [SerializeField] private float returnToJoinSceneDelay = 5f;
    
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
    
    public void ShowNotification()
    {
        ShowHostLeftNotification();
        
        // Return to Join Scene after delay
        StartCoroutine(ReturnToJoinScene());
    }
    
    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);
        
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
    
    private IEnumerator ReturnToJoinScene()
    {
        yield return new WaitForSeconds(returnToJoinSceneDelay);
        
        // Ngắt kết nối Photon nếu cần
        if (Photon.Pun.PhotonNetwork.IsConnected && Photon.Pun.PhotonNetwork.InRoom)
        {
            Photon.Pun.PhotonNetwork.LeaveRoom();
        }
        
        // Load JoinScene
        SceneManager.LoadScene("JoinScene");
    }
} 
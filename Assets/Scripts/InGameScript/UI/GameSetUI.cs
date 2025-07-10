using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class GameSetUI : MonoBehaviourPunCallbacks
{
    [Header("Game Set UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private TMP_Text winnerNameText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private GameObject winnerBackground;
    [SerializeField] private GameObject loserBackground;
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    
    // Events
    public System.Action OnRestartGame;
    public System.Action OnReturnToMainMenu;
    
    private void Start()
    {
        // Ẩn UI ban đầu
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
        // Thiết lập button events
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }
    
    public void ShowGameOver(string winnerName, bool isLocalPlayerWinner)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // Thiết lập text
        if (gameOverText != null)
        {
            gameOverText.text = isLocalPlayerWinner ? "Victory!" : "Game Over";
        }
        
        if (winnerNameText != null)
        {
            winnerNameText.text = $"Winner: {winnerName}";
        }
        
        // Hiển thị background phù hợp
        if (winnerBackground != null)
            winnerBackground.SetActive(isLocalPlayerWinner);
        if (loserBackground != null)
            loserBackground.SetActive(!isLocalPlayerWinner);
        
        // Phát hiệu ứng
        if (isLocalPlayerWinner)
        {
            PlayWinnerEffects();
        }
        else
        {
            PlayLoserEffects();
        }
        
        // Animation fade in
        StartCoroutine(FadeInGameOverPanel());
    }
    
    private void PlayWinnerEffects()
    {
        // Có thể thêm hiệu ứng khác nếu cần
        Debug.Log("Winner effects played!");
    }
    
    private void PlayLoserEffects()
    {
        // Có thể thêm hiệu ứng khác nếu cần
        Debug.Log("Loser effects played!");
    }
    
    private IEnumerator FadeInGameOverPanel()
    {
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            
            float duration = 1f;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                gameOverCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
                yield return null;
            }
            
            gameOverCanvasGroup.alpha = 1f;
        }
    }
    
    private void OnRestartClicked()
    {
        // Chỉ Master Client mới có thể restart game
        if (PhotonNetwork.IsMasterClient)
        {
            // Trigger event cho GameManager
            OnRestartGame?.Invoke();
        }
        else
        {
            Debug.Log("Only room master can restart the game!");
        }
        
        // Ẩn UI
        HideGameOverPanel();
    }
    
    private void OnMainMenuClicked()
    {
        // Trigger event cho GameManager
        OnReturnToMainMenu?.Invoke();
        
        // Ẩn UI
        HideGameOverPanel();
    }
    
    private void HideGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }
    
    public void ShowPlayerEliminated(string playerName, bool isLocalPlayer)
    {
        // Có thể thêm UI hiển thị khi player bị loại (không phải game over hoàn toàn)
        if (isLocalPlayer)
        {
            Debug.Log("You have been eliminated!");
        }
        else
        {
            Debug.Log($"{playerName} has been eliminated!");
        }
    }
}

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
    
    // Events để thông báo cho GameManager
    public System.Action OnRestartGame;
    public System.Action OnReturnToMainMenu;
    
    private void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }
    
    public void ShowGameOver(string winnerName, bool isLocalPlayerWinner)
    {
        Debug.Log($"[GameSetUI] ShowGameOver called - Winner: {winnerName}, IsLocalWinner: {isLocalPlayerWinner}");
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("[GameSetUI] Game over panel activated");
        }
        
        // Hiển thị text chiến thắng hoặc thua cuộc
        if (gameOverText != null)
        {
            string displayText = isLocalPlayerWinner ? "Victory!" : "Game Over";
            gameOverText.text = displayText;
            Debug.Log($"[GameSetUI] Game over text set to: {displayText}");
        }
        
        if (winnerNameText != null)
        {
            string winnerDisplayText = $"Winner: {winnerName}";
            winnerNameText.text = winnerDisplayText;
            Debug.Log($"[GameSetUI] Winner name text set to: {winnerDisplayText}");
        }
        
        // Hiển thị background tương ứng với kết quả
        bool showWinnerBg = isLocalPlayerWinner;
        bool showLoserBg = !isLocalPlayerWinner;
        
        if (winnerBackground != null)
        {
            winnerBackground.SetActive(showWinnerBg);
            Debug.Log($"[GameSetUI] Winner background set to: {showWinnerBg}");
        }
        
        if (loserBackground != null)
        {
            loserBackground.SetActive(showLoserBg);
            Debug.Log($"[GameSetUI] Loser background set to: {showLoserBg}");
        }
        
        if (isLocalPlayerWinner)
        {
            PlayWinnerEffects();
        }
        else
        {
            PlayLoserEffects();
        }
        
        StartCoroutine(FadeInGameOverPanel());
        
        Debug.Log("[GameSetUI] ShowGameOver setup completed");
    }
    
    private void PlayWinnerEffects()
    {
        Debug.Log("Winner effects played!");
    }
    
    private void PlayLoserEffects()
    {
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
        // Chỉ Master Client mới có quyền restart game
        if (PhotonNetwork.IsMasterClient)
        {
            OnRestartGame?.Invoke();
        }
        else
        {
            Debug.Log("Only room master can restart the game!");
        }
        
        HideGameOverPanel();
    }
    
    private void OnMainMenuClicked()
    {
        OnReturnToMainMenu?.Invoke();
        
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

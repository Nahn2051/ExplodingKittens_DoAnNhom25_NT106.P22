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
        Debug.Log($"[GameSetUI] ShowGameOver called - Winner: {winnerName}, IsLocalWinner: {isLocalPlayerWinner}");
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling(); // Ensure it's on top
            Debug.Log("[GameSetUI] Game over panel activated and moved to front");
        }
        else
        {
            Debug.LogError("[GameSetUI] gameOverPanel is null!");
        }
        
        // Force canvas settings to ensure it's visible
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000; // Very high priority
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log($"[GameSetUI] Canvas configured - enabled: {canvas.enabled}, sortingOrder: {canvas.sortingOrder}");
        }
        
        // Thiết lập text với debug logging
        if (gameOverText != null)
        {
            string displayText = isLocalPlayerWinner ? "🎉 VICTORY! 🎉" : "💀 GAME OVER 💀";
            gameOverText.text = displayText;
            Debug.Log($"[GameSetUI] Game over text set to: {displayText}");
        }
        else
        {
            Debug.LogError("[GameSetUI] gameOverText is null!");
        }
        
        if (winnerNameText != null)
        {
            string winnerDisplayText = isLocalPlayerWinner ? 
                $"Congratulations!\nWinner: {winnerName}" : 
                $"Better luck next time!\nWinner: {winnerName}";
            winnerNameText.text = winnerDisplayText;
            Debug.Log($"[GameSetUI] Winner name text set to: {winnerDisplayText}");
        }
        else
        {
            Debug.LogError("[GameSetUI] winnerNameText is null!");
        }
        
        // Force disable both backgrounds first, then enable the correct one with delay
        if (winnerBackground != null)
        {
            winnerBackground.SetActive(false);
        }
        if (loserBackground != null)
        {
            loserBackground.SetActive(false);
        }
        
        // Wait a frame then set the correct background
        StartCoroutine(SetBackgroundAfterDelay(isLocalPlayerWinner));
        
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
        
        Debug.Log("[GameSetUI] ShowGameOver setup completed");
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
    
    public void ShowPlayerEliminated(string message, bool isLocalPlayer)
    {
        Debug.Log($"[GameSetUI] ShowPlayerEliminated called - Message: {message}, IsLocal: {isLocalPlayer}");
        
        // For now, just show it as a game over screen if it's the local player
        if (isLocalPlayer)
        {
            Debug.Log("[GameSetUI] Local player eliminated - showing elimination screen");
            
            // Force the game over panel to be active and on top
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                gameOverPanel.transform.SetAsLastSibling();
                Debug.Log("[GameSetUI] Elimination panel activated and moved to front");
            }
            
            // Force canvas settings
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = true;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 1000;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            
            if (gameOverText != null)
            {
                gameOverText.text = "💀 YOU HAVE BEEN ELIMINATED! 💀";
                Debug.Log("[GameSetUI] Elimination text set");
            }
            
            if (winnerNameText != null)
            {
                winnerNameText.text = "Better luck next time!";
                Debug.Log("[GameSetUI] Elimination message set");
            }
            
            // Force show loser background
            if (winnerBackground != null)
                winnerBackground.SetActive(false);
            if (loserBackground != null)
            {
                loserBackground.SetActive(false);
                // Use coroutine to set background with delay
                StartCoroutine(SetEliminationBackgroundAfterDelay());
            }
                
            // Start fade in animation
            StartCoroutine(FadeInGameOverPanel());
        }
        else
        {
            Debug.Log($"[GameSetUI] Another player eliminated: {message}");
            // Could add a notification system here for other players
        }
    }
    
    private IEnumerator SetEliminationBackgroundAfterDelay()
    {
        yield return null; // Wait one frame
        
        if (loserBackground != null)
        {
            loserBackground.SetActive(true);
            Debug.Log($"[GameSetUI] Elimination background activated: {loserBackground.activeInHierarchy}");
        }
    }

    // Method to test win/lose display
    public void TestWinDisplay()
    {
        Debug.Log("[GameSetUI] Testing win display");
        ShowGameOver("TestPlayer", true);
    }
    
    public void TestLoseDisplay()
    {
        Debug.Log("[GameSetUI] Testing lose display");
        ShowGameOver("TestPlayer", false);
    }
    
    // Method to validate all UI components are assigned
    public void ValidateUIComponents()
    {
        Debug.Log("[GameSetUI] Validating UI components:");
        Debug.Log($"gameOverPanel: {(gameOverPanel != null ? "OK" : "NULL")}");
        Debug.Log($"gameOverText: {(gameOverText != null ? "OK" : "NULL")}");
        Debug.Log($"winnerNameText: {(winnerNameText != null ? "OK" : "NULL")}");
        Debug.Log($"restartButton: {(restartButton != null ? "OK" : "NULL")}");
        Debug.Log($"mainMenuButton: {(mainMenuButton != null ? "OK" : "NULL")}");
        Debug.Log($"winnerBackground: {(winnerBackground != null ? "OK" : "NULL")}");
        Debug.Log($"loserBackground: {(loserBackground != null ? "OK" : "NULL")}");
        Debug.Log($"gameOverCanvasGroup: {(gameOverCanvasGroup != null ? "OK" : "NULL")}");
    }
    
    private IEnumerator SetBackgroundAfterDelay(bool isLocalPlayerWinner)
    {
        yield return null; // Wait one frame
        
        // Now set the appropriate background
        bool showWinnerBg = isLocalPlayerWinner;
        bool showLoserBg = !isLocalPlayerWinner;
        
        if (winnerBackground != null)
        {
            winnerBackground.SetActive(showWinnerBg);
            Debug.Log($"[GameSetUI] Winner background set to: {showWinnerBg}, actually active: {winnerBackground.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("[GameSetUI] winnerBackground is null!");
        }
        
        if (loserBackground != null)
        {
            loserBackground.SetActive(showLoserBg);
            Debug.Log($"[GameSetUI] Loser background set to: {showLoserBg}, actually active: {loserBackground.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("[GameSetUI] loserBackground is null!");
        }
    }
}

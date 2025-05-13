using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbySetup : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject playerAvatarPrefab;
    public GameObject lobbyPlayerHandlerPrefab;
    
    private static LobbySetup _instance;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LobbyScene")
        {
            SetupLobbyScene();
        }
    }
    
    private void SetupLobbyScene()
    {
        // Tìm kiếm LobbyPlayerHandler
        LobbyPlayerHandler existingHandler = FindObjectOfType<LobbyPlayerHandler>();
        
        // Nếu không tìm thấy, tạo mới
        if (existingHandler == null && lobbyPlayerHandlerPrefab != null)
        {
            Debug.Log("Tạo LobbyPlayerHandler mới...");
            GameObject handlerObj = Instantiate(lobbyPlayerHandlerPrefab);
            LobbyPlayerHandler handler = handlerObj.GetComponent<LobbyPlayerHandler>();
            
            if (handler != null && playerAvatarPrefab != null)
            {
                handler.playerAvatarPrefab = playerAvatarPrefab;
                handler.Initialize();
            }
        }
        else if (existingHandler != null)
        {
            Debug.Log("LobbyPlayerHandler đã tồn tại, đảm bảo cấu hình đúng...");
            if (existingHandler.playerAvatarPrefab == null && playerAvatarPrefab != null)
            {
                existingHandler.playerAvatarPrefab = playerAvatarPrefab;
            }
            existingHandler.Initialize();
        }
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
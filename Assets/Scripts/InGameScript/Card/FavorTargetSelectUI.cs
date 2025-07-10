using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;
using Photon.Pun;

public class FavorTargetSelectUI : MonoBehaviour
{
    public static FavorTargetSelectUI Instance;

    [SerializeField] private Transform contentParent;
    private GameObject buttonPrefab;
    private Action<int> onTargetSelected;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Load prefab từ Resources
        buttonPrefab = Resources.Load<GameObject>("Prefabs/SimplePlayerButton");
        if (buttonPrefab == null) Debug.LogError("❌ Không tìm thấy prefab SimplePlayerButton trong Resources/Prefabs/");

        // Ensure this UI has proper components for interaction
        if (GetComponent<Canvas>() == null)
        {
            Debug.LogWarning("FavorTargetSelectUI: No Canvas component found, UI interaction might not work properly");
        }
        
        if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            Debug.LogWarning("FavorTargetSelectUI: No GraphicRaycaster component found, UI interaction might not work properly");
        }

        gameObject.SetActive(false); // ẩn từ đầu
    }

    public void Show(List<Player> players, int localActorNumber, Action<int> onSelected)
    {
        onTargetSelected = onSelected;
        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // Ensure panel is on top
        
        Debug.Log($"FavorTargetSelectUI: Showing panel for {players.Count} players");

        // Xoá cũ
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var player in players)
        {
            if (player.ActorNumber == localActorNumber) continue;

            GameObject btnGO = Instantiate(buttonPrefab, contentParent);
            var btnText = btnGO.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = player.NickName;

            var button = btnGO.GetComponent<Button>();
            if (button != null)
            {
                // Ensure button is interactable
                button.interactable = true;
                
                button.onClick.AddListener(() =>
                {
                    Debug.Log($"FavorTargetSelectUI: Player {player.NickName} (ID: {player.ActorNumber}) selected as target");
                    Debug.Log($"FavorTargetSelectUI: About to invoke callback for target player {player.ActorNumber}");
                    
                    onTargetSelected?.Invoke(player.ActorNumber);
                    gameObject.SetActive(false);
                    
                    Debug.Log($"FavorTargetSelectUI: Callback invoked for player {player.ActorNumber}, panel hidden");
                    
                    // Ensure UI interactions are restored
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.EnablePlayerInteractions();
                    }
                });
            }
            else
            {
                Debug.LogError("FavorTargetSelectUI: Button component not found on instantiated prefab");
            }
        }
        
        Debug.Log("FavorTargetSelectUI: Target selection buttons created and should be clickable");
    }
}

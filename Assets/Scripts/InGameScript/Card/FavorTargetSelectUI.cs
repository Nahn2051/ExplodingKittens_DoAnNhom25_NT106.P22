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

        gameObject.SetActive(false); // ẩn từ đầu
    }

    public void Show(List<Player> players, int localActorNumber, Action<int> onSelected)
    {
        onTargetSelected = onSelected;
        gameObject.SetActive(true);

        // Xoá cũ
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var player in players)
        {
            if (player.ActorNumber == localActorNumber) continue;

            GameObject btnGO = Instantiate(buttonPrefab, contentParent);
            var btnText = btnGO.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = player.NickName;

            btnGO.GetComponent<Button>().onClick.AddListener(() =>
            {
                onTargetSelected?.Invoke(player.ActorNumber);
                gameObject.SetActive(false);
            });
        }
    }
}

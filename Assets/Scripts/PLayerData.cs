﻿using UnityEngine;
using System.Collections.Generic;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance;

    [SerializeField] public string PlayerName = "Player";
    [SerializeField] public int AvatarIndex = 23;
    [SerializeField] public string RoomID;
    [SerializeField] public string UserId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}

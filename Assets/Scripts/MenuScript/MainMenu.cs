using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public AudioMixer MainAudioMixer;
    public Button StartButton;
    public TextMeshProUGUI nameText;
    public Image avatarImage;
    public AvatarImageManager avatarImageManager; // nếu bạn dùng giống ProfileScene

    public void Start()
    {
        float vol = PlayerPrefs.GetFloat("MusicVol", 0.75f); // Giá trị mặc định 0.75
        MainAudioMixer.SetFloat("MusicVol", vol);
        Debug.Log("Current volume: " + vol);
        LoadPlayerInfo();
    }
    public void LoadJoinScene() {
        SceneManager.LoadScene("JoinScene");
    }
    public void LoadProfileScene()
    {
        SceneManager.LoadScene("ProfileScene");
    }
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    public void LoadSettingsScene()
    {
        SceneManager.LoadScene("SettingsScene");
    }
    public void LoadLoginScene()
    {
        SceneManager.LoadScene("LoginScene");
    }
    private void LoadPlayerInfo()
    {
        if (PlayerData.Instance != null)
        {
            string playerName = PlayerData.Instance.PlayerName;
            int avatarIndex = PlayerData.Instance.AvatarIndex;

            if (nameText != null) nameText.text = playerName;
            if (avatarImageManager != null && avatarImage != null)
            {
                avatarImage.sprite = avatarImageManager.SetImage(avatarIndex);
            }

            Debug.Log($"🎮 MainMenu load player info: {playerName}, avatar index = {avatarIndex}");
        }
        else
        {
            Debug.LogWarning("⚠️ PlayerData.Instance is null in MainMenu!");
        }
    }
}

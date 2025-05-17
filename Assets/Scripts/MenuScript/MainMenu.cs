using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public AudioMixer MainAudioMixer;
    public Button StartButton;
    public void Start()
    {
        float vol = PlayerPrefs.GetFloat("MusicVol", 0.75f); // Giá trị mặc định 0.75
        MainAudioMixer.SetFloat("MusicVol", vol);
        Debug.Log("Current volume: " + vol);
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
}

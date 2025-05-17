using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class SettingsScript : MonoBehaviour
{
    public TMP_Dropdown GraphicsDropdown;
    public Slider MusicVol;
    public AudioMixer MainAudioMixer;
    void Start()
    {
        int currentLevel = QualitySettings.GetQualityLevel();
        GraphicsDropdown.value = currentLevel;
        Debug.Log("Start Quality: " + QualitySettings.names[currentLevel]);

        if (PlayerPrefs.HasKey("MusicVol"))
        {
            float vol = PlayerPrefs.GetFloat("MusicVol");
            MusicVol.value = vol;
            MainAudioMixer.SetFloat("MusicVol", vol);
            Debug.Log("Current volume: " + vol);
        }
    }
    public void ChangeMusicVolume()
    {
        float value = MusicVol.value;
        MainAudioMixer.SetFloat("MusicVol", value);
        PlayerPrefs.SetFloat("MusicVol", value); 
    }
    public void ChangeGraphicsQuality()
    {
        int selectedLevel = GraphicsDropdown.value;
        QualitySettings.SetQualityLevel(selectedLevel, true);
        Debug.Log("Changed to: " + QualitySettings.names[selectedLevel]);
    }
    public void LoadMainMenuScene()
    {
        SceneManager.LoadScene("Main Menu");
    }
}

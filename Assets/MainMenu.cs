using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button StartButton;
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
}

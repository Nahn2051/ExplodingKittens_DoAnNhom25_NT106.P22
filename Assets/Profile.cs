using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Profile : MonoBehaviour
{
    [Header("UI References")]
    public GameObject profilePanel;
    public Button profileButton;
    public Text userIdText;
    public InputField nameInputField;
    public Image avatarImage;
    public Sprite[] availableAvatars;
    public Button changeAvatarButton;
    public Button saveButton;

    private int currentAvatarIndex = 0;
    // Start is called before the first frame update
    void Start()
    {
        // Load saved data
        string savedName = PlayerPrefs.GetString("PlayerName", "Guest");
        int savedAvatarIndex = PlayerPrefs.GetInt("AvatarIndex", 0);
        string userId = System.Guid.NewGuid().ToString().Substring(0, 8); // Example user ID

        // Set UI
        userIdText.text = "ID: " + userId;
        nameInputField.text = savedName;
        avatarImage.sprite = availableAvatars[savedAvatarIndex];
        currentAvatarIndex = savedAvatarIndex;

        // Set up button events
        profileButton.onClick.AddListener(ToggleProfilePanel);
        changeAvatarButton.onClick.AddListener(ChangeAvatar);
        saveButton.onClick.AddListener(SaveProfile);

        profilePanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void ToggleProfilePanel()
    {
        profilePanel.SetActive(!profilePanel.activeSelf);
    }

    void ChangeAvatar()
    {
        currentAvatarIndex = (currentAvatarIndex + 1) % availableAvatars.Length;
        avatarImage.sprite = availableAvatars[currentAvatarIndex];
    }

    void SaveProfile()
    {
        PlayerPrefs.SetString("PlayerName", nameInputField.text);
        PlayerPrefs.SetInt("AvatarIndex", currentAvatarIndex);
        PlayerPrefs.Save();
    }
}

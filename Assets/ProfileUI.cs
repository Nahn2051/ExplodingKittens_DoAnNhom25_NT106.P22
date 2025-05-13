using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProfileUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text userIdText;
    public TMP_InputField nameInput;
    public Image avatarImage;
    public Sprite[] avatarOptions;
    public Button changeAvatarButton;
    public Button saveButton;

    private int currentAvatarIndex = 0;
    private string userId;

    void Start()
    {
        // Lấy dữ liệu đã lưu (nếu có), nếu chưa có thì tạo mới
        userId = PlayerPrefs.GetString("UserID", GenerateRandomUserId());
        string playerName = PlayerPrefs.GetString("PlayerName", "Guest");
        currentAvatarIndex = PlayerPrefs.GetInt("AvatarIndex", 0);

        // Hiển thị lên UI
        userIdText.text = "ID: " + userId;
        nameInput.text = playerName;
        avatarImage.sprite = avatarOptions[currentAvatarIndex];

        // Gán sự kiện cho nút
        changeAvatarButton.onClick.AddListener(OnChangeAvatar);
        saveButton.onClick.AddListener(OnSave);
    }

    string GenerateRandomUserId()
    {
        string newId = System.Guid.NewGuid().ToString().Substring(0, 8);
        PlayerPrefs.SetString("UserID", newId);
        return newId;
    }

    public void OnChangeAvatar()
    {
        currentAvatarIndex = (currentAvatarIndex + 1) % avatarOptions.Length;
        avatarImage.sprite = avatarOptions[currentAvatarIndex];
    }

    public void OnSave()
    {
        PlayerPrefs.SetString("PlayerName", nameInput.text);
        PlayerPrefs.SetInt("AvatarIndex", currentAvatarIndex);
        PlayerPrefs.Save();
        Debug.Log("Profile saved.");
    }
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}

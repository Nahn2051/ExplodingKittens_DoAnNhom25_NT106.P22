using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class ProfileManager : MonoBehaviour
{
    [Header("Save Success UI")]
    public GameObject savePanel;      // Panel hiển thị thông báo
    public TextMeshProUGUI saveText;  // Text hiển thị nội dung
    public AudioMixer MainAudioMixer;
    [Header("Dữ liệu người chơi")]
    public TMP_InputField nameInput;  // Ô nhập tên người chơi
    public Button avatarButton;
    public GameObject avatarContain;
    public Image Avatar;
    public AvatarImageManager avatarImageManager;
    void Start()
    {
        savePanel.SetActive(false); // Ẩn bảng lúc đầu
        SetupUIListeners();
        if (PlayerData.Instance == null)
        {
            Debug.LogError("Không tìm thấy PlayerData singleton! Đang tạo mới.");
            GameObject playerDataObj = new GameObject("PlayerData");
            playerDataObj.AddComponent<PlayerData>();
        }

        // Tự động điền UI với dữ liệu người chơi hiện có
        InitializeUIFromPlayerData();
        float vol = PlayerPrefs.GetFloat("MusicVol", 0.75f); // Giá trị mặc định 0.75
        MainAudioMixer.SetFloat("MusicVol", vol);
    }

    public void OnSaveButtonClick()
    {
        // 1. Lưu thông tin tên và avatar
        string playerName = nameInput.text;
        int avatarIndex = PlayerData.Instance != null ? PlayerData.Instance.AvatarIndex : 0;

        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetInt("AvatarIndex", avatarIndex);
        PlayerPrefs.Save();

        // Đồng bộ với PlayerData singleton (nếu có)
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.PlayerName = playerName;
            PlayerData.Instance.AvatarIndex = avatarIndex;
        }

        // 2. Hiện thông báo
        ShowSaveMessage("Save successfully!");
    }
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
    private void InitializeUIFromPlayerData()
    {
        // Tự động điền tên nếu có
        if (PlayerData.Instance != null)
        {
            if (!string.IsNullOrEmpty(PlayerData.Instance.PlayerName))
                nameInput.text = PlayerData.Instance.PlayerName;

            SetAvatarImage(PlayerData.Instance.AvatarIndex);
        }
    }
    void ShowSaveMessage(string message)
    {
        saveText.text = message;
        savePanel.SetActive(true);
        StartCoroutine(HideSaveMessageAfterDelay(2f)); // 2 giây sau ẩn
    }

    IEnumerator HideSaveMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        savePanel.SetActive(false);
    }

    public void OnAvatarClicked()
    {
        if (avatarImageManager != null && avatarContain != null)
        {
            avatarContain.SetActive(!avatarContain.activeSelf);
        }
    }

    public void SetAvatarImage(int index)
    {
        if (avatarImageManager != null && Avatar != null)
        {
            Debug.Log($"Profile set index = {index}");
            Avatar.sprite = avatarImageManager.SetImage(index);

            if (PlayerData.Instance != null)
                PlayerData.Instance.AvatarIndex = index;

            if (avatarContain != null)
                avatarContain.SetActive(false);
        }
    }

    private void SetupUIListeners()
    {
        avatarButton.onClick.AddListener(OnAvatarClicked);
    }
}
//using System.Collections;
//using UnityEngine;
//using TMPro;
//using UnityEngine.UI;
//using UnityEngine.SceneManagement;

//public class ProfileManager : MonoBehaviour
//{
//    [Header("Save Success UI")]
//    public GameObject savePanel;
//    public TextMeshProUGUI saveText;

//    [Header("Player Data UI")]
//    public TMP_InputField nameInput;
//    public Button avatarButton;
//    public GameObject avatarContain;
//    public Image Avatar;
//    public AvatarImageManager avatarImageManager;

//    void Start()
//    {
//        savePanel.SetActive(false);
//        SetupUIListeners();

//        if (PlayerData.Instance == null)
//        {
//            GameObject playerDataObj = new GameObject("PlayerData");
//            playerDataObj.AddComponent<PlayerData>();
//        }

//        InitializeUIFromPlayerData();
//    }

//    private void InitializeUIFromPlayerData()
//    {
//        if (PlayerData.Instance != null)
//        {
//            nameInput.text = PlayerData.Instance.PlayerName;
//            SetAvatarImage(PlayerData.Instance.AvatarIndex);
//        }
//    }

//    public void OnSaveButtonClick()
//    {
//        if (PlayerData.Instance != null)
//        {
//            PlayerData.Instance.PlayerName = nameInput.text;
//            ShowSaveMessage("Saved successfully!");
//        }
//    }

//    public void LoadMainMenu()
//    {
//        SceneManager.LoadScene("Main Menu"); // đổi tên đúng với scene join của bạn
//    }

//    void ShowSaveMessage(string message)
//    {
//        saveText.text = message;
//        savePanel.SetActive(true);
//        StartCoroutine(HideSaveMessageAfterDelay(2f));
//    }

//    IEnumerator HideSaveMessageAfterDelay(float delay)
//    {
//        yield return new WaitForSeconds(delay);
//        savePanel.SetActive(false);
//    }

//    public void OnAvatarClicked()
//    {
//        if (avatarContain != null)
//            avatarContain.SetActive(!avatarContain.activeSelf);
//    }

//    public void SetAvatarImage(int index)
//    {
//        if (avatarImageManager != null && Avatar != null)
//        {
//            Avatar.sprite = avatarImageManager.SetImage(index);

//            if (PlayerData.Instance != null)
//                PlayerData.Instance.AvatarIndex = index;

//            if (avatarContain != null)
//                avatarContain.SetActive(false);
//        }
//    }
//    private void SetupUIListeners()
//    {
//        avatarButton.onClick.AddListener(OnAvatarClicked);
//    }
//}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Fusion;
using TMPro;

public class JoinSceneManager : MonoBehaviour
{
    public Image Avatar;
    public AvatarImageManager avatarImageManager;
    public GameObject avatarContain;
    public TMP_InputField nameInput;

    public TMP_InputField roomIdInput;
    public Button avatarButton;
    public Button hostButton;
    public TMP_Text hostRoomFailed;
    public Button joinButton;
    public TMP_Text noRoomFoundText;
    public Button exitButton;
    public NetworkRunner runner;

    void Awake()
    {
        CheckAndLoadNetworkRunner();
    }
    private void CheckAndLoadNetworkRunner()
    {
        runner = FindObjectOfType<NetworkRunner>();
        if (runner == null)
        {
            GameObject runnerObject = new GameObject("NetworkRunner");
            runnerObject.AddComponent<NetworkRunner>();
            runnerObject.AddComponent<NetworkSceneManagerDefault>();
            runnerObject.AddComponent<NetworkRunnerScript>();
            runner = FindObjectOfType<NetworkRunner>();
        }
    }
    private void Start()
    {
        avatarButton.onClick.AddListener(OnAvatarClicked);
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        exitButton.onClick.AddListener(OnExitClicked);

        // Auto-fill name
        if (!string.IsNullOrEmpty(PlayerData.Instance.PlayerName))
            nameInput.text = PlayerData.Instance.PlayerName;
        SetAvatarImage(PlayerData.Instance.AvatarIndex);
    }

    private void OnAvatarClicked()
    {
        if (avatarImageManager != null)
        {
            avatarContain.SetActive(!avatarContain.activeSelf);
        }
    }
    public void SetAvatarImage(int index)
    {
        Avatar.sprite = avatarImageManager.SetImage(index);
        PlayerData.Instance.AvatarIndex = index;
        avatarContain.SetActive(false);
    }

    private async void OnHostClicked()
    {
        CheckAndLoadNetworkRunner();
        hostButton.interactable = false;
        string playerName = nameInput.text.Trim();
        if (string.IsNullOrEmpty(playerName)) playerName = PlayerData.Instance.PlayerName;

        string roomID = Random.Range(1000, 9999).ToString();

        PlayerData.Instance.PlayerName = playerName;
        PlayerData.Instance.RoomID = roomID;

        runner.ProvideInput = true;

        var startArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = roomID,
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>() ?? runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        var result = await runner.StartGame(startArgs);

        if (result.Ok)
        {
            SceneManager.LoadScene("Lobby");
        }
        else
        {
            Debug.LogError("Failed to start host: " + result.ShutdownReason);
            hostRoomFailed.gameObject.SetActive(true);
            hostButton.interactable = true;
        }
    }

    private async void OnJoinClicked()
    {
        CheckAndLoadNetworkRunner();
        joinButton.interactable = false;
        noRoomFoundText.gameObject.SetActive(false);
        string playerName = nameInput.text.Trim();
        string roomID = roomIdInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
            playerName = PlayerData.Instance.PlayerName;

        if (string.IsNullOrEmpty(roomID))
        {
            joinButton.interactable = true;
            return;
        }

        PlayerData.Instance.PlayerName = playerName;
        PlayerData.Instance.RoomID = roomID;

        runner.ProvideInput = true;

        var startArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = roomID,
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>() ?? runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        var result = await runner.StartGame(startArgs);

        if (result.Ok)
        {
            SceneManager.LoadScene("Lobby");
        }
        else
        {
            Debug.LogError("Failed to join room: " + result.ShutdownReason);
            noRoomFoundText.gameObject.SetActive(true);
            joinButton.interactable = true;
        }

    }

    private void OnExitClicked()
    {
        SceneManager.LoadScene("Main Menu");
    }
}

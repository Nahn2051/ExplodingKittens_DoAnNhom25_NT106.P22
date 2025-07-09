using UnityEngine;
using UnityEngine.UI;
using Photon.Voice.Unity;
using System.Collections;
using Photon.Pun;

[RequireComponent(typeof(Button))]
public class MicButton : MonoBehaviour
{
    public Sprite micOnSprite;
    public Sprite micOffSprite;
    public Image buttonImage;

    private Recorder recorder;
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ToggleMic); // Gán sự kiện onClick ở đây
    }

    void Start()
    {
        StartCoroutine(WaitForRecorder());
    }

    private IEnumerator WaitForRecorder()
    {
        yield return new WaitUntil(() => FindObjectOfType<Recorder>() != null);

        recorder = FindObjectOfType<Recorder>();
        if (recorder == null)
        {
            Debug.LogError("Không tìm thấy Recorder trong scene!");
            yield break;
        }

        UpdateIcon();
    }

    public void ToggleMic()
    {
        if (recorder == null) return;

        recorder.TransmitEnabled = !recorder.TransmitEnabled;
        Debug.Log($"[MicButton] Trạng thái mic sau khi toggle: {(recorder.TransmitEnabled ? "BẬT" : "TẮT")}");
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (buttonImage == null) return;

        buttonImage.sprite = recorder.TransmitEnabled ? micOnSprite : micOffSprite;
    }
}

using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour
{
    public AudioMixer audioMixer;

    private static MusicManager instance;

    void Awake()
    {
        // Đảm bảo chỉ có một bản duy nhất
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // ✅ Không bị hủy khi chuyển scene

            // Đọc âm lượng từ PlayerPrefs
            if (PlayerPrefs.HasKey("MusicVol"))
            {
                float vol = PlayerPrefs.GetFloat("MusicVol");
                audioMixer.SetFloat("MusicVol", vol);
            }

            // Bắt đầu phát nhạc
            GetComponent<AudioSource>().Play();
        }
        else
        {
            Destroy(gameObject); // Nếu đã có, hủy bản mới
        }
    }
}

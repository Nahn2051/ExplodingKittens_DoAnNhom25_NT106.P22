using UnityEngine;
using Photon.Pun;

public class VoiceSetupManager : MonoBehaviourPunCallbacks
{
    public GameObject voicePlayerPrefab;

    private void Start()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.Instantiate(voicePlayerPrefab.name, Vector3.zero, Quaternion.identity);
        }
    }
}

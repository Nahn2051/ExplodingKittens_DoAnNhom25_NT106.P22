using Photon.Pun;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using UnityEngine;

[RequireComponent(typeof(PhotonView), typeof(PhotonVoiceView), typeof(Recorder))]
public class VoicePlayer : MonoBehaviourPun
{
    void Start()
    {
        if (photonView.IsMine)
        {
            GetComponent<Recorder>().TransmitEnabled = true;
        }
    }
}

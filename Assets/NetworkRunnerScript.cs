using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class NetworkRunnerScript : MonoBehaviourPunCallbacks
{
    private void Awake()
    {
        if (FindObjectsOfType<NetworkRunnerScript>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }
}

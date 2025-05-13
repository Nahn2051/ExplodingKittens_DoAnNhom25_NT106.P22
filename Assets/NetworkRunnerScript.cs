using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using static Unity.Collections.Unicode;

public class NetworkRunnerScript : MonoBehaviour
{
    private void Awake()
    {
        if (FindObjectsOfType<NetworkRunner>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }
}

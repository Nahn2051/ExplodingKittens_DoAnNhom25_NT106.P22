using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Rules : MonoBehaviour
{
    public GameObject panelToToggle;

    // Hàm này sẽ được gọi bởi nút bấm
    public void TogglePanel()
    {
        if (panelToToggle != null)
        {
            // Bật panel nếu nó đang tắt, và tắt nếu nó đang bật
            panelToToggle.SetActive(!panelToToggle.activeSelf);
        }
    }
}

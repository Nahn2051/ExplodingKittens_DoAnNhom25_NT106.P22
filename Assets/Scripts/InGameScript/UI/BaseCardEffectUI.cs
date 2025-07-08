using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class BaseCardEffectUI : MonoBehaviour
{
    [Header("Base UI Components")]
    [SerializeField] protected GameObject mainPanel;
    [SerializeField] protected TMP_Text titleText;
    [SerializeField] protected TMP_Text descriptionText;
    [SerializeField] protected Button cancelButton;
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected Animator panelAnimator;
    
    [Header("Animation Settings")]
    [SerializeField] protected float fadeInDuration = 0.5f;
    [SerializeField] protected float fadeOutDuration = 0.3f;
    
    protected bool isUIActive = false;
    
    protected virtual void Start()
    {
        // Ẩn UI ban đầu
        if (mainPanel != null) mainPanel.SetActive(false);
        
        // Thiết lập cancel button
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
            
        // Thiết lập canvas group
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
    
    protected virtual void OnCancelClicked()
    {
        HideUI();
    }
    
    public virtual void ShowUI()
    {
        if (isUIActive) return;
        
        isUIActive = true;
        
        if (mainPanel != null)
            mainPanel.SetActive(true);
            
        // Animation show
        if (panelAnimator != null)
        {
            panelAnimator.SetTrigger("Show");
        }
        else
        {
            StartCoroutine(FadeIn());
        }
        
        OnUIShown();
    }
    
    public virtual void HideUI()
    {
        if (!isUIActive) return;
        
        // Animation hide
        if (panelAnimator != null)
        {
            panelAnimator.SetTrigger("Hide");
            // Giả định animation sẽ gọi HideUIComplete() khi hoàn thành
        }
        else
        {
            StartCoroutine(FadeOut());
        }
        
        OnUIHidden();
    }
    
    protected virtual void HideUIComplete()
    {
        isUIActive = false;
        
        if (mainPanel != null)
            mainPanel.SetActive(false);
            
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
    
    protected virtual IEnumerator FadeIn()
    {
        if (canvasGroup != null)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
    }
    
    protected virtual IEnumerator FadeOut()
    {
        if (canvasGroup != null)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
        }
        
        HideUIComplete();
    }
    
    protected virtual void SetTitle(string title)
    {
        if (titleText != null)
            titleText.text = title;
    }
    
    protected virtual void SetDescription(string description)
    {
        if (descriptionText != null)
            descriptionText.text = description;
    }
    
    // Abstract methods để override
    protected virtual void OnUIShown() { }
    protected virtual void OnUIHidden() { }
    
    // Utility methods
    protected void ClearContainer(Transform container)
    {
        if (container != null)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    protected Button CreateButton(Transform parent, Button prefab, string text, System.Action onClick)
    {
        if (prefab != null && parent != null)
        {
            Button button = Instantiate(prefab, parent);
            if (button.GetComponentInChildren<TMP_Text>() != null)
            {
                button.GetComponentInChildren<TMP_Text>().text = text;
            }
            button.onClick.AddListener(() => onClick?.Invoke());
            return button;
        }
        return null;
    }
}

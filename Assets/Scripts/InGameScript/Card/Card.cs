using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Photon.Pun;

public class Card : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Card Data")]
    public CardData data;

    [HideInInspector] private Canvas canvas;
    private Image imageComponent;
    private Vector3 offset;

    [Header("Movement")]
    [SerializeField] private float moveSpeedLimit = 50f;

    [Header("Selection")]
    [HideInInspector] public bool selected = false;
    [SerializeField] public float selectionOffset = 50f;
    private float pointerDownTime;
    private float pointerUpTime;

    [Header("States")]
    [HideInInspector] public bool isHovering = false;
    [HideInInspector] public bool isDragging = false;
    [HideInInspector] public bool wasDragged = false;
    [HideInInspector] public bool isPlayed = false;

    [Header("Events")]
    [HideInInspector] public UnityEvent<Card> PointerEnterEvent;
    [HideInInspector] public UnityEvent<Card> PointerExitEvent;
    [HideInInspector] public UnityEvent<Card, bool> PointerUpEvent;
    [HideInInspector] public UnityEvent<Card> PointerDownEvent;
    [HideInInspector] public UnityEvent<Card> BeginDragEvent;
    [HideInInspector] public UnityEvent<Card> EndDragEvent;
    [HideInInspector] public UnityEvent<Card, bool> SelectEvent;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        imageComponent = GetComponent<Image>();
        if (data != null && data.sprite != null)
        {
            imageComponent.sprite = data.sprite;
        }
    }

    private void Update()
    {
        ClampPosition();

        if (isDragging)
        {
            Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            float distance = Vector2.Distance(transform.position, targetPosition);
            Vector2 velocity = direction * Mathf.Min(moveSpeedLimit, distance / Time.deltaTime);
            transform.Translate(velocity * Time.deltaTime);
        }
    }

    public void Setup(CardData cardData)
    {
        data = cardData;
        if (imageComponent != null)
            imageComponent.sprite = data.sprite;
    }

    private void ClampPosition()
    {
        Vector2 screenBounds = Camera.main.ScreenToWorldPoint(
            new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z)
        );
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -screenBounds.x, screenBounds.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -screenBounds.y, screenBounds.y);
        transform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlayed) return;
        
        // Kiểm tra nếu exploding đang diễn ra
        if (CardEffectManager.IsExplodingInProgress)
        {
            // Chỉ cho phép người chơi bị dính exploding kéo thẻ Defuse
            if (CardEffectManager.ExplodingPlayerId != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                // Không phải người chơi bị dính exploding -> không cho kéo thẻ
                Debug.Log("Không thể kéo thẻ khi có người chơi đang xử lý Exploding!");
                return;
            }
            else if (data.effect != "Defuse")
            {
                // Người chơi bị dính exploding nhưng không phải thẻ Defuse -> không cho kéo
                Debug.Log("Chỉ có thể kéo thẻ Defuse khi bạn bị dính Exploding!");
                return;
            }
        }
        
        // QUAN TRỌNG: Cho phép kéo tất cả các lá bài bất kể lượt
        // Logic kiểm tra lượt và điều kiện sẽ được thực hiện khi thả vào PlayCardZone
        // Chỉ có exception duy nhất: khi exploding chỉ cho phép kéo Defuse
        
        BeginDragEvent?.Invoke(this);
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = mousePosition - (Vector2)transform.position;
        isDragging = true;
        wasDragged = true;
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        imageComponent.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPlayed) return;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlayed) return;

        EndDragEvent?.Invoke(this);
        isDragging = false;
        canvas.GetComponent<GraphicRaycaster>().enabled = true;
        imageComponent.raycastTarget = true;

        // Tạo list để chứa kết quả raycast UI
        List<RaycastResult> results = new List<RaycastResult>();
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            // Kiểm tra PlayZone - CHỈ handle nếu không có IDropHandler
            if (result.gameObject.CompareTag("PlayZone"))
            {
                // Kiểm tra xem object có IDropHandler không
                var dropHandler = result.gameObject.GetComponent<IDropHandler>();
                if (dropHandler == null)
                {
                    // Chỉ gọi PlayCard nếu KHÔNG có IDropHandler (fallback)
                    PlayCardZone playZone = result.gameObject.GetComponent<PlayCardZone>();
                    if (playZone != null)
                    {
                        playZone.PlayCard(this);
                        break;
                    }
                }
                else
                {
                    // Có IDropHandler - OnDrop sẽ handle, không cần gọi PlayCard
                    break;
                }
            }
            // Kiểm tra DefuseZone
            else if (result.gameObject.CompareTag("DefuseZone"))
            {
                // Trong trường hợp exploding, chỉ cho phép thẻ Defuse được thả vào DefuseZone
                if (CardEffectManager.IsExplodingInProgress && 
                    CardEffectManager.ExplodingPlayerId == PhotonNetwork.LocalPlayer.ActorNumber && 
                    data.effect == "Defuse")
                {
                    // Gọi trực tiếp method từ CardEffectManager
                    if (CardEffectManager.Instance != null)
                    {
                        CardEffectManager.Instance.OnDefuseCardDropped(this);
                    }
                    break;
                }
            }
        }

        StartCoroutine(ResetDragFlag());
    }

    private IEnumerator ResetDragFlag()
    {
        yield return new WaitForEndOfFrame();
        wasDragged = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isPlayed) return;
        PointerEnterEvent?.Invoke(this);
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPlayed) return;
        PointerExitEvent?.Invoke(this);
        isHovering = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isPlayed) return;
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        PointerDownEvent?.Invoke(this);
        pointerDownTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isPlayed) return;
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        pointerUpTime = Time.time;
        bool isLongPress = (pointerUpTime - pointerDownTime > 0.2f);
        PointerUpEvent?.Invoke(this, isLongPress);

        if (isLongPress || wasDragged)
            return;

        selected = !selected;
        SelectEvent?.Invoke(this, selected);

        if (selected)
        {
            transform.localPosition += transform.up * selectionOffset;
            Debug.Log($"Card {data.effect} SELECTED");
        }
        else
        {
            transform.localPosition = Vector3.zero;
            Debug.Log($"Card {data.effect} DESELECTED");
            
            // Notify PlayCardZone to cleanup deselected cards
            PlayCardZone playZone = FindObjectOfType<PlayCardZone>();
            if (playZone != null)
            {
                playZone.ForceCleanupSelection();
            }
        }
    }

    public void Deselect()
    {
        if (!selected) return;
        
        Debug.Log($"Deselect: Card {data.effect} being deselected");
        selected = false;
        transform.localPosition = Vector3.zero;
        
        // Notify PlayCardZone to cleanup
        PlayCardZone playZone = FindObjectOfType<PlayCardZone>();
        if (playZone != null)
        {
            playZone.ForceCleanupSelection();
        }
    }

    public int SiblingAmount()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.parent.childCount - 1 : 0;
    }

    public int ParentIndex()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.GetSiblingIndex() : 0;
    }

    public float NormalizedPosition()
    {
        if (!transform.parent.CompareTag("Slot")) return 0f;
        int index = ParentIndex();
        int total = transform.parent.parent.childCount - 1;
        return Mathf.InverseLerp(0, total, index);
    }
    
    // Method to enable or disable card interaction
    public void SetInteractable(bool interactable)
    {
        // Enable/disable the Graphic Raycaster component to control interaction
        GraphicRaycaster raycaster = GetComponentInParent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = interactable;
        }
        
        // Visually indicate interaction state
        if (imageComponent != null)
        {
            Color newColor = imageComponent.color;
            newColor.a = interactable ? 1.0f : 0.7f;
            imageComponent.color = newColor;
        }
    }
}

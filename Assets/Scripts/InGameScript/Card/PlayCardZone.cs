using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;

public class PlayCardZone : MonoBehaviour
{
    public List<Card> playedCards = new List<Card>();
    public CardHolder handHolder;

    public void PlayCard(Card card)
    {
        if (playedCards.Contains(card)) return;

        if (GameManager.Instance != null && GameManager.Instance.IsLocalPlayerTurn())
        {
            playedCards.Add(card);
            card.isDragging = false;
            card.isHovering = false;
            card.selected = false;

            card.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            card.GetComponent<CanvasGroup>().blocksRaycasts = false;
            handHolder.RemoveCard(card);

            if (CardManager.Instance != null)
            {
                CardManager.Instance.PlayCard(card, Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
        else
        {
            Debug.Log("Không thể chơi bài - chưa đến lượt của bạn!");
        }
    }

    public void AddPlayedCard(Card card, int playerActorNumber)
    {
        if (playedCards.Contains(card)) return;

        playedCards.Add(card);
        card.isDragging = false;
        card.isHovering = false;
        card.selected = false;

        float randomAngle = Random.Range(-20f, 20f);

        card.transform.SetParent(transform);
        card.transform.SetAsLastSibling();
        card.transform.localScale = Vector3.one * 0.4f;

        card.transform.DOLocalMove(Vector3.zero, 0.25f).SetEase(Ease.OutBack);
        card.transform.DORotate(new Vector3(0, 0, randomAngle), 0.25f);

        card.GetComponent<UnityEngine.UI.Image>().enabled = true;

        Debug.Log($"Người chơi {playerActorNumber} đã chơi thẻ {card.data.cardName}: {card.data.effect}");

        StartCoroutine(ShowCardEffectAnimation(card.data.effect));
    }

    private IEnumerator ShowCardEffectAnimation(string effectType)
    {
        yield return new WaitForSeconds(1f);
    }

    public void ClearPlayZone()
    {
        foreach (Card card in playedCards)
        {
            Destroy(card.transform.parent.gameObject);
        }
        playedCards.Clear();
    }
}

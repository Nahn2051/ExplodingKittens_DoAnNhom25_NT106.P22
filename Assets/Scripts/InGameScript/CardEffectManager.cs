using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

public class CardEffectManager : MonoBehaviourPunCallbacks
{
    public static CardEffectManager Instance;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    // Xử lý kích hoạt hiệu ứng thẻ bài
    public void ActivateCardEffect(string effectType, int cardId)
    {
        // Đảm bảo chỉ có người chơi đến lượt mới kích hoạt hiệu ứng
        if (GameManager.Instance != null)
        {
            photonView.RPC("RPC_ActivateCardEffect", RpcTarget.All, effectType, cardId, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }
    
    [PunRPC]
    private void RPC_ActivateCardEffect(string effectType, int cardId, int activatingPlayerId)
    {
        Debug.Log($"Hiệu ứng '{effectType}' được kích hoạt bởi người chơi {activatingPlayerId}");
        
        // Xử lý các hiệu ứng khác nhau dựa trên loại thẻ
        switch (effectType)
        {
            case "Exploding":
                HandleExplodingEffect(activatingPlayerId);
                break;
                
            case "Defuse":
                HandleDefuseEffect(activatingPlayerId);
                break;
                
            case "Attack":
                HandleAttackEffect(activatingPlayerId);
                break;
                
            case "Favor":
                HandleFavorEffect(activatingPlayerId);
                break;
                
            case "Nope":
                HandleNopeEffect(activatingPlayerId);
                break;
                
            case "Shuffle":
                HandleShuffleEffect(activatingPlayerId);
                break;
                
            case "Skip":
                HandleSkipEffect(activatingPlayerId);
                break;
                
            case "SeeTheFuture":
                HandleSeeTheFutureEffect(activatingPlayerId);
                break;
                
            default:
                Debug.LogWarning($"Hiệu ứng '{effectType}' không được định nghĩa");
                break;
        }
    }
    
    // Các hàm xử lý hiệu ứng
    
    private void HandleExplodingEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Exploding từ người chơi {playerId}");
        // TODO: Implement khi game phát triển thêm
    }
    
    private void HandleDefuseEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Defuse từ người chơi {playerId}");
        // TODO: Implement khi game phát triển thêm
    }
    
    private void HandleAttackEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Attack từ người chơi {playerId}");
        // TODO: Implement khi game phát triển thêm
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerId)
        {
            GameManager.Instance.ProcessAttackPlayed();
        }
    }

    private void HandleFavorEffect(int playerId)
    {
        Debug.Log($"[Favor] Xử lý hiệu ứng Favor từ player {playerId}");

        if (PhotonNetwork.LocalPlayer.ActorNumber != playerId)
        {
            Debug.Log("[Favor] Đây không phải lượt của mình.");
            return;
        }

        if (FavorTargetSelectUI.Instance == null)
        {
            Debug.LogError("FavorTargetSelectUI.Instance vẫn null!");
            return;
        }

        FavorTargetSelectUI.Instance.Show(
            GameManager.Instance.playerList,
            PhotonNetwork.LocalPlayer.ActorNumber,
            (targetPlayerId) =>
            {
                Debug.Log("[Favor] Đã chọn người chơi có ID: " + targetPlayerId);
                // Gửi RPC tiếp theo ở đây
                photonView.RPC("RPC_RequestFavorCard", RpcTarget.All, playerId, targetPlayerId);
            }
        );
    }


    private void HandleNopeEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Nope từ người chơi {playerId}");
        // TODO: Implement khi game phát triển thêm
    }
    
    private void HandleShuffleEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Shuffle từ người chơi {playerId}");
        // TODO: Implement khi game phát triển thêm
        // Yêu cầu Master Client xáo bài
        CardManager.Instance.PhotonView.RPC("RPC_RequestShuffle", RpcTarget.MasterClient);
    }
    
    private void HandleSkipEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Skip từ người chơi {playerId}");
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerId)
        {
            GameManager.Instance.ProcessSkipPlayed();
        }
    }
    
    private void HandleSeeTheFutureEffect(int activatingPlayerId)
    {
        Debug.Log($"Xử lý hiệu ứng SeeTheFuture từ người chơi {activatingPlayerId}");
        // TODO: Implement khi game phát triển thêm
        // Chỉ người chơi đã kích hoạt hiệu ứng mới gửi yêu cầu đến MasterClient lấy 3 lá bài trên cùng của bộ bài
        if (PhotonNetwork.LocalPlayer.ActorNumber == activatingPlayerId)
        {
            CardManager.Instance.PhotonView.RPC("RPC_RequestSeeTheFuture", RpcTarget.MasterClient, activatingPlayerId);
        }
    }


    [PunRPC]
    private void RPC_ReceiveFutureCards(int[] spriteIndexes)
    {
        Debug.Log("You see the future! Top cards are: " + string.Join(", ", spriteIndexes));

        //Gọi UI để hiển thị các lá bài
        if (SeeTheFutureUI.Instance != null)
        {
            SeeTheFutureUI.Instance.ShowFutureCards(spriteIndexes);
        }
    }
    [PunRPC]
    private void RPC_RequestFavorCard(int fromPlayerId, int toPlayerId)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == toPlayerId)
        {
            var cards = CardHolder.Instance.Cards.Select(c => c.data).ToList();

            FavorGiveCardUI.Instance.Show(cards, (selectedCardName) =>
            {
                CardData selectedCard = cards.FirstOrDefault(c => c.cardName == selectedCardName);
                if (selectedCard == null)
                {
                    Debug.LogError("❌ Không tìm thấy cardData với tên: " + selectedCardName);
                    return;
                }

                int spriteIndex = CardManager.Instance.GetSpriteIndex(selectedCard.sprite);
                photonView.RPC("RPC_ReceiveFavorCardByData", RpcTarget.All,
                    fromPlayerId,
                    toPlayerId,
                    selectedCard.cardName,
                    spriteIndex,
                    selectedCard.effect);
            });
        }
    }
    [PunRPC]
    private void RPC_ReceiveFavorCardByData(int fromPlayerId, int toPlayerId, string cardName, int spriteIndex, string effect)
    {
        // Người bị yêu cầu: xoá bài
        if (PhotonNetwork.LocalPlayer.ActorNumber == toPlayerId)
        {
            if (CardHolder.Instance != null)
            {
                CardHolder.Instance.RemoveCardByName(cardName);
                CardHolder.Instance.ArrangeCards();
                GameManager.Instance?.UpdatePlayerCardCount();
                Debug.Log($"❌ Người chơi {toPlayerId} đã đưa lá {cardName}");
            }
        }

        // Người yêu cầu: nhận bài
        if (PhotonNetwork.LocalPlayer.ActorNumber == fromPlayerId)
        {
            CardData cardData = new CardData
            {
                cardName = cardName,
                sprite = CardManager.Instance.allCardSprites[spriteIndex],
                effect = effect
            };

            if (CardHolder.Instance != null)
            {
                CardHolder.Instance.AddCard(CardManager.Instance.cardPrefab, cardData);
                Debug.Log($"🎁 Người chơi {fromPlayerId} đã nhận được lá {cardName}");
            }
        }
    }
}
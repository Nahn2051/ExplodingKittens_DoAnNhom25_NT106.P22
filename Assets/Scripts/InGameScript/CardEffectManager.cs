using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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
    }
    
    private void HandleFavorEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Favor từ người chơi {playerId}");
        // TODO: Implement khi game phát triển thêm
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
        
        if (PhotonNetwork.IsMasterClient)
        {
            // Xáo bộ bài
            if (CardManager.Instance != null)
            {
                photonView.RPC("RPC_ShuffleDeck", RpcTarget.All);
            }
        }
    }
    
    private void HandleSkipEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Skip từ người chơi {playerId}");
        
        if (GameManager.Instance != null && PhotonNetwork.IsMasterClient)
        {
            // Tìm người chơi tiếp theo
            int currentTurnIndex = GameManager.Instance.GetCurrentTurnIndex();
            int nextPlayerIndex = (currentTurnIndex + 1) % PhotonNetwork.CurrentRoom.PlayerCount;
            
            // Chuyển đến lượt tiếp theo (bỏ qua một lượt)
            nextPlayerIndex = (nextPlayerIndex + 1) % PhotonNetwork.CurrentRoom.PlayerCount;
            GameManager.Instance.StartTurn(nextPlayerIndex);
        }
    }
    
    private void HandleSeeTheFutureEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng SeeTheFuture từ người chơi {playerId}");
        // TODO: Implement khi game phát triển thêm
    }
    
    [PunRPC]
    private void RPC_ShuffleDeck()
    {
        if (CardManager.Instance != null)
        {
            Debug.Log("Xáo trộn bộ bài");
            // TODO: Implement khi game phát triển thêm
        }
    }
} 
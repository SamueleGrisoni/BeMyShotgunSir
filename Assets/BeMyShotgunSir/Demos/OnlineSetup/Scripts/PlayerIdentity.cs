using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[System.Serializable]
public struct PlayerInfo
{
    public string PlayerName;
    public Color PlayerColor;

    public PlayerInfo(string playerName, Color playerColor)
    {
        PlayerName = playerName;
        PlayerColor = playerColor;
    }
}
public class PlayerIdentity : NetworkBehaviour
{
    private readonly SyncVar<int> _playerId = new(new SyncTypeSettings(WritePermission.ServerOnly));
    private readonly SyncVar<PlayerInfo> _playerInfo = new(new SyncTypeSettings(WritePermission.ClientUnsynchronized, ReadPermission.ExcludeOwner));

    [ServerRpc]
    private void SetPlayerInfo_ServerRpc(PlayerInfo info) => _playerInfo.Value = info;

    [Client]
    private void UpdateMyInfo(string playerName, Color playerColor)
    {
        if (!IsOwner)
            return;
        var info = new PlayerInfo(playerName, playerColor);
        _playerInfo.Value = info;
        SetPlayerInfo_ServerRpc(info);
    }

    [Server]
    public void Initialize(int playerId, string playerName, Color playerColor)
    {
        _playerId.Value = playerId;
        _playerInfo.Value = new PlayerInfo(playerName, playerColor);
    }
}

using System;
using System.Collections.Generic;
using BeMyShotgunSir.Core;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{

    public interface ILobbyNetData : INetData
    {
        LobbyInfo LobbyInfo { get; }
        int PlayerCount { get; }
        Dictionary<int, PlayerLobbyState> PlayerStates { get; }
    }

    public interface ILobbyData : IData
    {

    }

    public interface ILobbyDataView : ILobbyData, ILobbyNetData
    {
        event Action OnLobbyInfoChanged;
        event Action OnLobbyIPChanged;
        event Action OnPlayerCountChanged;
        event Action OnPlayerStatesChanged;
    }
}

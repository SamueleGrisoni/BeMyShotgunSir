using System;
using BeMyShotgunSir.Core;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public interface ILobbyData : IData
    {
        string LobbyIP { get; }
        int PlayerCount { get; }
        string[] PlayerNames { get; }
    }

    public interface ILobbyDataView : ILobbyData
    {
        event Action OnLobbyIPChanged;
        event Action OnPlayerCountChanged;
        event Action OnPlayerNamesChanged;
    }
}

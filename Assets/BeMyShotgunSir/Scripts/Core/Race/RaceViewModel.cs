using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object.Synchronizing;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public interface IRaceNetData : INetData
    {
        Dictionary<int, RacePlayerState> PlayerStates { get; }
        Dictionary<int, RaceTeamData> TeamData { get; }
        List<int> Leaderboard { get; }
    }
    public interface IRaceData : IData { }
    public interface IRaceDataView : IDataView, IRaceData, IRaceNetData
    {
        ILobbyDataView LobbyDataView { get; }
        event Action OnRacePlayerStatesChanged;
        event Action OnRaceTeamDataChanged;
        event Action OnRaceLeaderboardChanged;
    }

    public struct RaceNetDataSnapshot : IRaceNetData
    {
        public Dictionary<int, RacePlayerState> PlayerStates { get; set; }
        public Dictionary<int, RaceTeamData> TeamData { get; set; }
        public List<int> Leaderboard { get; set; }

        public RaceNetDataSnapshot(Dictionary<int, RacePlayerState> playerStates, Dictionary<int, RaceTeamData> teamData, List<int> leaderboard)
        {
            PlayerStates = new Dictionary<int, RacePlayerState>(playerStates);
            TeamData = new Dictionary<int, RaceTeamData>(teamData);
            Leaderboard = new List<int>(leaderboard);
        }
    }

    public class RaceViewModel : ViewModel<IRaceData, IRaceNetData>, IRaceDataView
    {
        private bool _log = true;
        private LobbyViewModel _lobbyViewModel;
        public ILobbyDataView LobbyDataView => _lobbyViewModel;

        public event Action OnLobbyInfoChanged;
        public event Action OnLobbyIPChanged;
        public event Action OnPlayerCountChanged;
        public event Action OnPlayerStatesChanged;
        public event Action OnRacePlayerStatesChanged;
        public event Action OnRaceTeamDataChanged;
        public event Action OnRaceLeaderboardChanged;

        public override void InitData(IRaceData data = null)
        {
            Log.DLazy(() => "Initializing RaceViewModel data.", this, _log);
            PlayerStates = new Dictionary<int, RacePlayerState>();
            TeamData = new Dictionary<int, RaceTeamData>();
            Leaderboard = new List<int>();
        }

        public void InitData(LobbyViewModel lobbyViewModel, IRaceData raceData = null)
        {
            _lobbyViewModel = lobbyViewModel;
            InitData(raceData);
            Log.DLazy(() => "Including LobbyViewModel data in RaceViewModel Initialization.", this);
        }

        public override void InitNetData(IRaceNetData data)
        {
            Log.DLazy(() => "Initializing RaceViewModel net data.", this, _log);
            // no setters to allow a real refresh even for unchanged values
            PlayerStates = new Dictionary<int, RacePlayerState>(data.PlayerStates);
            OnRacePlayerStatesChanged?.Invoke();
            TeamData = new Dictionary<int, RaceTeamData>(data.TeamData);
            OnRaceTeamDataChanged?.Invoke();
            Leaderboard = new List<int>(data.Leaderboard);
            OnRaceLeaderboardChanged?.Invoke();
        }

        public Dictionary<int, RacePlayerState> PlayerStates { get; private set; } = new();
        public void SetPlayerStates(SyncDictionaryOperation op, int key, RacePlayerState value)
        {
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                case SyncDictionaryOperation.Set:
                    PlayerStates[key] = value;
                    break;
                case SyncDictionaryOperation.Remove:
                    PlayerStates.Remove(key);
                    break;
                case SyncDictionaryOperation.Clear:
                    PlayerStates.Clear();
                    break;
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    break;
            }
            if (op != SyncDictionaryOperation.Complete)
            {
                Log.DLazy(() => $"PlayerStates updated: {op} key: {key} value: {value}", this, _log);
                OnRacePlayerStatesChanged?.Invoke();
            }
        }

        public Dictionary<int, RaceTeamData> TeamData { get; private set; } = new();
        public void SetTeamData(SyncDictionaryOperation op, int key, RaceTeamData value)
        {
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                case SyncDictionaryOperation.Set:
                    TeamData[key] = value;
                    break;
                case SyncDictionaryOperation.Remove:
                    TeamData.Remove(key);
                    break;
                case SyncDictionaryOperation.Clear:
                    TeamData.Clear();
                    break;
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    break;
            }
            if (op != SyncDictionaryOperation.Complete)
            {
                Log.DLazy(() => $"TeamData updated: {op} key: {key} value: {value}", this, _log);
                OnRaceTeamDataChanged?.Invoke();
            }
        }

        public List<int> Leaderboard { get; private set; } = new();
        public void SetLeaderboard(SyncListOperation op, int index, int prev, int next)
        {
            switch (op)
            {
                case SyncListOperation.Add:
                    Leaderboard.Add(next);
                    break;
                case SyncListOperation.Insert:
                    if (index >= 0 && index <= Leaderboard.Count)
                        Leaderboard.Insert(index, next);
                    break;
                case SyncListOperation.Set:
                    if (index >= 0 && index < Leaderboard.Count)
                        Leaderboard[index] = next;
                    break;
                case SyncListOperation.RemoveAt:
                    if (index >= 0 && index < Leaderboard.Count)
                        Leaderboard.RemoveAt(index);
                    break;
                case SyncListOperation.Clear:
                    Leaderboard.Clear();
                    break;
                case SyncListOperation.Complete:
                    break;
                default:
                    break;
            }
            Log.DLazy(() => $"Leaderboard updated. First 3 teams: {string.Join(", ", Leaderboard.GetRange(0, Math.Min(3, Leaderboard.Count)))}", this, _log);
            OnRaceLeaderboardChanged?.Invoke();
        }
    }
}

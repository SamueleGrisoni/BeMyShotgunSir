using System;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Common
{
    public enum ReadyFlag
    {
        Manager,
        State,
        NetController
    }

    public interface IReady
    {
        bool IsReady { get; }
        event Action OnReady;
    }

    /// <summary>
    /// Centralized initialization state tracked on Net-related GameObjects.
    /// Other components can set flags and listen for changes instead of using many local booleans.
    /// </summary>
    public class ReadinessState : MonoBehaviour
    {
        public event Action OnReadyFlagChanged;

        private readonly System.Collections.Generic.Dictionary<ReadyFlag, bool> _flags = new System.Collections.Generic.Dictionary<ReadyFlag, bool>();

        private void Awake()
        {
            foreach (ReadyFlag f in Enum.GetValues(typeof(ReadyFlag)))
                _flags[f] = false;
        }

        public bool GetFlag(ReadyFlag flag) => _flags.TryGetValue(flag, out bool v) && v;

        public void SetFlag(ReadyFlag flag, bool value)
        {
            if (GetFlag(flag) == value)
                return;
            _flags[flag] = value;
            OnReadyFlagChanged?.Invoke();
        }

        public bool IsReady(params ReadyFlag[] required)
        {
            foreach (ReadyFlag r in required)
            {
                if (!GetFlag(r))
                    return false;
            }
            return true;
        }

        public bool AllReadyExcept(params ReadyFlag[] excluded)
        {
            foreach (ReadyFlag f in Enum.GetValues(typeof(ReadyFlag)))
            {
                if (Array.Exists(excluded, e => e == f))
                    continue;
                if (!GetFlag(f))
                    return false;
            }
            return true;
        }

        public bool AllReadyExcept(ReadyFlag excluded) => AllReadyExcept(new ReadyFlag[] { excluded });
    }
}

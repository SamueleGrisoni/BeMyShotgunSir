using BeMyShotgunSir.Core;
using UnityEngine;
namespace BeMyShotgunSir.Scripts.Core
{
    public abstract class SOData : ScriptableObject
    {
        public abstract void InitData(SOData data = null);
        public abstract void InitNetData(INetData data);

    }

}

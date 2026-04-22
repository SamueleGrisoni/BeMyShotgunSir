using BeMyShotgunSir.Core;
using UnityEngine;
namespace BeMyShotgunSir.Scripts.Core
{
    public abstract class SOData : ScriptableObject
    {
        public abstract void InitData();
        public abstract void Refresh(IData data);

    }

}

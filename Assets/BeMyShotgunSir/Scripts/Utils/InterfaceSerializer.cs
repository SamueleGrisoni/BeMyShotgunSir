
using System;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Utils
{
    [Serializable]
    public class InterfaceSerializer<O, T> where O : class where T : class
    {
        [SerializeField] private O _interfaceOwner;
        public T Interface => _interfaceOwner as T;
    }
}

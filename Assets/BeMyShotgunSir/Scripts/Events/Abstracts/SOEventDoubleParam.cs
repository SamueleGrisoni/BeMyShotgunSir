
using System;

namespace BeMyShotgunSir.Scripts.Events.Abstracts
{
    public abstract class SOEventDoubleParam<T1, T2> : SOEventBase
    {
        public event Action<IEventSender, T1, T2> OnEventRaised;
        public virtual void RaiseEvent(IEventSender sender, T1 value1, T2 value2) => OnEventRaised?.Invoke(sender, value1, value2);
    }
}

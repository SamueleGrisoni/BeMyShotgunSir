
using System;

namespace BeMyShotgunSir.Scripts.Events.Abstracts
{
    public abstract class SOEventSingleParam<T> : SOEventBase
    {
        public event Action<IEventSender, T> OnEventRaised;
        public virtual void RaiseEvent(IEventSender sender, T value) => OnEventRaised?.Invoke(sender, value);
    }
}

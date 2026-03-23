using System;

namespace BeMyShotgunSir.Scripts.Events.Abstracts
{
    public abstract class SOEventVoid : SOEventBase
    {
        public event Action<IEventSender> OnEventRaised;
        public void RaiseEvent(IEventSender sender) => OnEventRaised?.Invoke(sender);
    }
}

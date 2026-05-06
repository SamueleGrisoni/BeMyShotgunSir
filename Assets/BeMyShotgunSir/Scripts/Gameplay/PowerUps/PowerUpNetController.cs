using System;
using BeMyShotgunSir.Scripts.Core;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    public struct ActivePowerUp
    {
        public PowerUp PowerUp;
        public int TeamId;
        public float RemainingTime;
        public ulong TargetNetId; // 0 if none
    }

    public interface IPowerUpNetController : INetController { }

    public class PowerUpNetController : NetworkBehaviour, IPowerUpNetController
    {
        private bool _log = true;
        public static event Action<IPowerUpNetController> OnPowerUpNetControllerSpawned;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner)
            {
                OnPowerUpNetControllerSpawned?.Invoke(this);
            }
        }

        //NOTE 1. Validate() che controlla lo stato del team in _raceNet; 2. in caso positivo viene chiamato Activate(); 3. OnTick() per il comportamento quando attivo; 4. Deactivate() quando disattivato manualmente; 5. Expire() quando il tempo scade
        //4. e 5. devono aggiornare _raceNet

        private readonly SyncList<ActivePowerUp> _activePowerUps = new SyncList<ActivePowerUp>();



    }
}

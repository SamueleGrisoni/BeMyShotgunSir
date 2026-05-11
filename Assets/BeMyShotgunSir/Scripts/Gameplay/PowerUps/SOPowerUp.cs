using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    public interface IPowerUpStrategy
    {
        bool TryPreliminaryCheck(InfoUsePowerUp info, StrategyContext context);
        PowerUpRuntime OnCreateRuntime(InfoUsePowerUp info, StrategyContext context);
        bool CanActivate(PowerUpRuntime runtime, StrategyContext context);
        void OnActivate(PowerUpRuntime runtime, StrategyContext context);
        void OnChangeTarget(PowerUpRuntime runtime, int instanceId, int targetTeamId, StrategyContext context);
        void OnUse(PowerUpRuntime runtime, StrategyContext context);
        void OnTick(float dt, PowerUpRuntime runtime, StrategyContext context);
        void OnAction(PowerUpRuntime runtime, StrategyContext context, PowerUpAction action);
        void OnExpire(PowerUpRuntime runtime, StrategyContext context);
        void OnDeactivate(PowerUpRuntime runtime, StrategyContext context);
    }

    [CreateAssetMenu(fileName = "SOPowerUps", menuName = "Be My Shotgun, Sir!/PowerUp", order = 0)]
    public abstract class SOPowerUp : ScriptableObject, IPowerUpStrategy
    {
        [field: SerializeField] public PowerUp PowerUpType { get; private set; }
        [field: SerializeField] public PowerUpClass PowerUpClass { get; private set; }
        [field: SerializeField] public bool IsOwnerTargeted { get; private set; }
        [field: SerializeField] public float Duration { get; private set; }

        public virtual bool TryPreliminaryCheck(InfoUsePowerUp info, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
            if (!raceNetStateStore.TryGetTeamInventory(info.OwnerTeamId, out InventoryData inventory))
            {
                Log.WLazy(() => $"Trying to activate power-up for team {info.OwnerTeamId} but no inventory found.", this);
                return false;
            }
            if (inventory.SelectedSlot == PowerUp.None)
            {
                Log.WLazy(() => $"Trying to activate power-up for team {info.OwnerTeamId} but no slot selected.", this);
                return false;
            }
            if (inventory.SelectedSlot != info.PowerUp)
            {
                Log.WLazy(() => $"Trying to activate power-up {info.PowerUp} for team {info.OwnerTeamId} but selected slot contains {inventory.SelectedSlot}.", this);
                return false;
            }
            return true;
        }

        public virtual PowerUpRuntime OnCreateRuntime(InfoUsePowerUp info, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
            var activePowerUpData = new ActivePowerUp(info, powerUpsNetController.GetInstanceId(), this);

            NetworkObject targetNob = null;
            if (IsOwnerTargeted)
                //for owner-targeted power-ups the target nob is the owner's team itself
                raceNetStateStore.TryGetTeamNob(info.OwnerTeamId, out targetNob);
            return new PowerUpRuntime(this, activePowerUpData, targetNob);
        }

        public virtual bool CanActivate(PowerUpRuntime runtime, StrategyContext context) => true;

        public virtual void OnActivate(PowerUpRuntime runtime, StrategyContext context)
        {
            if (runtime.Definition.PowerUpClass == PowerUpClass.TimeBased || runtime.Definition.PowerUpClass == PowerUpClass.OneShot)
                runtime.Definition.OnUse(runtime, context);
        }

        public virtual void OnChangeTarget(PowerUpRuntime runtime, int instanceId, int targetTeamId, StrategyContext context)
        {
            if (context.RaceNetStateStore.TryGetTeamNob(targetTeamId, out NetworkObject targetNob))
            {
                runtime.TargetNob = targetNob;
                runtime.ActivePowerUpData.TargetTeamId = targetTeamId;
                // runtime.Definition.OnUse(runtime, context);
                Log.DLazy(() => $"Changed target of power-up instance {instanceId} to team {targetTeamId}.", this);
            }
        }
        public abstract void OnUse(PowerUpRuntime runtime, StrategyContext context);
        public virtual void OnTick(float dt, PowerUpRuntime runtime, StrategyContext context)
        {
            if (runtime.ActivePowerUpData.PowerUpState == PowerUpState.Ticking)
            {
                runtime.ActivePowerUpData.RemainingDuration -= dt;
                if (runtime.ActivePowerUpData.RemainingDuration <= 0)
                    runtime.Definition.OnExpire(runtime, context);
            }
        }

        public virtual void OnAction(PowerUpRuntime runtime, StrategyContext context, PowerUpAction action) { }

        public virtual void OnDeactivate(PowerUpRuntime runtime, StrategyContext context) { }

        public virtual void OnExpire(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore _, out PowerUpsNetController powerUpsNetController);
            runtime.ActivePowerUpData.PowerUpState = PowerUpState.Expired;
            powerUpsNetController.RemoveActivePowerUp(runtime.ActivePowerUpData.ManagerInstanceId);
        }

        public void UnwrapContext(StrategyContext context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController)
        {
            raceNetStateStore = context.RaceNetStateStore;
            powerUpsNetController = context.PowerUpsNetController;
        }
    }
}

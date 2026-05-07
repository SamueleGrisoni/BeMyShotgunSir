using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    public interface IPowerUpStrategy
    {
        bool TryPreliminaryCheck(InfoUsePowerUp info, StrategyContext context);
        PowerUpRuntime OnCreateRuntime(SOPowerUp definition, InfoUsePowerUp info, StrategyContext context);
        bool CanActivate(PowerUpRuntime runtime, StrategyContext context);
        void OnActivate(PowerUpRuntime runtime, StrategyContext context);
        void OnChangeTarget(PowerUpRuntime runtime, int instanceId, int targetTeamId, StrategyContext context);
        void OnUse(PowerUpRuntime runtime, StrategyContext context);
        void OnTick(float dt, PowerUpRuntime runtime, StrategyContext context);
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
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out IRaceNetStateRead raceNetStateRead, out PowerUpsNetController powerUpsNetController, out Dictionary<int, PowerUpRuntime> activePowerUps);
            if (!raceNetStateRead.TryGetPlayerInventory(info.OwnerTeamId, out InventoryData inventory))
            {
                Log.WLazy(() => $"Trying to activate power-up for team {info.OwnerTeamId} but no inventory found.", this);
                return false;
            }
            if (inventory.SelectedSlot == null)
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

        public virtual PowerUpRuntime OnCreateRuntime(SOPowerUp definition, InfoUsePowerUp info, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out IRaceNetStateRead raceNetStateRead, out PowerUpsNetController powerUpsNetController, out Dictionary<int, PowerUpRuntime> activePowerUps);
            var activePowerUpData = new ActivePowerUp(info, powerUpsNetController.NextPowerUpInstanceId, definition);

            NetworkObject targetNob = null;
            if (definition.IsOwnerTargeted)
                //for owner-targeted power-ups the target nob is the owner's team itself
                raceNetStateStore.TryGetTeamNob(info.OwnerTeamId, out targetNob);
            return new PowerUpRuntime(definition, activePowerUpData, targetNob);
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
            if (runtime.ActivePowerUpData.PowerUpState == PowerUpState.Active)
            {
                runtime.ActivePowerUpData.RemainingDuration -= dt;
                if (runtime.ActivePowerUpData.RemainingDuration <= 0)
                    runtime.Definition.OnExpire(runtime, context);
            }
        }

        public virtual void OnDeactivate(PowerUpRuntime runtime, StrategyContext context) { }

        public virtual void OnExpire(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore _, out IRaceNetStateRead _, out PowerUpsNetController _, out Dictionary<int, PowerUpRuntime> activePowerUps);
            runtime.ActivePowerUpData.PowerUpState = PowerUpState.Expired;
            activePowerUps.Remove(runtime.ActivePowerUpData.ManagerInstanceId);
        }

        public void UnwrapContext(StrategyContext context, out RaceNetStateStore raceNetStateStore, out IRaceNetStateRead raceNetStateRead, out PowerUpsNetController powerUpsNetController, out Dictionary<int, PowerUpRuntime> activePowerUps)
        {
            raceNetStateStore = context.RaceNetStateStore;
            raceNetStateRead = context.RaceNetStateStore;
            powerUpsNetController = context.PowerUpsNetController;
            activePowerUps = context.ActivePowerUps;
        }
    }
}

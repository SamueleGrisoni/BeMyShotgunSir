namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    public enum PowerUp
    {
        None,
        Shield, //Time-based, target self, resist adversary power-ups effects
        Armor, //Time-based, target self, gives an armor that can absorb hits
        Invisibility, //Time-based, target self, makes the team invisible to adversaries (and maybe also to self?)
        // SwapRole, // Action-based, target self, swap the position of the team's members within the hit team
        // ClimateChange, // One-shot, change the climate of the track for the adversary team, making it more difficult to drive
        Spear, // Action-based, throw a spear that will break the hit adversary's shotgun map
        StealPowerUp, // Time-Based, steal a random power-up from the hit team (if any) and give it to the owner team
        RerollPowerUp, // Time-Based, reroll every team's current power-up for a random new one of the same class
        RoadBlock, // One-shot, spawn an obstacle on the track for the adversary team that they will have to avoid (could be a hole, a wall, oil on the ground...)
        // Lasso
    }

    public enum PowerUpClass
    {
        None,
        ActionBased,
        TimeBased,
        OneShot
    }

    public enum PowerUpState
    {
        None,
        Active,
        Ticking,
        Expired
    }

    public enum PowerUpActionType
    {
        None,
        CustomAction1,
        CustomAction2
    }

    public struct PowerUpIdentifier
    {
        public PowerUp PowerUp;
        public int InstanceId;

        public PowerUpIdentifier(PowerUp powerUp, int instanceId)
        {
            PowerUp = powerUp;
            InstanceId = instanceId;
        }

        public override string ToString() => $"PowerUp: {PowerUp}, InstanceId: {InstanceId}";
    }

    public struct PowerUpAction
    {
        public PowerUpActionType ActionType;
        public PowerUpIdentifier Identifier;
        public int OwnerTeamId;
        public int? TargetTeamId;
        public int value1;
        public int value2;

        public PowerUpAction(PowerUpActionType actionType, PowerUpIdentifier identifier, int ownerTeamId, int? targetTeamId = null, int value1 = 0, int value2 = 0)
        {
            ActionType = actionType;
            Identifier = identifier;
            OwnerTeamId = ownerTeamId;
            TargetTeamId = targetTeamId;
            this.value1 = value1;
            this.value2 = value2;
        }

        public override string ToString() => $"PowerUpAction: ActionType={ActionType}, Identifier={Identifier}, OwnerTeamId={OwnerTeamId}, TargetTeamId={TargetTeamId}, value1={value1}, value2={value2}";
    }
}

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    public enum PowerUp
    {
        None,
        Shield,
        Armor,
        Invisibility,
        // SwapRole,
        // ClimateChange,
        Spear,
        StealPowerUp,
        RerollPowerUp,
        RoadBlock,
        // Lasso
    }

    public enum PowerUpClass
    {
        ActionBased,
        TimeBased,
        OneShot
    }

    public enum PowerUpState
    {
        Aiming,
        Fired,
        Active
    }
}

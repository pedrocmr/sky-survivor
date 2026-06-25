using UnityEngine;

namespace NeonSkySurvivor
{
    [CreateAssetMenu(menuName = "Sky Survivor/Enemy Variant", fileName = "EnemyVariantConfig")]
    public sealed class EnemyVariantConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Enemy";

        [Header("Stats")]
        [SerializeField, Min(1)] private int maxHealth = 2;
        [SerializeField, Min(0.1f)] private float baseSpeed = 2.1f;
        [SerializeField, Min(0)] private int scoreReward = 100;
        [SerializeField, Min(0f)] private float energyReward = 8f;
        [SerializeField, Range(0f, 1f)] private float pickupChance = 0.28f;
        [SerializeField, Min(0f)] private float escapedEnergyPenalty = 10f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float predictionTime = 0.3f;
        [SerializeField, Min(0f)] private float steeringStrength = 1.9f;
        [SerializeField, Min(0f)] private float horizontalWave = 0.75f;
        [SerializeField, Min(0f)] private float waveFrequency = 2.2f;

        [Header("Attack")]
        [SerializeField, Min(0.2f)] private float minShotInterval = 1.5f;
        [SerializeField, Min(0.2f)] private float maxShotInterval = 3.2f;
        [SerializeField, Min(0.1f)] private float projectileSpeedMultiplier = 1f;

        public string DisplayName => displayName;
        public int MaxHealth => maxHealth;
        public float BaseSpeed => baseSpeed;
        public int ScoreReward => scoreReward;
        public float EnergyReward => energyReward;
        public float PickupChance => pickupChance;
        public float EscapedEnergyPenalty => escapedEnergyPenalty;
        public float PredictionTime => predictionTime;
        public float SteeringStrength => steeringStrength;
        public float HorizontalWave => horizontalWave;
        public float WaveFrequency => waveFrequency;
        public float MinShotInterval => minShotInterval;
        public float MaxShotInterval => maxShotInterval;
        public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
    }
}

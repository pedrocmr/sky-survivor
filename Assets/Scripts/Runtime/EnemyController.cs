using UnityEngine;

namespace NeonSkySurvivor
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class EnemyController : MonoBehaviour
    {
        [Header("Variant")]
        [SerializeField] private EnemyVariantConfig variantConfig;

        [Header("Stats")]
        [SerializeField, Min(1)] private int maxHealth = 2;
        [SerializeField, Min(0.1f)] private float baseSpeed = 2.1f;
        [SerializeField, Min(0)] private int scoreReward = 100;
        [SerializeField, Min(0f)] private float energyReward = 8f;
        [SerializeField, Range(0f, 1f)] private float pickupChance = 0.28f;
        [SerializeField, Min(0f)] private float escapedEnergyPenalty = 10f;

        [Header("Intelligent Movement")]
        [SerializeField, Min(0f)] private float predictionTime = 0.3f;
        [SerializeField, Min(0f)] private float steeringStrength = 1.9f;
        [SerializeField, Min(0f)] private float horizontalWave = 0.75f;
        [SerializeField, Min(0f)] private float waveFrequency = 2.2f;

        [Header("Attack")]
        [SerializeField] private EnemyProjectile enemyProjectilePrefab;
        [SerializeField, Min(0.2f)] private float minShotInterval = 1.5f;
        [SerializeField, Min(0.2f)] private float maxShotInterval = 3.2f;

        [Header("Rewards")]
        [SerializeField] private EnergyPickup pickupPrefab;
        [SerializeField] private EnemyVisualAnimator visualAnimator;

        private Rigidbody2D body;
        private Transform player;
        private PlayerController playerController;
        private Rigidbody2D playerBody;
        private int currentHealth;
        private float phase;
        private float nextShotTime;
        private bool dying;

        private int MaxHealth => variantConfig != null ? variantConfig.MaxHealth : maxHealth;
        private float BaseSpeed => variantConfig != null ? variantConfig.BaseSpeed : baseSpeed;
        private int ScoreReward => variantConfig != null ? variantConfig.ScoreReward : scoreReward;
        private float EnergyReward => variantConfig != null ? variantConfig.EnergyReward : energyReward;
        private float PickupChance => variantConfig != null ? variantConfig.PickupChance : pickupChance;
        private float EscapedEnergyPenalty => variantConfig != null ? variantConfig.EscapedEnergyPenalty : escapedEnergyPenalty;
        private float PredictionTime => variantConfig != null ? variantConfig.PredictionTime : predictionTime;
        private float SteeringStrength => variantConfig != null ? variantConfig.SteeringStrength : steeringStrength;
        private float HorizontalWave => variantConfig != null ? variantConfig.HorizontalWave : horizontalWave;
        private float WaveFrequency => variantConfig != null ? variantConfig.WaveFrequency : waveFrequency;
        private float MinShotInterval => variantConfig != null ? variantConfig.MinShotInterval : minShotInterval;
        private float MaxShotInterval => variantConfig != null ? variantConfig.MaxShotInterval : maxShotInterval;
        private float ProjectileSpeedMultiplier => variantConfig != null ? variantConfig.ProjectileSpeedMultiplier : 1f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            currentHealth = MaxHealth;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Start()
        {
            FindPlayer();
            ScheduleNextShot();
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || dying)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            if (player == null)
            {
                FindPlayer();
            }

            float difficulty = GameManager.Instance.DifficultyMultiplier;
            Vector2 desired = Vector2.down;

            if (player != null)
            {
                Vector2 predictedVelocity = playerBody != null ? playerBody.linearVelocity : Vector2.zero;
                Vector2 predictedPosition = (Vector2)player.position + predictedVelocity * PredictionTime;
                Vector2 toPlayer = (predictedPosition - body.position).normalized;
                desired = Vector2.Lerp(Vector2.down, toPlayer, 0.45f).normalized;
            }

            float wave = Mathf.Sin(Time.time * WaveFrequency + phase) * HorizontalWave;
            desired.x += wave;
            Vector2 targetVelocity = desired.normalized * BaseSpeed * Mathf.Lerp(1f, difficulty, 0.55f);
            body.linearVelocity = Vector2.Lerp(body.linearVelocity, targetVelocity, SteeringStrength * Time.fixedDeltaTime);
            visualAnimator.SetVelocity(body.linearVelocity);

            if (Time.time >= nextShotTime && player != null)
            {
                ShootAtPlayer(difficulty);
                ScheduleNextShot();
            }

            if (transform.position.y < -6.3f)
            {
                GameManager.Instance.RegisterEnemyEscaped(EscapedEnergyPenalty);
                Destroy(gameObject);
            }
        }

        public void TakeDamage(int amount, bool fromPulse)
        {
            if (dying || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            currentHealth -= Mathf.Max(1, amount);
            visualAnimator.PlayHit();

            if (currentHealth <= 0)
            {
                Die(fromPulse);
            }
        }

        private void Die(bool fromPulse)
        {
            dying = true;
            body.linearVelocity = Vector2.zero;
            GameManager.Instance.RegisterEnemyDefeated(fromPulse ? ScoreReward / 2 : ScoreReward, EnergyReward);

            if (!fromPulse && pickupPrefab != null && Random.value <= PickupChance)
            {
                Instantiate(pickupPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }

        private void ShootAtPlayer(float difficulty)
        {
            if (enemyProjectilePrefab == null || player == null)
            {
                return;
            }

            Vector2 direction = ((Vector2)player.position - body.position).normalized;
            EnemyProjectile projectile = Instantiate(enemyProjectilePrefab, transform.position, Quaternion.identity);
            projectile.Initialize(direction, ProjectileSpeedMultiplier * Mathf.Lerp(1f, difficulty, 0.4f));
            visualAnimator.PlayAttack();
        }

        private void ScheduleNextShot()
        {
            float difficulty = GameManager.Instance != null ? GameManager.Instance.DifficultyMultiplier : 1f;
            float interval = Random.Range(MinShotInterval, MaxShotInterval) / Mathf.Clamp(difficulty, 1f, 2.5f);
            nextShotTime = Time.time + interval;
        }

        private void FindPlayer()
        {
            playerController = FindObjectOfType<PlayerController>();
            player = playerController != null ? playerController.transform : null;
            playerBody = playerController != null ? playerController.GetComponent<Rigidbody2D>() : null;
        }
    }
}

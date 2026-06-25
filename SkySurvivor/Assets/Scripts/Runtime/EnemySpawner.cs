using UnityEngine;

namespace NeonSkySurvivor
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [System.Serializable]
        private sealed class EnemySpawnEntry
        {
            [SerializeField] private EnemyController prefab;
            [SerializeField, Min(0f)] private float unlockAfterSeconds;
            [SerializeField, Min(0f)] private float baseWeight = 1f;
            [SerializeField, Min(0f)] private float extraWeightAtFullRamp;

            public EnemyController Prefab => prefab;
            public float UnlockAfterSeconds => unlockAfterSeconds;

            public float GetWeight(float rampProgress)
            {
                return Mathf.Max(0f, baseWeight + extraWeightAtFullRamp * rampProgress);
            }
        }

        [SerializeField] private EnemyController enemyPrefab;
        [SerializeField] private EnemySpawnEntry[] enemyVariants;
        [SerializeField, Min(0.1f)] private float initialInterval = 1.9f;
        [SerializeField, Min(0.1f)] private float minimumInterval = 0.75f;
        [SerializeField, Min(0.1f)] private float difficultyRampSeconds = 120f;
        [SerializeField, Min(1)] private int maximumEnemiesAlive = 16;
        [SerializeField] private float spawnY = 6.2f;
        [SerializeField] private float horizontalMargin = 0.6f;

        private Camera mainCamera;
        private float nextSpawnTime;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (Time.time < nextSpawnTime)
            {
                return;
            }

            int alive = FindObjectsOfType<EnemyController>().Length;
            if (alive < maximumEnemiesAlive)
            {
                SpawnEnemy();
            }

            ScheduleNextSpawn();
        }

        public void ResetSpawner()
        {
            nextSpawnTime = Time.time + 0.8f;
        }

        private void SpawnEnemy()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            float left = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, 0f)).x + horizontalMargin;
            float right = mainCamera.ViewportToWorldPoint(new Vector3(1f, 0f, 0f)).x - horizontalMargin;
            Vector3 position = new Vector3(Random.Range(left, right), spawnY, 0f);

            EnemyController prefab = SelectEnemyPrefab();
            if (prefab != null)
            {
                Instantiate(prefab, position, Quaternion.identity);
            }
        }

        private void ScheduleNextSpawn()
        {
            float progress = Mathf.Clamp01(GameManager.Instance.ElapsedTime / difficultyRampSeconds);
            float interval = Mathf.Lerp(initialInterval, minimumInterval, progress);
            interval /= Mathf.Clamp(GameManager.Instance.DifficultyMultiplier * 0.6f, 1f, 1.45f);
            nextSpawnTime = Time.time + interval * Random.Range(0.8f, 1.2f);
        }

        private EnemyController SelectEnemyPrefab()
        {
            if (enemyVariants == null || enemyVariants.Length == 0 || GameManager.Instance == null)
            {
                return enemyPrefab;
            }

            float elapsed = GameManager.Instance.ElapsedTime;
            float rampProgress = Mathf.Clamp01(elapsed / difficultyRampSeconds);
            float totalWeight = 0f;

            for (int i = 0; i < enemyVariants.Length; i++)
            {
                EnemySpawnEntry entry = enemyVariants[i];
                if (entry != null && entry.Prefab != null && elapsed >= entry.UnlockAfterSeconds)
                {
                    totalWeight += entry.GetWeight(rampProgress);
                }
            }

            if (totalWeight <= 0f)
            {
                return enemyPrefab;
            }

            float roll = Random.value * totalWeight;
            for (int i = 0; i < enemyVariants.Length; i++)
            {
                EnemySpawnEntry entry = enemyVariants[i];
                if (entry == null || entry.Prefab == null || elapsed < entry.UnlockAfterSeconds)
                {
                    continue;
                }

                roll -= entry.GetWeight(rampProgress);
                if (roll <= 0f)
                {
                    return entry.Prefab;
                }
            }

            return enemyPrefab;
        }
    }
}

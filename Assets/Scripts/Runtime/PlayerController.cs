using System.Collections;
using UnityEngine;

namespace NeonSkySurvivor
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float moveSpeed = 6f;
        [SerializeField, Min(0.1f)] private float dashSpeed = 15f;
        [SerializeField, Min(0.05f)] private float dashDuration = 0.22f;
        [SerializeField, Min(0f)] private float dashEnergyCost = 25f;

        [Header("Combat")]
        [SerializeField] private PlayerProjectile projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField, Min(0.02f)] private float fireInterval = 0.18f;
        [SerializeField, Min(0f)] private float pulseEnergyCost = 60f;
        [SerializeField, Min(0.5f)] private float pulseRadius = 4f;
        [SerializeField, Min(0.05f)] private float pulseCooldown = 0.45f;

        [Header("Health")]
        [SerializeField, Min(1)] private int maxLives = 3;
        [SerializeField, Min(0.1f)] private float damageInvulnerabilitySeconds = 1.05f;

        [Header("Feedback")]
        [SerializeField] private PlayerVisualAnimator visualAnimator;
        [SerializeField] private CameraEffects cameraEffects;

        private Rigidbody2D body;
        private Camera mainCamera;
        private Vector2 moveInput;
        private float nextFireTime;
        private float nextPulseTime;
        private bool dashing;
        private bool invulnerable;
        private bool dead;
        private int currentLives;

        public bool IsInvulnerable => invulnerable;
        public int CurrentLives => currentLives;
        public int MaxLives => maxLives;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            mainCamera = Camera.main;
            if (cameraEffects == null && mainCamera != null)
            {
                cameraEffects = mainCamera.GetComponent<CameraEffects>();
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || dead)
            {
                moveInput = Vector2.zero;
                return;
            }

            ReadMovement();

            if ((Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0)) && Time.time >= nextFireTime)
            {
                Fire();
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                TryDash();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                TryPulse();
            }
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || dead || dashing)
            {
                return;
            }

            body.linearVelocity = moveInput * moveSpeed;
            ClampToCamera();
        }

        public void ResetPlayer()
        {
            StopAllCoroutines();
            dead = false;
            dashing = false;
            invulnerable = false;
            currentLives = maxLives;
            nextFireTime = 0f;
            nextPulseTime = 0f;
            transform.position = new Vector3(0f, -3.5f, 0f);
            body.linearVelocity = Vector2.zero;
            gameObject.SetActive(true);
            visualAnimator.SetDead(false);
        }

        public void Hit()
        {
            if (dead || invulnerable || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            currentLives = Mathf.Max(0, currentLives - 1);
            if (currentLives <= 0)
            {
                StartCoroutine(DieRoutine());
                return;
            }

            StartCoroutine(DamageRoutine());
        }

        private void ReadMovement()
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;

            moveInput = new Vector2(horizontal, vertical).normalized;
            visualAnimator.SetMovement(moveInput);
        }

        private void Fire()
        {
            nextFireTime = Time.time + fireInterval;
            PlayerProjectile projectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
            projectile.Initialize(Vector2.up);
            visualAnimator.PlayShootKick();
            if (cameraEffects != null) cameraEffects.Shake(0.035f, 0.07f);
        }

        private void TryDash()
        {
            if (dashing || moveInput.sqrMagnitude < 0.01f)
            {
                return;
            }

            if (GameManager.Instance.TrySpendEnergy(dashEnergyCost))
            {
                StartCoroutine(DashRoutine(moveInput));
            }
        }

        private IEnumerator DashRoutine(Vector2 direction)
        {
            dashing = true;
            invulnerable = true;
            visualAnimator.SetDashing(true);

            float timer = 0f;
            while (timer < dashDuration)
            {
                timer += Time.deltaTime;
                body.linearVelocity = direction * dashSpeed;
                ClampToCamera();
                yield return new WaitForFixedUpdate();
            }

            body.linearVelocity = Vector2.zero;
            visualAnimator.SetDashing(false);
            invulnerable = false;
            dashing = false;
        }

        private void TryPulse()
        {
            if (Time.time < nextPulseTime)
            {
                return;
            }

            nextPulseTime = Time.time + pulseCooldown;

            if (!GameManager.Instance.TrySpendEnergy(pulseEnergyCost))
            {
                visualAnimator.PlayDeniedFeedback();
                return;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pulseRadius);
            foreach (Collider2D hit in hits)
            {
                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(999, true);
                    continue;
                }

                EnemyProjectile enemyProjectile = hit.GetComponent<EnemyProjectile>();
                if (enemyProjectile != null)
                {
                    Destroy(enemyProjectile.gameObject);
                }
            }

            visualAnimator.PlayPulse(pulseRadius);
            if (cameraEffects != null)
            {
                cameraEffects.PulseZoom();
                cameraEffects.Shake(0.18f, 0.3f);
            }
        }

        private IEnumerator DieRoutine()
        {
            dead = true;
            invulnerable = true;
            body.linearVelocity = Vector2.zero;
            visualAnimator.SetDead(true);
            if (cameraEffects != null) cameraEffects.Shake(0.28f, 0.45f);
            yield return new WaitForSeconds(0.45f);
            GameManager.Instance.GameOver();
        }

        private IEnumerator DamageRoutine()
        {
            invulnerable = true;
            body.linearVelocity = Vector2.zero;
            visualAnimator.PlayDamageFeedback(damageInvulnerabilitySeconds);
            if (cameraEffects != null) cameraEffects.Shake(0.18f, 0.24f);
            yield return new WaitForSeconds(damageInvulnerabilitySeconds);
            invulnerable = false;
        }

        private void ClampToCamera()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            Vector3 min = mainCamera.ViewportToWorldPoint(new Vector3(0.05f, 0.06f, 0f));
            Vector3 max = mainCamera.ViewportToWorldPoint(new Vector3(0.95f, 0.92f, 0f));
            Vector2 position = body.position;
            position.x = Mathf.Clamp(position.x, min.x, max.x);
            position.y = Mathf.Clamp(position.y, min.y, max.y);
            body.position = position;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<EnemyController>() != null)
            {
                Hit();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, pulseRadius);
        }
    }
}

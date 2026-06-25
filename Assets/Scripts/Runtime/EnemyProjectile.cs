using UnityEngine;

namespace NeonSkySurvivor
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 5f;
        [SerializeField, Min(0.1f)] private float lifetime = 6f;

        private Rigidbody2D body;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            Destroy(gameObject, lifetime);
        }

        public void Initialize(Vector2 direction, float speedMultiplier)
        {
            body.linearVelocity = direction.normalized * speed * Mathf.Max(0.1f, speedMultiplier);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                return;
            }

            if (!player.IsInvulnerable)
            {
                player.Hit();
            }

            Destroy(gameObject);
        }
    }
}

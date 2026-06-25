using UnityEngine;

namespace NeonSkySurvivor
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PlayerProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 12f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.1f)] private float lifetime = 3f;

        private Rigidbody2D body;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            Destroy(gameObject, lifetime);
        }

        public void Initialize(Vector2 direction)
        {
            body.velocity = direction.normalized * speed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy == null)
            {
                return;
            }

            enemy.TakeDamage(damage, false);
            Destroy(gameObject);
        }
    }
}

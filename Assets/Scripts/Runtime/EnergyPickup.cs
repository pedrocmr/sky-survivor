using UnityEngine;

namespace NeonSkySurvivor
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class EnergyPickup : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float fallSpeed = 1.8f;
        [SerializeField, Min(0f)] private float energyReward = 22f;
        [SerializeField, Min(0)] private int scoreReward = 25;
        [SerializeField, Min(0.1f)] private float lifetime = 8f;

        private Rigidbody2D body;
        private float baseScale;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            baseScale = transform.localScale.x;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * 7f) * 0.13f;
            transform.localScale = Vector3.one * baseScale * pulse;
            transform.Rotate(0f, 0f, 85f * Time.deltaTime);
        }

        private void FixedUpdate()
        {
            body.linearVelocity = Vector2.down * fallSpeed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            GameManager.Instance.RegisterPickup(scoreReward, energyReward);
            Destroy(gameObject);
        }
    }
}

using System.Collections;
using UnityEngine;

namespace NeonSkySurvivor
{
    public sealed class EnemyVisualAnimator : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hitColor = new Color(1f, 1f, 0.3f, 1f);
        [SerializeField] private Color attackColor = new Color(1f, 0.25f, 0.7f, 1f);

        private Vector2 velocity;
        private Vector3 initialScale;
        private float randomOffset;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            initialScale = visualRoot.localScale;
            randomOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * 5.5f + randomOffset) * 0.09f;
            visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, initialScale * pulse, 8f * Time.deltaTime);

            float tilt = -velocity.x * 7f + Mathf.Sin(Time.time * 3f + randomOffset) * 5f;
            visualRoot.localRotation = Quaternion.Lerp(
                visualRoot.localRotation,
                Quaternion.Euler(0f, 0f, tilt),
                7f * Time.deltaTime);
        }

        public void SetVelocity(Vector2 value)
        {
            velocity = value;
        }

        public void PlayHit()
        {
            StopCoroutine(nameof(FlashRoutine));
            StartCoroutine(FlashRoutine(hitColor, 0.09f));
        }

        public void PlayAttack()
        {
            StopCoroutine(nameof(FlashRoutine));
            StartCoroutine(FlashRoutine(attackColor, 0.12f));
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            if (spriteRenderer == null)
            {
                yield break;
            }

            spriteRenderer.color = color;
            yield return new WaitForSeconds(duration);
            spriteRenderer.color = normalColor;
        }
    }
}

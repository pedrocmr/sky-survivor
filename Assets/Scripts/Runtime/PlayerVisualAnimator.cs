using System.Collections;
using UnityEngine;

namespace NeonSkySurvivor
{
    public sealed class PlayerVisualAnimator : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer shipRenderer;
        [SerializeField] private SpriteRenderer pulseRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color dashColor = new Color(0.35f, 1f, 1f, 1f);
        [SerializeField] private Color deniedColor = new Color(1f, 0.25f, 0.35f, 1f);
        [SerializeField] private Color damageColor = new Color(1f, 0.18f, 0.28f, 1f);

        private Vector2 movement;
        private Vector3 initialLocalPosition;
        private Vector3 initialScale;
        private float shootKick;
        private bool dashing;
        private bool dead;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            initialLocalPosition = visualRoot.localPosition;
            initialScale = visualRoot.localScale;

            if (pulseRenderer != null)
            {
                pulseRenderer.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (dead)
            {
                visualRoot.Rotate(0f, 0f, 430f * Time.unscaledDeltaTime);
                visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, Vector3.zero, 6f * Time.unscaledDeltaTime);
                return;
            }

            float bob = Mathf.Sin(Time.time * 4.2f) * 0.055f;
            visualRoot.localPosition = initialLocalPosition + Vector3.up * bob - Vector3.up * shootKick;

            float targetAngle = -movement.x * 18f;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
            visualRoot.localRotation = Quaternion.Lerp(visualRoot.localRotation, targetRotation, 10f * Time.deltaTime);

            Vector3 movementScale = initialScale + new Vector3(0.04f * Mathf.Abs(movement.x), 0.07f * Mathf.Abs(movement.y), 0f);
            if (dashing)
            {
                movementScale += new Vector3(-0.08f, 0.28f, 0f);
            }

            visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, movementScale, 12f * Time.deltaTime);
            shootKick = Mathf.MoveTowards(shootKick, 0f, Time.deltaTime * 2.8f);
        }

        public void SetMovement(Vector2 input)
        {
            movement = input;
        }

        public void SetDashing(bool value)
        {
            dashing = value;
            if (shipRenderer != null)
            {
                shipRenderer.color = value ? dashColor : normalColor;
            }
        }

        public void SetDead(bool value)
        {
            dead = value;
            if (!value)
            {
                visualRoot.localPosition = initialLocalPosition;
                visualRoot.localScale = initialScale;
                visualRoot.localRotation = Quaternion.identity;
                if (shipRenderer != null) shipRenderer.color = normalColor;
            }
        }

        public void PlayShootKick()
        {
            shootKick = 0.12f;
        }

        public void PlayDeniedFeedback()
        {
            StopCoroutine(nameof(DeniedRoutine));
            StartCoroutine(DeniedRoutine());
        }

        public void PlayDamageFeedback(float duration)
        {
            StopCoroutine(nameof(DamageRoutine));
            StartCoroutine(DamageRoutine(duration));
        }

        public void PlayPulse(float worldRadius)
        {
            if (pulseRenderer == null)
            {
                return;
            }

            StopCoroutine(nameof(PulseRoutine));
            StartCoroutine(PulseRoutine(worldRadius));
        }

        private IEnumerator DeniedRoutine()
        {
            if (shipRenderer == null)
            {
                yield break;
            }

            shipRenderer.color = deniedColor;
            yield return new WaitForSeconds(0.13f);
            shipRenderer.color = dashing ? dashColor : normalColor;
        }

        private IEnumerator DamageRoutine(float duration)
        {
            if (shipRenderer == null)
            {
                yield break;
            }

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                bool flash = Mathf.FloorToInt(timer * 12f) % 2 == 0;
                shipRenderer.color = flash ? damageColor : normalColor;
                yield return null;
            }

            shipRenderer.color = dashing ? dashColor : normalColor;
        }

        private IEnumerator PulseRoutine(float worldRadius)
        {
            pulseRenderer.gameObject.SetActive(true);
            pulseRenderer.color = new Color(0.2f, 1f, 1f, 0.85f);
            pulseRenderer.transform.localScale = Vector3.one * 0.2f;

            float timer = 0f;
            const float duration = 0.35f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / duration);
                float spriteDiameter = pulseRenderer.sprite != null ? pulseRenderer.sprite.bounds.size.x : 1f;
                float targetScale = (worldRadius * 2f) / Mathf.Max(0.01f, spriteDiameter);
                pulseRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, targetScale, progress);
                Color color = pulseRenderer.color;
                color.a = 1f - progress;
                pulseRenderer.color = color;
                yield return null;
            }

            pulseRenderer.gameObject.SetActive(false);
        }
    }
}

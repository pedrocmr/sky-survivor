using System.Collections;
using UnityEngine;

namespace NeonSkySurvivor
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraEffects : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float pulseZoomAmount = 0.35f;
        [SerializeField, Min(0.05f)] private float pulseZoomDuration = 0.28f;

        private Camera controlledCamera;
        private Vector3 baseLocalPosition;
        private float baseOrthographicSize;
        private Coroutine shakeRoutine;
        private Coroutine zoomRoutine;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            baseLocalPosition = transform.localPosition;
            baseOrthographicSize = controlledCamera.orthographicSize;
        }

        public void Shake(float magnitude, float duration)
        {
            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
            }

            shakeRoutine = StartCoroutine(ShakeRoutine(Mathf.Max(0f, magnitude), Mathf.Max(0f, duration)));
        }

        public void PulseZoom()
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
            }

            zoomRoutine = StartCoroutine(PulseZoomRoutine());
        }

        private IEnumerator ShakeRoutine(float magnitude, float duration)
        {
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float strength = 1f - Mathf.Clamp01(timer / duration);
                Vector2 offset = Random.insideUnitCircle * magnitude * strength;
                transform.localPosition = baseLocalPosition + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }

            transform.localPosition = baseLocalPosition;
            shakeRoutine = null;
        }

        private IEnumerator PulseZoomRoutine()
        {
            float timer = 0f;
            while (timer < pulseZoomDuration)
            {
                timer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(timer / pulseZoomDuration);
                float wave = Mathf.Sin(progress * Mathf.PI);
                controlledCamera.orthographicSize = baseOrthographicSize - pulseZoomAmount * wave;
                yield return null;
            }

            controlledCamera.orthographicSize = baseOrthographicSize;
            zoomRoutine = null;
        }
    }
}

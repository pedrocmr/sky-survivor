using System.Collections.Generic;
using UnityEngine;

namespace NeonSkySurvivor
{
    public sealed class Starfield : MonoBehaviour
    {
        [SerializeField] private Sprite starSprite;
        [SerializeField, Range(10, 150)] private int starCount = 70;
        [SerializeField] private Vector2 sizeRange = new Vector2(0.025f, 0.09f);
        [SerializeField] private Vector2 speedRange = new Vector2(0.35f, 1.35f);
        [SerializeField] private float minX = -5.2f;
        [SerializeField] private float maxX = 5.2f;
        [SerializeField] private float minY = -6.2f;
        [SerializeField] private float maxY = 6.2f;

        private readonly List<Star> stars = new List<Star>();

        private sealed class Star
        {
            public Transform Transform;
            public float Speed;
        }

        private void Start()
        {
            for (int i = 0; i < starCount; i++)
            {
                GameObject starObject = new GameObject($"Star_{i:00}");
                starObject.transform.SetParent(transform);
                starObject.transform.position = RandomPosition();
                float scale = Random.Range(sizeRange.x, sizeRange.y);
                starObject.transform.localScale = Vector3.one * scale;

                SpriteRenderer renderer = starObject.AddComponent<SpriteRenderer>();
                renderer.sprite = starSprite;
                renderer.sortingOrder = -20;
                float brightness = Random.Range(0.35f, 0.9f);
                renderer.color = new Color(brightness, brightness, 1f, brightness);

                stars.Add(new Star
                {
                    Transform = starObject.transform,
                    Speed = Random.Range(speedRange.x, speedRange.y)
                });
            }
        }

        private void Update()
        {
            float difficulty = GameManager.Instance != null ? GameManager.Instance.DifficultyMultiplier : 1f;
            float speedBoost = Mathf.Lerp(1f, difficulty, 0.22f);

            foreach (Star star in stars)
            {
                star.Transform.position += Vector3.down * star.Speed * speedBoost * Time.deltaTime;
                if (star.Transform.position.y < minY)
                {
                    star.Transform.position = new Vector3(Random.Range(minX, maxX), maxY, 0f);
                }
            }
        }

        private Vector3 RandomPosition()
        {
            return new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0f);
        }
    }
}

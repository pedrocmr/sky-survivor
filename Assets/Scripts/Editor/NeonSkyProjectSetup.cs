#if UNITY_EDITOR
using System;
using System.IO;
using NeonSkySurvivor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace NeonSkySurvivorEditor
{
    public static class NeonSkyProjectSetup
    {
        private const string GeneratedArtFolder = "Assets/Art/Generated";
        private const string VariantFolder = "Assets/EnemyVariants";
        private const string PrefabFolder = "Assets/Prefabs";
        private const string ScenePath = "Assets/Scenes/NeonSkySurvivor.unity";

        private static Font uiFont;

        private sealed class EnemyVariantDefinition
        {
            public string Name;
            public int MaxHealth;
            public float BaseSpeed;
            public int ScoreReward;
            public float EnergyReward;
            public float PickupChance;
            public float EscapedEnergyPenalty;
            public float PredictionTime;
            public float SteeringStrength;
            public float HorizontalWave;
            public float WaveFrequency;
            public float MinShotInterval;
            public float MaxShotInterval;
            public float ProjectileSpeedMultiplier;
            public float VisualScale;
            public float ColliderRadius;
            public Color NormalColor;
            public Color HitColor;
            public Color AttackColor;
            public float UnlockAfterSeconds;
            public float BaseWeight;
            public float ExtraWeightAtFullRamp;
        }

        [MenuItem("Tools/Sky Survivor/Build Sample Scene")]
        public static void BuildSampleScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolders();
            CreateGeneratedSprites();
            AssetDatabase.Refresh();

            Sprite playerSprite = LoadSprite("PlayerShip");
            Sprite enemySprite = LoadSprite("EnemyShip");
            Sprite playerProjectileSprite = LoadSprite("PlayerProjectile");
            Sprite enemyProjectileSprite = LoadSprite("EnemyProjectile");
            Sprite pickupSprite = LoadSprite("EnergyPickup");
            Sprite ringSprite = LoadSprite("PulseRing");
            Sprite starSprite = LoadSprite("Star");
            Sprite tileSprite = LoadSprite("BackgroundTile");

            PlayerProjectile playerProjectilePrefab = CreatePlayerProjectilePrefab(playerProjectileSprite);
            EnemyProjectile enemyProjectilePrefab = CreateEnemyProjectilePrefab(enemyProjectileSprite);
            EnergyPickup pickupPrefab = CreatePickupPrefab(pickupSprite);
            EnemyVariantDefinition[] enemyDefinitions = CreateEnemyVariantDefinitions();
            EnemyController[] enemyPrefabs = CreateEnemyPrefabs(enemySprite, enemyProjectilePrefab, pickupPrefab, enemyDefinitions);
            PlayerController playerPrefab = CreatePlayerPrefab(playerSprite, ringSprite, playerProjectilePrefab);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            playerPrefab = LoadPrefabComponent<PlayerController>($"{PrefabFolder}/Player.prefab");
            for (int i = 0; i < enemyDefinitions.Length; i++)
            {
                enemyPrefabs[i] = LoadPrefabComponent<EnemyController>($"{PrefabFolder}/Enemy{enemyDefinitions[i].Name}.prefab");
            }

            Camera mainCamera = CreateCamera();
            CreateTilemapBackground(tileSprite);
            CreateStarfield(starSprite);

            GameObject playerObject = PrefabUtility.InstantiatePrefab(playerPrefab.gameObject) as GameObject;
            PlayerController player = playerObject.GetComponent<PlayerController>();
            player.transform.position = new Vector3(0f, -3.5f, 0f);

            EnemySpawner spawner = CreateSpawner(enemyPrefabs[0], enemyPrefabs, enemyDefinitions);
            GameUI ui = CreateUI();
            CreateEventSystem();
            CreateGameManager(player, spawner, ui);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorUtility.DisplayDialog(
                "Sky Survivor",
                "Projeto configurado. Abra a cena Assets/Scenes/NeonSkySurvivor.unity e pressione Play.",
                "OK");
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets", "Art");
            CreateFolder("Assets/Art", "Generated");
            CreateFolder("Assets", "EnemyVariants");
            CreateFolder("Assets", "Prefabs");
            CreateFolder("Assets", "Scenes");
        }

        private static void CreateFolder(string parent, string name)
        {
            string fullPath = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void CreateGeneratedSprites()
        {
            WriteSprite("PlayerShip", 64, 64, (x, y, w, h) =>
            {
                float nx = (x + 0.5f) / w * 2f - 1f;
                float ny = (y + 0.5f) / h * 2f - 1f;
                bool nose = ny > 0.02f && ny < 0.94f && Mathf.Abs(nx) < Mathf.Lerp(0.34f, 0.03f, (ny - 0.02f) / 0.92f);
                bool core = ny > -0.66f && ny < 0.35f && Mathf.Abs(nx) < 0.28f;
                bool wings = ny > -0.5f && ny < 0.22f && Mathf.Abs(nx) < Mathf.Lerp(0.88f, 0.22f, (ny + 0.5f) / 0.72f);
                bool engines = ny > -0.88f && ny < -0.42f && (Mathf.Abs(nx - 0.26f) < 0.12f || Mathf.Abs(nx + 0.26f) < 0.12f);
                if (!nose && !core && !wings && !engines) return Clear;
                if (Mathf.Abs(nx) < 0.09f && ny > -0.58f) return new Color32(245, 255, 255, 255);
                if (engines) return new Color32(255, 225, 70, 255);
                return wings && Mathf.Abs(nx) > 0.34f ? new Color32(42, 155, 255, 255) : new Color32(40, 235, 255, 255);
            });

            WriteSprite("EnemyShip", 64, 64, (x, y, w, h) =>
            {
                float nx = (x + 0.5f) / w * 2f - 1f;
                float ny = (y + 0.5f) / h * 2f - 1f;
                bool shell = Mathf.Abs(nx) < 0.72f - Mathf.Abs(ny) * 0.25f && Mathf.Abs(ny) < 0.78f;
                bool claws = ny > -0.08f && ny < 0.42f && Mathf.Abs(nx) > 0.38f && Mathf.Abs(nx) < 0.9f - ny * 0.34f;
                bool tail = ny < -0.42f && Mathf.Abs(nx) < 0.24f + (ny + 0.78f) * 0.28f;
                if (!shell && !claws && !tail) return Clear;
                if (Mathf.Abs(nx) < 0.18f && Mathf.Abs(ny) < 0.26f) return new Color32(255, 235, 255, 255);
                if (claws) return new Color32(255, 55, 95, 255);
                return new Color32(180, 65, 255, 255);
            });

            WriteSprite("PlayerProjectile", 20, 48, (x, y, w, h) =>
            {
                float nx = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                float ny = (y + 0.5f) / h;
                if (nx > 0.34f) return Clear;
                return ny > 0.75f ? new Color32(255, 255, 255, 255) : new Color32(255, 225, 60, 255);
            });

            WriteSprite("EnemyProjectile", 32, 32, (x, y, w, h) =>
            {
                float nx = (x + 0.5f) / w * 2f - 1f;
                float ny = (y + 0.5f) / h * 2f - 1f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                if (distance > 0.86f) return Clear;
                return distance < 0.42f ? new Color32(255, 235, 255, 255) : new Color32(255, 55, 210, 255);
            });

            WriteSprite("EnergyPickup", 48, 48, (x, y, w, h) =>
            {
                float nx = (x + 0.5f) / w * 2f - 1f;
                float ny = (y + 0.5f) / h * 2f - 1f;
                if (Mathf.Abs(nx) + Mathf.Abs(ny) > 0.9f) return Clear;
                bool core = Mathf.Abs(nx) < 0.18f || Mathf.Abs(ny) < 0.18f;
                return core ? new Color32(235, 255, 235, 255) : new Color32(55, 255, 145, 255);
            });

            WriteSprite("PulseRing", 96, 96, (x, y, w, h) =>
            {
                float nx = (x + 0.5f) / w * 2f - 1f;
                float ny = (y + 0.5f) / h * 2f - 1f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                return distance > 0.77f && distance < 0.94f ? new Color32(75, 245, 255, 220) : Clear;
            });

            WriteSprite("Star", 16, 16, (x, y, w, h) =>
            {
                float nx = (x + 0.5f) / w * 2f - 1f;
                float ny = (y + 0.5f) / h * 2f - 1f;
                return nx * nx + ny * ny < 0.75f ? new Color32(255, 255, 255, 255) : Clear;
            });

            WriteSprite("BackgroundTile", 64, 64, (x, y, w, h) =>
            {
                bool border = x <= 1 || y <= 1 || x >= w - 2 || y >= h - 2;
                bool innerLine = x == w / 2 || y == h / 2;
                if (border) return new Color32(13, 42, 72, 255);
                if (innerLine) return new Color32(8, 27, 48, 255);
                return new Color32(4, 14, 29, 255);
            });
        }

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        private static void WriteSprite(string name, int width, int height, Func<int, int, int, int, Color32> pixelFactory)
        {
            string assetPath = $"{GeneratedArtFolder}/{name}.png";
            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = pixelFactory(x, y, width, height);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedArtFolder}/{name}.png");
        }

        private static PlayerProjectile CreatePlayerProjectilePrefab(Sprite sprite)
        {
            GameObject root = new GameObject("PlayerProjectile");
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 15;

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.18f, 0.55f);

            PlayerProjectile component = root.AddComponent<PlayerProjectile>();
            return SavePrefab(root, $"{PrefabFolder}/PlayerProjectile.prefab").GetComponent<PlayerProjectile>();
        }

        private static EnemyProjectile CreateEnemyProjectilePrefab(Sprite sprite)
        {
            GameObject root = new GameObject("EnemyProjectile");
            root.transform.localScale = Vector3.one * 0.7f;

            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 14;

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.32f;

            root.AddComponent<EnemyProjectile>();
            return SavePrefab(root, $"{PrefabFolder}/EnemyProjectile.prefab").GetComponent<EnemyProjectile>();
        }

        private static EnergyPickup CreatePickupPrefab(Sprite sprite)
        {
            GameObject root = new GameObject("EnergyPickup");
            root.transform.localScale = Vector3.one * 0.65f;

            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 12;

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.44f;

            root.AddComponent<EnergyPickup>();
            return SavePrefab(root, $"{PrefabFolder}/EnergyPickup.prefab").GetComponent<EnergyPickup>();
        }

        private static EnemyVariantDefinition[] CreateEnemyVariantDefinitions()
        {
            return new[]
            {
                new EnemyVariantDefinition
                {
                    Name = "Scout",
                    MaxHealth = 1,
                    BaseSpeed = 2.8f,
                    ScoreReward = 80,
                    EnergyReward = 5f,
                    PickupChance = 0.18f,
                    EscapedEnergyPenalty = 7f,
                    PredictionTime = 0.22f,
                    SteeringStrength = 2.45f,
                    HorizontalWave = 1.05f,
                    WaveFrequency = 3.15f,
                    MinShotInterval = 2.7f,
                    MaxShotInterval = 4.8f,
                    ProjectileSpeedMultiplier = 0.9f,
                    VisualScale = 0.72f,
                    ColliderRadius = 0.34f,
                    NormalColor = new Color(0.35f, 1f, 0.38f, 1f),
                    HitColor = new Color(1f, 1f, 0.35f, 1f),
                    AttackColor = new Color(0.95f, 1f, 0.28f, 1f),
                    UnlockAfterSeconds = 0f,
                    BaseWeight = 1f,
                    ExtraWeightAtFullRamp = -0.25f
                },
                new EnemyVariantDefinition
                {
                    Name = "Tank",
                    MaxHealth = 5,
                    BaseSpeed = 1.15f,
                    ScoreReward = 230,
                    EnergyReward = 14f,
                    PickupChance = 0.42f,
                    EscapedEnergyPenalty = 18f,
                    PredictionTime = 0.2f,
                    SteeringStrength = 1.35f,
                    HorizontalWave = 0.38f,
                    WaveFrequency = 1.35f,
                    MinShotInterval = 2.4f,
                    MaxShotInterval = 4.5f,
                    ProjectileSpeedMultiplier = 0.85f,
                    VisualScale = 1.22f,
                    ColliderRadius = 0.54f,
                    NormalColor = new Color(1f, 0.12f, 0.22f, 1f),
                    HitColor = new Color(1f, 0.95f, 0.25f, 1f),
                    AttackColor = new Color(1f, 0.48f, 0.12f, 1f),
                    UnlockAfterSeconds = 30f,
                    BaseWeight = 0.12f,
                    ExtraWeightAtFullRamp = 0.24f
                },
                new EnemyVariantDefinition
                {
                    Name = "Sniper",
                    MaxHealth = 2,
                    BaseSpeed = 1.55f,
                    ScoreReward = 170,
                    EnergyReward = 9f,
                    PickupChance = 0.26f,
                    EscapedEnergyPenalty = 12f,
                    PredictionTime = 0.82f,
                    SteeringStrength = 1.75f,
                    HorizontalWave = 0.2f,
                    WaveFrequency = 1.9f,
                    MinShotInterval = 1.45f,
                    MaxShotInterval = 2.55f,
                    ProjectileSpeedMultiplier = 1.2f,
                    VisualScale = 0.88f,
                    ColliderRadius = 0.39f,
                    NormalColor = new Color(0.2f, 0.58f, 1f, 1f),
                    HitColor = new Color(1f, 1f, 0.35f, 1f),
                    AttackColor = new Color(0.46f, 0.9f, 1f, 1f),
                    UnlockAfterSeconds = 55f,
                    BaseWeight = 0.08f,
                    ExtraWeightAtFullRamp = 0.2f
                }
            };
        }

        private static EnemyController[] CreateEnemyPrefabs(Sprite sprite, EnemyProjectile projectilePrefab, EnergyPickup pickupPrefab, EnemyVariantDefinition[] definitions)
        {
            EnemyController[] prefabs = new EnemyController[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                EnemyVariantConfig config = CreateEnemyVariantConfig(definitions[i]);
                prefabs[i] = CreateEnemyPrefab(sprite, projectilePrefab, pickupPrefab, definitions[i], config);
            }

            return prefabs;
        }

        private static EnemyVariantConfig CreateEnemyVariantConfig(EnemyVariantDefinition definition)
        {
            string path = $"{VariantFolder}/{definition.Name}.asset";
            AssetDatabase.DeleteAsset(path);

            EnemyVariantConfig config = ScriptableObject.CreateInstance<EnemyVariantConfig>();
            SerializedObject serialized = new SerializedObject(config);
            SetString(serialized, "displayName", definition.Name);
            SetInt(serialized, "maxHealth", definition.MaxHealth);
            SetFloat(serialized, "baseSpeed", definition.BaseSpeed);
            SetInt(serialized, "scoreReward", definition.ScoreReward);
            SetFloat(serialized, "energyReward", definition.EnergyReward);
            SetFloat(serialized, "pickupChance", definition.PickupChance);
            SetFloat(serialized, "escapedEnergyPenalty", definition.EscapedEnergyPenalty);
            SetFloat(serialized, "predictionTime", definition.PredictionTime);
            SetFloat(serialized, "steeringStrength", definition.SteeringStrength);
            SetFloat(serialized, "horizontalWave", definition.HorizontalWave);
            SetFloat(serialized, "waveFrequency", definition.WaveFrequency);
            SetFloat(serialized, "minShotInterval", definition.MinShotInterval);
            SetFloat(serialized, "maxShotInterval", definition.MaxShotInterval);
            SetFloat(serialized, "projectileSpeedMultiplier", definition.ProjectileSpeedMultiplier);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        private static EnemyController CreateEnemyPrefab(Sprite sprite, EnemyProjectile projectilePrefab, EnergyPickup pickupPrefab, EnemyVariantDefinition definition, EnemyVariantConfig config)
        {
            GameObject root = new GameObject($"Enemy {definition.Name}");

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = definition.ColliderRadius;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * definition.VisualScale;
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = definition.NormalColor;
            renderer.sortingOrder = 10;

            EnemyVisualAnimator animator = root.AddComponent<EnemyVisualAnimator>();
            SetReference(animator, "visualRoot", visual.transform);
            SetReference(animator, "spriteRenderer", renderer);
            SetColor(animator, "normalColor", definition.NormalColor);
            SetColor(animator, "hitColor", definition.HitColor);
            SetColor(animator, "attackColor", definition.AttackColor);

            EnemyController controller = root.AddComponent<EnemyController>();
            SetReference(controller, "variantConfig", config);
            SetReference(controller, "enemyProjectilePrefab", projectilePrefab);
            SetReference(controller, "pickupPrefab", pickupPrefab);
            SetReference(controller, "visualAnimator", animator);

            return SavePrefab(root, $"{PrefabFolder}/Enemy{definition.Name}.prefab").GetComponent<EnemyController>();
        }

        private static PlayerController CreatePlayerPrefab(Sprite sprite, Sprite ringSprite, PlayerProjectile projectilePrefab)
        {
            GameObject root = new GameObject("Player");

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.38f;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.85f;
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 11;

            GameObject pulse = new GameObject("PulseRing");
            pulse.transform.SetParent(root.transform, false);
            SpriteRenderer pulseRenderer = pulse.AddComponent<SpriteRenderer>();
            pulseRenderer.sprite = ringSprite;
            pulseRenderer.sortingOrder = 9;
            pulse.SetActive(false);

            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.65f, 0f);

            PlayerVisualAnimator animator = root.AddComponent<PlayerVisualAnimator>();
            SetReference(animator, "visualRoot", visual.transform);
            SetReference(animator, "shipRenderer", renderer);
            SetReference(animator, "pulseRenderer", pulseRenderer);

            PlayerController controller = root.AddComponent<PlayerController>();
            SetReference(controller, "projectilePrefab", projectilePrefab);
            SetReference(controller, "muzzle", muzzle.transform);
            SetReference(controller, "visualAnimator", animator);

            return SavePrefab(root, $"{PrefabFolder}/Player.prefab").GetComponent<PlayerController>();
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            AssetDatabase.DeleteAsset(path);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static T LoadPrefabComponent<T>(string path) where T : Component
        {
            T component = AssetDatabase.LoadAssetAtPath<T>(path);
            if (component == null)
            {
                throw new InvalidOperationException($"Nao foi possivel carregar o prefab em {path}.");
            }

            return component;
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.008f, 0.02f, 0.05f, 1f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<CameraEffects>();
            return camera;
        }

        private static void CreateTilemapBackground(Sprite tileSprite)
        {
            string tilePath = $"{GeneratedArtFolder}/BackgroundTile.asset";
            AssetDatabase.DeleteAsset(tilePath);
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = tileSprite;
            tile.color = Color.white;
            AssetDatabase.CreateAsset(tile, tilePath);

            GameObject gridObject = new GameObject("Background Grid", typeof(Grid));
            GameObject tilemapObject = new GameObject("Arena Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
            tilemapObject.transform.SetParent(gridObject.transform, false);

            Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObject.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = -30;

            for (int x = -6; x <= 6; x++)
            {
                for (int y = -6; y <= 6; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        private static void CreateStarfield(Sprite starSprite)
        {
            GameObject starfieldObject = new GameObject("Procedural Starfield");
            Starfield starfield = starfieldObject.AddComponent<Starfield>();
            SetReference(starfield, "starSprite", starSprite);
        }

        private static EnemySpawner CreateSpawner(EnemyController fallbackEnemyPrefab, EnemyController[] enemyPrefabs, EnemyVariantDefinition[] definitions)
        {
            GameObject spawnerObject = new GameObject("Enemy Spawner");
            EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>();
            SetReference(spawner, "enemyPrefab", fallbackEnemyPrefab);
            SetFloat(spawner, "initialInterval", 1.9f);
            SetFloat(spawner, "minimumInterval", 0.75f);
            SetFloat(spawner, "difficultyRampSeconds", 120f);
            SetInt(spawner, "maximumEnemiesAlive", 16);
            SetEnemySpawnEntries(spawner, enemyPrefabs, definitions);
            return spawner;
        }

        private static void CreateGameManager(PlayerController player, EnemySpawner spawner, GameUI ui)
        {
            GameObject managerObject = new GameObject("Game Manager");
            GameManager manager = managerObject.AddComponent<GameManager>();
            SetReference(manager, "player", player);
            SetReference(manager, "enemySpawner", spawner);
            SetReference(manager, "gameUI", ui);
        }

        private static GameUI CreateUI()
        {
            GameObject canvasObject = new GameObject("Game UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameUI ui = canvasObject.AddComponent<GameUI>();
            uiFont = GetUIFont();

            ui.mainMenuPanel = CreatePanel(canvasObject.transform, "Main Menu", new Color(0.01f, 0.025f, 0.07f, 0.96f));
            CreateText(ui.mainMenuPanel.transform, "Title", "SKY\nSURVIVOR", 92, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.9f), new Color(0.2f, 0.95f, 1f));
            CreateText(ui.mainMenuPanel.transform, "Subtitle", "SHOOT 'EM UP SURVIVAL", 31, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.61f), new Vector2(0.9f, 0.68f), new Color(1f, 0.3f, 0.65f));
            ui.mainMenuHighScoreText = CreateText(ui.mainMenuPanel.transform, "High Score", "RECORDE: 000000", 30, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.54f), new Vector2(0.9f, 0.6f), Color.white);
            CreateText(ui.mainMenuPanel.transform, "Instructions", "WASD / SETAS  ·  MOVER\nESPAÇO / MOUSE  ·  ATIRAR\nSHIFT  ·  DASH (-25 ENERGIA)\nE  ·  PULSO EMP (-60 ENERGIA)\nESC  ·  PAUSAR", 28, TextAnchor.MiddleCenter, new Vector2(0.12f, 0.27f), new Vector2(0.88f, 0.51f), new Color(0.82f, 0.9f, 1f));
            ui.startButton = CreateButton(ui.mainMenuPanel.transform, "Start Button", "INICIAR", new Vector2(0.23f, 0.14f), new Vector2(0.77f, 0.22f));
            ui.quitButton = CreateButton(ui.mainMenuPanel.transform, "Quit Button", "SAIR", new Vector2(0.31f, 0.06f), new Vector2(0.69f, 0.115f));

            ui.hudPanel = CreatePanel(canvasObject.transform, "HUD", Color.clear, false);
            ui.scoreText = CreateText(ui.hudPanel.transform, "Score", "SCORE 000000", 29, TextAnchor.UpperLeft, new Vector2(0.035f, 0.91f), new Vector2(0.49f, 0.985f), Color.white);
            ui.highScoreText = CreateText(ui.hudPanel.transform, "Best", "BEST 000000", 24, TextAnchor.UpperLeft, new Vector2(0.035f, 0.865f), new Vector2(0.49f, 0.925f), new Color(0.65f, 0.8f, 1f));
            ui.timeText = CreateText(ui.hudPanel.transform, "Time", "00:00", 31, TextAnchor.UpperRight, new Vector2(0.67f, 0.91f), new Vector2(0.965f, 0.985f), Color.white);
            ui.comboText = CreateText(ui.hudPanel.transform, "Combo", string.Empty, 34, TextAnchor.UpperCenter, new Vector2(0.25f, 0.81f), new Vector2(0.75f, 0.88f), new Color(1f, 0.35f, 0.68f));
            ui.healthSlider = CreateSlider(ui.hudPanel.transform, new Vector2(0.08f, 0.095f), new Vector2(0.92f, 0.135f), new Color(1f, 0.16f, 0.35f, 1f));
            ui.healthText = CreateText(ui.hudPanel.transform, "Health Text", "VIDA 3/3", 24, TextAnchor.MiddleCenter, new Vector2(0.18f, 0.135f), new Vector2(0.82f, 0.175f), new Color(1f, 0.45f, 0.6f));
            ui.energySlider = CreateSlider(ui.hudPanel.transform, new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.075f), new Color(0.15f, 0.95f, 0.62f, 1f));
            ui.energyText = CreateText(ui.hudPanel.transform, "Energy Text", "ENERGIA 50/100", 24, TextAnchor.MiddleCenter, new Vector2(0.18f, 0.075f), new Vector2(0.82f, 0.115f), new Color(0.3f, 1f, 0.75f));

            ui.pausePanel = CreatePanel(canvasObject.transform, "Pause", new Color(0.005f, 0.01f, 0.03f, 0.9f));
            CreateText(ui.pausePanel.transform, "Pause Title", "PAUSADO", 78, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.66f), new Vector2(0.9f, 0.79f), new Color(0.2f, 0.95f, 1f));
            ui.resumeButton = CreateButton(ui.pausePanel.transform, "Resume Button", "CONTINUAR", new Vector2(0.22f, 0.47f), new Vector2(0.78f, 0.55f));
            ui.pauseRestartButton = CreateButton(ui.pausePanel.transform, "Restart Button", "RECOMEÇAR", new Vector2(0.22f, 0.36f), new Vector2(0.78f, 0.44f));
            ui.pauseMenuButton = CreateButton(ui.pausePanel.transform, "Menu Button", "MENU PRINCIPAL", new Vector2(0.22f, 0.25f), new Vector2(0.78f, 0.33f));

            ui.gameOverPanel = CreatePanel(canvasObject.transform, "Game Over", new Color(0.035f, 0.006f, 0.03f, 0.94f));
            CreateText(ui.gameOverPanel.transform, "Game Over Title", "GAME OVER", 85, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.7f), new Vector2(0.92f, 0.83f), new Color(1f, 0.2f, 0.48f));
            ui.finalScoreText = CreateText(ui.gameOverPanel.transform, "Final Score", "PONTUAÇÃO: 000000", 36, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.56f), new Vector2(0.9f, 0.63f), Color.white);
            ui.finalHighScoreText = CreateText(ui.gameOverPanel.transform, "Final High Score", "RECORDE: 000000", 30, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.49f), new Vector2(0.9f, 0.55f), new Color(0.3f, 1f, 0.8f));
            ui.finalTimeText = CreateText(ui.gameOverPanel.transform, "Final Time", "TEMPO: 00:00", 30, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.48f), new Color(0.75f, 0.85f, 1f));
            ui.restartButton = CreateButton(ui.gameOverPanel.transform, "Restart Button", "JOGAR NOVAMENTE", new Vector2(0.2f, 0.26f), new Vector2(0.8f, 0.35f));
            ui.gameOverMenuButton = CreateButton(ui.gameOverPanel.transform, "Menu Button", "MENU PRINCIPAL", new Vector2(0.25f, 0.16f), new Vector2(0.75f, 0.23f));

            ui.hudPanel.SetActive(false);
            ui.pausePanel.SetActive(false);
            ui.gameOverPanel.SetActive(false);
            return ui;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color, bool raycastTarget = true)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return panel;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = uiFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, fontSize / 2);
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.05f, 0.35f, 0.5f, 0.95f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.05f, 0.35f, 0.5f, 0.95f);
            colors.highlightedColor = new Color(0.08f, 0.55f, 0.7f, 1f);
            colors.pressedColor = new Color(1f, 0.15f, 0.5f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            CreateText(buttonObject.transform, "Label", label, 31, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Color.white);
            return button;
        }

        private static Slider CreateSlider(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color fillColor)
        {
            GameObject sliderObject = new GameObject("Energy Slider", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            background.transform.SetParent(sliderObject.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.2f, 0.95f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0.015f, 0.15f);
            fillAreaRect.anchorMax = new Vector2(0.985f, 0.85f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = fillColor;

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = background.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 50f;
            slider.interactable = false;
            return slider;
        }

        private static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static Font GetUIFont()
        {
            Font font = null;
            try
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch (Exception)
            {
            }

            if (font == null)
            {
                try
                {
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                catch (Exception)
                {
                }
            }

            return font;
        }

        private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Property '{propertyName}' not found on {target.name}.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(UnityEngine.Object target, string propertyName, Color value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Property '{propertyName}' not found on {target.name}.");
                return;
            }

            property.colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(UnityEngine.Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Property '{propertyName}' not found on {target.name}.");
                return;
            }

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Property '{propertyName}' not found on {target.name}.");
                return;
            }

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnemySpawnEntries(EnemySpawner spawner, EnemyController[] prefabs, EnemyVariantDefinition[] definitions)
        {
            SerializedObject serialized = new SerializedObject(spawner);
            SerializedProperty variants = serialized.FindProperty("enemyVariants");
            if (variants == null)
            {
                Debug.LogError("Property 'enemyVariants' not found on EnemySpawner.");
                return;
            }

            variants.arraySize = prefabs.Length;
            for (int i = 0; i < prefabs.Length; i++)
            {
                SerializedProperty entry = variants.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefabs[i];
                entry.FindPropertyRelative("unlockAfterSeconds").floatValue = definitions[i].UnlockAfterSeconds;
                entry.FindPropertyRelative("baseWeight").floatValue = definitions[i].BaseWeight;
                entry.FindPropertyRelative("extraWeightAtFullRamp").floatValue = definitions[i].ExtraWeightAtFullRamp;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }
    }
}
#endif

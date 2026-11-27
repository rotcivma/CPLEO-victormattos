using System;
using Propulsor.Core;
using Propulsor.Core.Gameplay;
using Propulsor.Core.Save;
using Propulsor.Data;
using Propulsor.Gameplay;
using Propulsor.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Propulsor.Bootstrap
{
    /// <summary>
    /// Monta a cena de jogo em código, para que o projeto rode sem cenas/prefabs
    /// serializados: basta abrir qualquer cena vazia e apertar Play.
    /// Se a cena já tiver um <see cref="RunDriver"/>, nada é feito.
    /// </summary>
    public static class PrototypeBootstrap
    {
        /// <summary>Desligue para cenas que não devem receber o protótipo automaticamente.</summary>
        public static bool AutoBuildEnabled = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBuild()
        {
            if (!AutoBuildEnabled) return;
            if (Object.FindAnyObjectByType<RunDriver>() != null) return;
            BuildGame(new PlayerInput(), Resources.Load<PlayerPhysicsDef>("PlayerPhysics"));
        }

        /// <summary>
        /// Jogo completo: corrida + fluxo de telas (splash → carregamento → menu → corrida → ...).
        /// </summary>
        /// <param name="storage">Onde salvar o perfil (null = arquivo em Application.persistentDataPath).</param>
        public static FlowController BuildGame(IThrustInput input, PlayerPhysicsDef def = null,
                                               Func<RunConfig> configFactory = null, ISaveStorage storage = null)
        {
            var driver = Build(input, def, configFactory, debugHud: false);
            driver.DebugKeys = Application.isEditor || Debug.isDebugBuild;

            var save = new SaveManager(storage ?? new FileSaveStorage(Application.persistentDataPath));
            var services = new GameServices(driver.Bus, save);
            if (save.LastOutcome == LoadOutcome.LoadedFromBackup)
                Debug.LogWarning("Perfil principal corrompido; carregado da cópia de segurança. " + save.LastError);

            var flow = driver.gameObject.AddComponent<FlowController>();
            flow.Bind(driver, services);

            var audio = new GameObject("Audio").AddComponent<AudioManager>();
            audio.Bind(driver.Bus, services.Profile);
            flow.Audio = audio.Director;

            var playerView = Object.FindAnyObjectByType<PlayerView>();
            playerView.ApplyAppearance(services.Inventory);
            flow.AppearanceChanged += () =>
            {
                playerView.ApplyAppearance(services.Inventory);
                audio.UseProfile(services.Profile); // o perfil muda ao apagar o progresso
            };

            new GameObject("Interface").AddComponent<UIManager>().Bind(flow);
            return flow;
        }

        /// <summary>Cria câmera, corredor, simulação e views (sem menus). Retorna o condutor da corrida.</summary>
        /// <param name="configFactory">Gera a configuração de cada corrida (null = padrão).</param>
        /// <param name="debugHud">Mostra o HUD de depuração (usado sem menus).</param>
        public static RunDriver Build(IThrustInput input, PlayerPhysicsDef def = null, Func<RunConfig> configFactory = null,
                                      bool debugHud = true)
        {
            Application.targetFrameRate = 60;
            if (def == null) def = ScriptableObject.CreateInstance<PlayerPhysicsDef>();

            var square = MakeSquareSprite();
            var circle = MakeCircleSprite();
            var font = LoadBuiltinFont();

            // Câmera
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }
            cam.backgroundColor = new Color32(0x1B, 0x1F, 0x3B, 255);
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Não usar "??" com UnityEngine.Object (null "falso" do Unity).
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();

            // Simulação
            var driver = new GameObject("Corrida").AddComponent<RunDriver>();
            driver.Configure(def, input, configFactory);

            // Protagonista (Nina — personagem original; arte provisória geométrica)
            var playerGo = new GameObject("Jogador");
            var onFoot = new GameObject("APe").transform;
            onFoot.SetParent(playerGo.transform, false);
            var bodyGo = new GameObject("Corpo");
            bodyGo.transform.SetParent(onFoot, false);
            var body = bodyGo.AddComponent<SpriteRenderer>();
            body.sprite = square;
            body.color = new Color32(0x2E, 0xC4, 0xB6, 255);
            body.sortingOrder = 10;
            bodyGo.transform.localScale = new Vector3(0.55f, def.playerHeight, 1f);
            bodyGo.transform.localPosition = new Vector3(0f, def.playerHeight * 0.5f, 0f);

            var pack = MakeChild(bodyGo.transform, "Mochila", square, new Color32(0xFF, 0x9F, 0x1C, 255), 9,
                                 new Vector3(-0.7f, 0f, 0f), new Vector3(0.5f, 0.55f, 1f));
            MakeChild(bodyGo.transform, "Oculos", square, new Color32(0x1B, 0x1F, 0x3B, 255), 11,
                      new Vector3(0.2f, 0.3f, 0f), new Vector3(0.6f, 0.1f, 1f));
            var particles = MakeThrustParticles(pack);

            var playerView = playerGo.AddComponent<PlayerView>();
            playerView.Bind(driver, onFoot, body, particles, square);

            follow.Configure(playerGo.transform, def.ceilingY - def.floorY + 2f, (def.floorY + def.ceilingY) * 0.5f);

            // Cenário e mundo
            var corridor = new GameObject("Corredor").AddComponent<CorridorView>();
            corridor.Build(cam.transform, def.floorY, def.ceilingY, square, font);
            new GameObject("Mundo").AddComponent<WorldView>().Bind(driver, cam, square, circle, follow, font);

            // HUD de depuração (apenas no modo sem menus)
            if (debugHud) new GameObject("DebugHud").AddComponent<DebugHud>().Bind(driver);

            return driver;
        }

        private static Transform MakeChild(Transform parent, string name, Sprite sprite, Color color, int order,
                                           Vector3 localPos, Vector3 localScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return go.transform;
        }

        private static ParticleSystem MakeThrustParticles(Transform pack)
        {
            var go = new GameObject("Jato");
            go.transform.SetParent(pack, false);
            go.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // emite para baixo

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 6f;
            main.startSize = 0.18f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color32(0xFF, 0xD2, 0x3F, 255),
                                                                new Color32(0xE7, 0x1D, 0x36, 255));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var emission = ps.emission;
            emission.rateOverTime = 80f;
            emission.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.05f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingOrder = 8;
            ps.Play();
            return ps;
        }

        private static Sprite MakeSquareSprite()
        {
            var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point, name = "Quadrado" };
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private static Sprite MakeCircleSprite()
        {
            const int n = 64;
            var tex = new Texture2D(n, n) { filterMode = FilterMode.Bilinear, name = "Circulo" };
            var pixels = new Color[n * n];
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                    pixels[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d));
                }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        private static Font LoadBuiltinFont()
        {
            // Unity 2022.2+ usa LegacyRuntime.ttf; versões antigas, Arial.ttf.
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}

using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// Corredor infinito do protótipo: chão, teto, faixas de fundo com parallax e
    /// marcos a cada 10 m. Tudo reciclado (sem instanciar durante a corrida).
    /// Arte provisória geométrica e original.
    /// </summary>
    [DefaultExecutionOrder(100)] // ordem: jogador → câmera (0) → cenário
    public sealed class CorridorView : MonoBehaviour
    {
        private Transform _cam;
        private Transform _floor, _ceiling, _backWall;
        private Transform[] _pillars;
        private Transform[] _markers;
        private TextMesh[] _markerLabels;
        private float _floorY, _ceilingY;
        private const float PillarSpacing = 6f;
        private const float MarkerSpacing = 10f;
        private const float Parallax = 0.5f; // pilares do fundo movem-se a 50 % da velocidade

        // ---- arte gerada: blocos de parallax por setor (Resources/Art/Backgrounds/Tiles) ----
        private static readonly string[] Sectors = { "Montagem", "Caldeiras", "Testes", "Deposito" };
        private const float SectorLength = 400f;
        private sealed class TileLayer
        {
            public string Prefix; public float Width, K; public int PerSector, Order;
            public readonly System.Collections.Generic.List<SpriteRenderer> Pool = new System.Collections.Generic.List<SpriteRenderer>();
        }
        private TileLayer[] _layers;
        private SpriteRenderer[] _floorBand, _ceilBand;
        private bool _useTiles;

        public void Build(Transform cameraTransform, float floorY, float ceilingY, Sprite square, Font font)
        {
            _cam = cameraTransform;
            _floorY = floorY;
            _ceilingY = ceilingY;

            _backWall = MakeQuad("ParedeFundo", square, new Color32(0x2B, 0x31, 0x63, 255), -20);
            _floor = MakeQuad("Chao", square, new Color32(0x1B, 0x1F, 0x3B, 255), 5);
            _ceiling = MakeQuad("Teto", square, new Color32(0x1B, 0x1F, 0x3B, 255), 5);

            _pillars = new Transform[12];
            for (int i = 0; i < _pillars.Length; i++)
                _pillars[i] = MakeQuad("Pilar" + i, square, new Color32(0x3A, 0x41, 0x80, 255), -10);

            _useTiles = SpriteLibrary.Tile("Floor") != null && SpriteLibrary.Tile("Far_Montagem_0") != null;
            if (_useTiles)
            {
                _backWall.gameObject.SetActive(false);
                _floor.gameObject.SetActive(false);
                _ceiling.gameObject.SetActive(false);
                foreach (var pl in _pillars) pl.gameObject.SetActive(false);
                _layers = new[]
                {
                    new TileLayer { Prefix = "Far", Width = 10f, K = .22f, PerSector = 4, Order = -30 },
                    new TileLayer { Prefix = "Mid", Width = 7f, K = .55f, PerSector = 6, Order = -25 },
                };
                _floorBand = Band("Chao", "Floor", 6);
                _ceilBand = Band("Teto", "Ceiling", 6);
            }

            _markers = new Transform[8];
            _markerLabels = new TextMesh[_markers.Length];
            for (int i = 0; i < _markers.Length; i++)
            {
                _markers[i] = MakeQuad("Marco" + i, square, new Color32(0xFF, 0x9F, 0x1C, 255), -5);
                var label = new GameObject("Rotulo");
                label.transform.SetParent(_markers[i], false);
                var tm = label.AddComponent<TextMesh>();
                tm.font = font;
                tm.GetComponent<MeshRenderer>().sharedMaterial = font != null ? font.material : null;
                tm.GetComponent<MeshRenderer>().sortingOrder = -4;
                tm.anchor = TextAnchor.LowerCenter;
                tm.characterSize = 0.12f;
                tm.fontSize = 48;
                tm.color = new Color32(0xF6, 0xF7, 0xFB, 255);
                _markerLabels[i] = tm;
            }
        }

        private SpriteRenderer[] Band(string name, string tile, int order)
        {
            var arr = new SpriteRenderer[4];
            for (int i = 0; i < arr.Length; i++)
            {
                var sr = MakeQuad(name + i, SpriteLibrary.Tile(tile), Color.white, order).GetComponent<SpriteRenderer>();
                arr[i] = sr;
            }
            return arr;
        }

        private static int Sector(float x) => ((int)Mathf.Floor(x / SectorLength) % 4 + 4) % 4;

        private void LateUpdateTiles(float camX, float halfW)
        {
            float viewW = halfW * 2f;
            foreach (var L in _layers)
            {
                float layerX = camX * L.K;
                int i0 = Mathf.FloorToInt((layerX - halfW) / L.Width) - 1;
                int count = Mathf.CeilToInt(viewW / L.Width) + 3;
                while (L.Pool.Count < count) L.Pool.Add(MakeQuad(L.Prefix + L.Pool.Count, null, Color.white, L.Order).GetComponent<SpriteRenderer>());
                for (int k = 0; k < L.Pool.Count; k++)
                {
                    int i = i0 + k;
                    var sr = L.Pool[k];
                    if (k >= count) { sr.enabled = false; continue; }
                    int variant = ((i * 7919) % L.PerSector + L.PerSector) % L.PerSector;
                    sr.sprite = SpriteLibrary.Tile($"{L.Prefix}_{Sectors[Sector(i * L.Width / L.K)]}_{variant}");
                    sr.enabled = sr.sprite != null;
                    // bloco com pivô no canto inferior esquerdo, base em y = -1
                    sr.transform.position = new Vector3(i * L.Width + camX * (1f - L.K), _floorY - 1f, 0f);
                    sr.transform.localScale = Vector3.one;
                }
            }
            PlaceBand(_floorBand, camX, halfW, -1.5f);
            PlaceBand(_ceilBand, camX, halfW, 6.75f);
        }

        private static void PlaceBand(SpriteRenderer[] band, float camX, float halfW, float y)
        {
            const float w = 18f;
            float start = Mathf.Floor((camX - halfW) / w) * w;
            for (int k = 0; k < band.Length; k++)
            {
                band[k].transform.position = new Vector3(start + k * w, y, 0f);
                band[k].transform.localScale = Vector3.one;
            }
        }

        private Transform MakeQuad(string name, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return go.transform;
        }

        private void LateUpdate()
        {
            if (_cam == null) return;
            var cam = _cam.GetComponent<Camera>();
            float halfW = cam.orthographicSize * cam.aspect;
            float camX = _cam.position.x;
            float width = halfW * 2f + 4f;

            if (_useTiles) LateUpdateTiles(camX, halfW);
            Place(_backWall, camX, (_floorY + _ceilingY) * 0.5f, width, _ceilingY - _floorY);
            Place(_floor, camX, _floorY - 1f, width, 2f);
            Place(_ceiling, camX, _ceilingY + 1f, width, 2f);

            // Pilares com parallax: posição no "espaço do fundo".
            float bgX = camX * Parallax;
            float firstPillar = Mathf.Floor((bgX - halfW - 2f) / PillarSpacing) * PillarSpacing;
            for (int i = 0; i < _pillars.Length; i++)
            {
                float worldX = firstPillar + i * PillarSpacing + camX * (1f - Parallax);
                Place(_pillars[i], worldX, (_floorY + _ceilingY) * 0.5f, 0.6f, _ceilingY - _floorY);
            }

            // Marcos de distância a cada 10 m (presos ao mundo).
            float firstMarker = Mathf.Max(0f, Mathf.Floor((camX - halfW) / MarkerSpacing) * MarkerSpacing);
            for (int i = 0; i < _markers.Length; i++)
            {
                float mx = firstMarker + i * MarkerSpacing;
                Place(_markers[i], mx, _floorY + 0.15f, 0.08f, 0.3f);
                _markerLabels[i].transform.localScale = new Vector3(1f / 0.08f, 1f / 0.3f, 1f);
                _markerLabels[i].transform.localPosition = new Vector3(0f, 0.6f, 0f);
                _markerLabels[i].text = ((int)mx) + " m";
            }
        }

        private static void Place(Transform t, float x, float y, float w, float h)
        {
            t.position = new Vector3(x, y, 0f);
            t.localScale = new Vector3(w, h, 1f);
        }
    }
}

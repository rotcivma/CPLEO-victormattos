using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// Corredor infinito do protótipo: chão, teto, faixas de fundo com parallax e
    /// marcos a cada 10 m. Tudo reciclado (sem instanciar durante a corrida).
    /// Arte provisória geométrica e original.
    /// </summary>
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

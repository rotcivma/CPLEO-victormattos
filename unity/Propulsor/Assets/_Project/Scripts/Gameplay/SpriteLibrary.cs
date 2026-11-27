using System.Collections.Generic;
using Propulsor.Core.Save;
using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// Carrega as folhas de sprites geradas por <c>web/tools/bake-sprites.js</c>
    /// (Resources/Art + manifest.json) e as fatia em <see cref="Sprite"/>s em tempo de execução.
    /// Se os arquivos não existirem, <see cref="Available"/> é falso e as views usam a arte geométrica.
    /// </summary>
    public static class SpriteLibrary
    {
        public sealed class Anim
        {
            public Sprite[] Frames;
            public float Fps;
            public Sprite Frame(float time)
            {
                if (Frames.Length == 1 || Fps <= 0f) return Frames[0];
                int i = (int)(time * Fps) % Frames.Length;
                return Frames[i < 0 ? i + Frames.Length : i];
            }
        }

        private static Dictionary<string, Anim> _anims;
        private static readonly Dictionary<string, Sprite> _single = new Dictionary<string, Sprite>();
        private static IDictionary<string, object> _tilesInfo;

        public static bool Available => Load() && _anims.Count > 0;

        /// <summary>Animação pelo nome da folha (ex.: "Nina_run", "Vehicle_mech", "Nut"). Nulo se não houver.</summary>
        public static Anim Get(string name)
        {
            if (!Load()) return null;
            return _anims.TryGetValue(name, out var a) ? a : null;
        }

        /// <summary>Sprite único de um arquivo em Resources/Art (pivot em fração, y de baixo).</summary>
        public static Sprite Single(string path, float ppu, Vector2 pivot)
        {
            if (_single.TryGetValue(path, out var s)) return s;
            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu, 0, SpriteMeshType.FullRect);
            }
            _single[path] = s;
            return s;
        }

        /// <summary>Bloco de parallax/faixa: "Far_Montagem_0", "Mid_Testes_3", "Floor", "Ceiling".</summary>
        public static Sprite Tile(string name) =>
            Single("Art/Backgrounds/Tiles/" + name, TilePpu, Vector2.zero);

        public static float TilePpu => _tilesInfo != null ? _tilesInfo.GetFloat("pixelsPerUnit", 64f) : 64f;

        private static bool Load()
        {
            if (_anims != null) return true;
            _anims = new Dictionary<string, Anim>();
            var manifest = Resources.Load<TextAsset>("Art/manifest");
            if (manifest == null) return true;
            IDictionary<string, object> root;
            try { root = MiniJson.Parse(manifest.text) as IDictionary<string, object>; }
            catch (System.FormatException e) { Debug.LogWarning("PROPULSOR: manifest de arte inválido: " + e.Message); return true; }
            _tilesInfo = root.GetObject("tiles");

            foreach (var o in root.GetList("sheets") ?? new List<object>())
            {
                if (!(o is IDictionary<string, object> s)) continue;
                var tex = Resources.Load<Texture2D>(s.GetString("path"));
                if (tex == null) continue;
                tex.wrapMode = TextureWrapMode.Clamp;
                int n = s.GetInt("frames", 1), fw = s.GetInt("frameWidth"), fh = s.GetInt("frameHeight");
                var pivot = new Vector2(s.GetFloat("pivotX", .5f), s.GetFloat("pivotY", .5f));
                float ppu = s.GetFloat("pixelsPerUnit", 100f);
                var frames = new Sprite[n];
                for (int i = 0; i < n; i++)
                    frames[i] = Sprite.Create(tex, new Rect(i * fw, 0, fw, fh), pivot, ppu, 0, SpriteMeshType.FullRect);
                _anims[s.GetString("name")] = new Anim { Frames = frames, Fps = s.GetFloat("fps", 10f) };
            }
            return true;
        }
    }
}

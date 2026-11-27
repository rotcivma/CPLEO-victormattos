using System.Collections.Generic;
using Propulsor.Core.Audio;
using Propulsor.Core.Events;
using Propulsor.Core.Flow;
using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// Saída de áudio do Unity: toca os sons sintetizados que o <see cref="AudioDirector"/>
    /// (lógica pura) pede. Pool de fontes para efeitos, duas fontes de música com
    /// transição cruzada (0,5 s), som contínuo do propulsor e volumes do perfil.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour, IAudioOutput
    {
        private const int SfxVoices = 10;
        private const float CrossfadeTime = 0.5f;

        public AudioDirector Director { get; private set; }

        private readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        private readonly Dictionary<Music, AudioClip> _music = new Dictionary<Music, AudioClip>();
        private readonly List<AudioSource> _voices = new List<AudioSource>();
        private AudioSource _musicA, _musicB, _jet;
        private SessionProfile _profile;
        private float _duckDb, _duckCurrent;
        private float _fade = 1f;
        private int _next;

        public void Bind(EventBus bus, SessionProfile profile)
        {
            _profile = profile;
            foreach (Sfx s in System.Enum.GetValues(typeof(Sfx))) _clips[s] = ProceduralSynth.Make(s);

            for (int i = 0; i < SfxVoices; i++) _voices.Add(NewSource(false));
            _musicA = NewSource(true);
            _musicB = NewSource(true);
            _jet = NewSource(true);
            _jet.clip = ProceduralSynth.JetLoop();

            Director = new AudioDirector(bus, this);
        }

        /// <summary>Troca o perfil (ex.: após apagar o progresso).</summary>
        public void UseProfile(SessionProfile profile) => _profile = profile;

        private AudioSource NewSource(bool loop)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            return src;
        }

        private float SfxVol => _profile?.SfxVolume ?? 1f;
        private float MusicVol => _profile?.MusicVolume ?? 0.8f;

        public void PlaySfx(Sfx sfx, float volume, float pitch)
        {
            if (!_clips.TryGetValue(sfx, out var clip)) return;
            var v = _voices[_next];
            _next = (_next + 1) % _voices.Count;
            v.pitch = pitch;
            v.PlayOneShot(clip, volume * SfxVol);
        }

        public void SetMusic(Music music)
        {
            // Nova música entra na fonte B e cruza com a A.
            var tmp = _musicA; _musicA = _musicB; _musicB = tmp;
            if (music == Music.None) { _musicA.Stop(); }
            else
            {
                if (!_music.TryGetValue(music, out var clip)) _music[music] = clip = ProceduralSynth.MakeMusic(music);
                _musicA.clip = clip;
                _musicA.volume = 0f;
                _musicA.Play();
            }
            _fade = 0f;
        }

        public void SetMusicDuck(float db) => _duckDb = db;

        public void SetJetLoop(bool on)
        {
            if (on && !_jet.isPlaying) _jet.Play();
            else if (!on && _jet.isPlaying) _jet.Stop();
        }

        private void Update()
        {
            if (Director == null) return;
            float dt = Time.unscaledDeltaTime;
            Director.Tick(dt);

            _duckCurrent = Mathf.MoveTowards(_duckCurrent, _duckDb, 40f * dt); // suaviza o ducking
            float duck = Mathf.Pow(10f, _duckCurrent / 20f);
            _fade = Mathf.Min(1f, _fade + dt / CrossfadeTime);
            float baseVol = MusicVol * 0.6f * duck;
            _musicA.volume = baseVol * _fade;
            _musicB.volume = baseVol * (1f - _fade);
            if (_fade >= 1f && _musicB.isPlaying) _musicB.Stop();
            _jet.volume = 0.25f * SfxVol;
        }
    }
}

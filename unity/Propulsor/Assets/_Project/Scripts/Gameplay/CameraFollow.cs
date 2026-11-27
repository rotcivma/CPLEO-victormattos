using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// Câmera ortográfica que segue apenas o eixo X, mantendo o alvo a uma fração
    /// fixa da largura da tela (22 %). A altura visível é constante em qualquer
    /// proporção de tela — a largura visível é que varia (docs/fase-2/01 §3).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("Posição horizontal do alvo na tela (0 = borda esquerda, 1 = direita).")]
        [Range(0f, 1f)] [SerializeField] private float targetScreenX = 0.22f;
        [Tooltip("Altura total visível em unidades do mundo (corredor + margens).")]
        [SerializeField] private float visibleHeight = 9f;
        [Tooltip("Centro vertical da câmera em unidades do mundo.")]
        [SerializeField] private float centerY = 3.5f;

        private Camera _cam;
        private float _shakeTime, _shakeDuration, _shakeAmplitude;

        public void Configure(Transform followTarget, float height, float yCenter)
        {
            target = followTarget;
            visibleHeight = height;
            centerY = yCenter;
        }

        /// <summary>Tremor de câmera (ex.: explosão de veículo).</summary>
        public void Shake(float amplitude, float duration)
        {
            _shakeAmplitude = amplitude;
            _shakeDuration = _shakeTime = duration;
        }

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            _cam.orthographicSize = visibleHeight * 0.5f;
            float visibleWidth = visibleHeight * _cam.aspect;

            // O centro da câmera fica à direita do alvo, de modo que ele apareça em targetScreenX.
            float x = target.position.x + (0.5f - targetScreenX) * visibleWidth;
            Vector3 offset = Vector3.zero;
            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.deltaTime;
                float k = _shakeAmplitude * (_shakeTime / _shakeDuration);
                offset = new Vector3(Random.Range(-k, k), Random.Range(-k, k), 0f);
            }
            transform.position = new Vector3(x, centerY, -10f) + offset;
        }
    }
}

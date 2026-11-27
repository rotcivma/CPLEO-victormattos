using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Propulsor.Gameplay
{
    /// <summary>
    /// Entrada de botão único: toque na tela, botão esquerdo do mouse ou barra de espaço.
    /// Funciona com o Input Manager antigo e com o novo Input System.
    /// </summary>
    public interface IThrustInput
    {
        /// <summary>true enquanto o botão está pressionado.</summary>
        bool IsHeld { get; }
        /// <summary>true apenas no quadro em que o botão foi pressionado.</summary>
        bool PressedThisFrame { get; }
    }

    public sealed class PlayerInput : IThrustInput
    {
        public bool IsHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
                if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
                if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) return true;
                return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
                if (Input.touchCount > 0) return true;
                return Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
#else
                return false;
#endif
            }
        }

        public bool PressedThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
                if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
                return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
                for (int i = 0; i < Input.touchCount; i++)
                    if (Input.GetTouch(i).phase == TouchPhase.Began) return true;
                return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
#else
                return false;
#endif
            }
        }

        /// <summary>Teclas de depuração do protótipo (K = matar, R = reiniciar).</summary>
        public static bool DebugKeyDown(char key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            switch (char.ToUpperInvariant(key))
            {
                case 'K': return Keyboard.current.kKey.wasPressedThisFrame;
                case 'R': return Keyboard.current.rKey.wasPressedThisFrame;
                default: return false;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch (char.ToUpperInvariant(key))
            {
                case 'K': return Input.GetKeyDown(KeyCode.K);
                case 'R': return Input.GetKeyDown(KeyCode.R);
                default: return false;
            }
#else
            return false;
#endif
        }
    }
}

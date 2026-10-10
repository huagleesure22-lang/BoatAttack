using UnityEngine;
using UnityEngine.InputSystem;

namespace Tidebound
{
    // The repository enables only the new Input System. Do not change its global input setting.
    internal static class TideInput
    {
        public static Vector2 Move
        {
            get
            {
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                return Vector2.ClampMagnitude(new Vector2(
                    (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) -
                    (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) -
                    (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0)), 1);
            }
        }
        public static Vector2 Look => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        public static bool Jump => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        public static bool Wave => Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
        public static bool Interact => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        public static bool Pause => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        public static bool Restart => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        public static bool Fire => Mouse.current != null && Mouse.current.leftButton.isPressed;
        public static bool Sprint => Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
    }
}

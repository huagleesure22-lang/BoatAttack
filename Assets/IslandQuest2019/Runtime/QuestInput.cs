using UnityEngine;
#if ENABLE_INPUT_SYSTEM || ISLANDQUEST_NEW_INPUT
using UnityEngine.InputSystem;
#endif

namespace IslandQuest2019
{
    // Prefer the enabled Input System. Legacy calls are compiled only when it is disabled.
    public static class QuestInput
    {
        public static bool Held(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM || ISLANDQUEST_NEW_INPUT
            Key mapped=Map(key);
            return Keyboard.current!=null && mapped!=Key.None && Keyboard.current[mapped].isPressed;
#else
            return Input.GetKey(key);
#endif
        }
        public static bool Pressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM || ISLANDQUEST_NEW_INPUT
            Key mapped=Map(key);
            return Keyboard.current!=null && mapped!=Key.None && Keyboard.current[mapped].wasPressedThisFrame;
#else
            return Input.GetKeyDown(key);
#endif
        }
        public static bool MouseHeld(int button)
        {
#if ENABLE_INPUT_SYSTEM || ISLANDQUEST_NEW_INPUT
            if(Mouse.current==null) return false;
            return button==0 ? Mouse.current.leftButton.isPressed : Mouse.current.rightButton.isPressed;
#else
            return Input.GetMouseButton(button);
#endif
        }
        public static bool MousePressed(int button)
        {
#if ENABLE_INPUT_SYSTEM || ISLANDQUEST_NEW_INPUT
            if(Mouse.current==null) return false;
            return button==0 ? Mouse.current.leftButton.wasPressedThisFrame : Mouse.current.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(button);
#endif
        }
        public static Vector2 Look
        {
            get
            {
#if ENABLE_INPUT_SYSTEM || ISLANDQUEST_NEW_INPUT
                return Mouse.current!=null ? Mouse.current.delta.ReadValue()*.07f : Vector2.zero;
#else
                return new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"));
#endif
            }
        }
        public static float Scroll
        {
            get
            {
#if ENABLE_INPUT_SYSTEM || ISLANDQUEST_NEW_INPUT
                return Mouse.current!=null ? Mouse.current.scroll.ReadValue().y/1200f : 0;
#else
                return Input.GetAxis("Mouse ScrollWheel");
#endif
            }
        }
        public static Vector2 Movement
        {
            get
            {
                float x=(Held(KeyCode.D)||Held(KeyCode.RightArrow) ? 1 : 0)-(Held(KeyCode.A)||Held(KeyCode.LeftArrow) ? 1 : 0);
                float y=(Held(KeyCode.W)||Held(KeyCode.UpArrow) ? 1 : 0)-(Held(KeyCode.S)||Held(KeyCode.DownArrow) ? 1 : 0);
                return Vector2.ClampMagnitude(new Vector2(x,y),1);
            }
        }
#if ENABLE_INPUT_SYSTEM || ISLANDQUEST_NEW_INPUT
        static Key Map(KeyCode key)
        {
            switch(key)
            {
                case KeyCode.W: return Key.W; case KeyCode.A: return Key.A;
                case KeyCode.S: return Key.S; case KeyCode.D: return Key.D;
                case KeyCode.E: return Key.E; case KeyCode.R: return Key.R;
                case KeyCode.LeftShift: return Key.LeftShift; case KeyCode.Space: return Key.Space;
                case KeyCode.Escape: return Key.Escape;case KeyCode.F1: return Key.F1;case KeyCode.F8: return Key.F8;
                case KeyCode.Alpha0: return Key.Digit0;case KeyCode.Alpha1: return Key.Digit1;
                case KeyCode.Alpha2: return Key.Digit2;case KeyCode.Alpha3: return Key.Digit3;case KeyCode.Alpha4: return Key.Digit4;
                case KeyCode.LeftArrow: return Key.LeftArrow;case KeyCode.RightArrow: return Key.RightArrow;
                case KeyCode.UpArrow: return Key.UpArrow;case KeyCode.DownArrow: return Key.DownArrow;
                default: return Key.None;
            }
        }
#endif
    }
}

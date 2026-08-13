using UnityEngine;
using UnityEngine.EventSystems;

namespace PsyCurio.Shop.Interaction
{
    /// <summary>One frame's pointer reading.</summary>
    public readonly struct PointerSample
    {
        public static readonly PointerSample None = default;

        public bool HasPointer { get; }
        public Vector2 Position { get; }
        public bool ClickThisFrame { get; }

        public PointerSample(Vector2 position, bool clickThisFrame)
        {
            HasPointer = true;
            Position = position;
            ClickThisFrame = clickThisFrame;
        }
    }

    /// <summary>
    /// The one place that reads pointer hardware; a future input method (gaze,
    /// controller) replaces this class, not the router. Touch is read directly,
    /// never through mouse emulation: the emulated position lags a frame on
    /// Android and keeps "hovering" the last touch point forever. Without a
    /// finger down there is no pointer on mobile.
    /// </summary>
    public static class PointerSource
    {
        public static PointerSample Sample()
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                return new PointerSample(touch.position, touch.phase == TouchPhase.Began);
            }
            if (Application.isMobilePlatform)
            {
                return PointerSample.None;
            }
            return new PointerSample(Input.mousePosition, Input.GetMouseButtonDown(0));
        }

        /// <summary>Per-finger overload while a touch is active — the
        /// parameterless one can miss touches on Android.</summary>
        public static bool IsPointerOverUi()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }
            if (Input.touchCount > 0)
            {
                return eventSystem.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }
            return eventSystem.IsPointerOverGameObject();
        }
    }
}

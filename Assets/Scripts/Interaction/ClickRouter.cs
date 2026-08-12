using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PsyCurio.Shop.Interaction
{
    /// <summary>
    /// The single place that reads the mouse. Raycasts from the fixed camera
    /// every frame: hover enter/exit plus cursor swap for clickables, click
    /// dispatch via IClickable, and a DeadClicked event for clicks that hit
    /// nothing interactive (audible feedback attaches to it in the usability
    /// pass — no click may fall silent).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ClickRouter : MonoBehaviour
    {
        [Tooltip("Raycast reach; generous for a 4 m room.")]
        [SerializeField] private float maxRayDistance = 20f;

        public event Action DeadClicked;

        /// <summary>A click that reached an IClickable — used by the first-run
        /// hint to fade after the first real interaction.</summary>
        public event Action ClickDispatched;

        private Camera rayCamera;
        private IHoverable currentHover;

        private void Awake()
        {
            rayCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            if (IsPointerOverUi())
            {
                // The panel and prompts own the pointer; scene hover ends and
                // scene clicks must not leak through UI.
                SetHover(null);
                return;
            }

            // Touch is read directly, never through mouse emulation: the
            // emulated position lags a frame on Android (taps land where the
            // previous tap was) and keeps "hovering" the last touch point
            // forever, which froze highlights on. On touch, highlight acts as
            // press feedback while a finger is down; without a finger there
            // is no pointer, so there is no hover.
            Vector2 pointerPosition;
            bool clickThisFrame;
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                pointerPosition = touch.position;
                clickThisFrame = touch.phase == TouchPhase.Began;
            }
            else if (Application.isMobilePlatform)
            {
                SetHover(null);
                return;
            }
            else
            {
                pointerPosition = Input.mousePosition;
                clickThisFrame = Input.GetMouseButtonDown(0);
            }

            var ray = rayCamera.ScreenPointToRay(pointerPosition);
            IClickable clickable = null;
            IHoverable hoverable = null;
            if (Physics.Raycast(ray, out var hit, maxRayDistance))
            {
                clickable = hit.collider.GetComponentInParent<IClickable>();
                hoverable = hit.collider.GetComponentInParent<IHoverable>();
            }

            // Hover affordance strictly follows clickability: an object that
            // cannot be clicked never highlights and never changes the cursor.
            SetHover(clickable != null ? hoverable : null);
            InteractionCursor.ShowPointer(clickable != null);

            if (clickThisFrame)
            {
                if (clickable != null)
                {
                    clickable.OnClick();
                    ClickDispatched?.Invoke();
                }
                else
                {
                    DeadClicked?.Invoke();
                }
            }
        }

        private void OnDisable()
        {
            SetHover(null);
            InteractionCursor.ShowPointer(false);
        }

        private void SetHover(IHoverable next)
        {
            if (ReferenceEquals(currentHover, next))
            {
                return;
            }

            currentHover?.OnHoverExit();
            currentHover = next;
            currentHover?.OnHoverEnter();
        }

        private static bool IsPointerOverUi()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            // On Android the parameterless overload can miss touches; ask per
            // finger when a touch is active.
            if (Input.touchCount > 0)
            {
                return eventSystem.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }
            return eventSystem.IsPointerOverGameObject();
        }
    }
}

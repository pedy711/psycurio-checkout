using System;
using UnityEngine;

namespace PsyCurio.Shop.Interaction
{
    /// <summary>
    /// The single click dispatcher: raycasts the pointer from the fixed
    /// camera, drives hover and cursor affordances, dispatches clicks via
    /// IClickable, and raises DeadClicked for clicks that hit nothing — no
    /// click may fall silent. Pointer hardware is read by PointerSource.
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
        private Collider lastHitCollider;
        private IClickable lastClickable;
        private IHoverable lastHoverable;

        private void Awake()
        {
            rayCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            if (PointerSource.IsPointerOverUi())
            {
                // UI owns the pointer: scene hover ends, the cursor resets,
                // and scene clicks must not leak through.
                SetHover(null);
                InteractionCursor.ShowPointer(false);
                return;
            }

            var pointer = PointerSource.Sample();
            if (!pointer.HasPointer)
            {
                SetHover(null);
                return;
            }

            var ray = rayCamera.ScreenPointToRay(pointer.Position);
            IClickable clickable = null;
            IHoverable hoverable = null;
            if (Physics.Raycast(ray, out var hit, maxRayDistance))
            {
                // Cached per collider: the pointer rests on the same object
                // for hundreds of consecutive frames.
                if (!ReferenceEquals(hit.collider, lastHitCollider))
                {
                    lastHitCollider = hit.collider;
                    lastClickable = hit.collider.GetComponentInParent<IClickable>();
                    lastHoverable = hit.collider.GetComponentInParent<IHoverable>();
                }
                clickable = lastClickable;
                hoverable = lastHoverable;
            }
            else
            {
                lastHitCollider = null;
                lastClickable = null;
                lastHoverable = null;
            }

            // An object that cannot be clicked never highlights and never
            // changes the cursor.
            SetHover(clickable != null ? hoverable : null);
            InteractionCursor.ShowPointer(clickable != null);

            if (pointer.ClickThisFrame)
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
            // Interface-typed fields bypass UnityEngine.Object's overloaded
            // null: a hovered item destroyed by removal would otherwise
            // receive OnHoverExit after death.
            if (currentHover is UnityEngine.Object hoverObject && hoverObject == null)
            {
                currentHover = null;
            }

            if (ReferenceEquals(currentHover, next))
            {
                return;
            }

            currentHover?.OnHoverExit();
            currentHover = next;
            currentHover?.OnHoverEnter();
        }
    }
}

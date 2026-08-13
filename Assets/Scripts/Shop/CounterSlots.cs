using System.Collections;
using UnityEngine;

namespace PsyCurio.Shop
{
    /// <summary>
    /// Renders basket state onto the five named slot anchors under the counter.
    /// Anchor references are assigned by the scene wiring — no positions are
    /// hard-coded anywhere. Slot index i always mirrors basket index i.
    /// </summary>
    public sealed class CounterSlots : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color PulseColor = new Color(0.85f, 0.25f, 0.2f);

        [Tooltip("Slot_0..4 under Counter/SlotAnchors, assigned by scene wiring.")]
        [SerializeField] private Transform[] anchors = new Transform[0];
        [Tooltip("Particle burst played where an item lands, assigned by scene wiring.")]
        [SerializeField] private GameObject landingBurstPrefab;
        [SerializeField] private float pulseSeconds = 0.9f;

        private Coroutine activePulse;

        /// <summary>Raised when a flying item touches down on its slot.</summary>
        public event System.Action ItemLanded;

        public int Count => anchors.Length;

        public GameObject Place(int index, ShopItemDefinition definition, Vector3 fromWorldPosition)
        {
            var anchor = anchors[index];
            var item = Instantiate(definition.CounterPrefab, anchor);
            // Item prefabs are bottom-pivoted: local zero rests on the anchor.
            var restingPosition = Vector3.zero;
            item.name = $"CounterItem_{definition.name}";

            var flight = item.AddComponent<ItemFlight>();
            flight.Begin(fromWorldPosition, restingPosition, () =>
            {
                if (landingBurstPrefab != null)
                {
                    // The item's own position, not the spawn-time anchor: a
                    // mid-flight shift-down may have retargeted it to a
                    // different slot by the time it lands.
                    Instantiate(landingBurstPrefab, item.transform.position, Quaternion.identity);
                }
                ItemLanded?.Invoke();
            });
            return item;
        }

        /// <summary>Flashes all five markers red twice — the visible half of a
        /// refused placement.</summary>
        public void PulseMarkers()
        {
            if (activePulse != null)
            {
                StopCoroutine(activePulse);
            }
            activePulse = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            // Markers and their resting colors are loop-invariant — resolve
            // them once, not per anchor per frame.
            var block = new MaterialPropertyBlock();
            var markers = new (Renderer renderer, Color baseColor)[anchors.Length];
            for (var i = 0; i < anchors.Length; i++)
            {
                var renderer = anchors[i].GetChild(0).GetComponent<Renderer>();
                markers[i] = (renderer, renderer.sharedMaterial.GetColor(BaseColorId));
            }

            for (var t = 0f; t < pulseSeconds; t += Time.deltaTime)
            {
                // Two full sine pulses over the duration.
                var strength = Mathf.Abs(Mathf.Sin(t / pulseSeconds * Mathf.PI * 2f));
                foreach (var (renderer, baseColor) in markers)
                {
                    block.SetColor(BaseColorId, Color.Lerp(baseColor, PulseColor, strength));
                    renderer.SetPropertyBlock(block);
                }
                yield return null;
            }

            foreach (var (renderer, _) in markers)
            {
                renderer.SetPropertyBlock(null);
            }
            activePulse = null;
        }

        /// <summary>
        /// Re-syncs visuals after a mid-basket removal: everything after the
        /// removed slot shifts down one, mirroring Basket.RemoveAt semantics.
        /// </summary>
        public void ShiftDownFrom(int removedIndex)
        {
            for (var i = removedIndex; i < anchors.Length - 1; i++)
            {
                ClearAnchor(anchors[i]);
                var next = FirstChild(anchors[i + 1]);
                if (next != null)
                {
                    next.SetParent(anchors[i], false);
                }
            }
            ClearAnchor(anchors[anchors.Length - 1]);
        }

        public void Clear()
        {
            foreach (var anchor in anchors)
            {
                ClearAnchor(anchor);
            }
        }

        private static Transform FirstChild(Transform anchor)
        {
            return anchor.childCount > 1 ? anchor.GetChild(1) : null;
        }

        private static void ClearAnchor(Transform anchor)
        {
            // Child 0 is the always-present slot marker; spawned items follow.
            for (var i = anchor.childCount - 1; i >= 1; i--)
            {
                Destroy(anchor.GetChild(i).gameObject);
            }
        }
    }
}

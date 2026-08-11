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
        [SerializeField] private float pulseSeconds = 0.9f;

        private Coroutine activePulse;

        public int Count => anchors.Length;

        public GameObject Place(int index, ShopItemDefinition definition)
        {
            var anchor = anchors[index];
            var item = Instantiate(definition.CounterPrefab, anchor);
            // Primitive prefabs carry their size in localScale; rest the item
            // on the marker instead of intersecting it.
            item.transform.localPosition = new Vector3(0f, definition.CounterPrefab.transform.localScale.y / 2f, 0f);
            item.name = $"CounterItem_{definition.name}";
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
            var block = new MaterialPropertyBlock();
            for (var t = 0f; t < pulseSeconds; t += Time.deltaTime)
            {
                // Two full sine pulses over the duration.
                var strength = Mathf.Abs(Mathf.Sin(t / pulseSeconds * Mathf.PI * 2f));
                foreach (var anchor in anchors)
                {
                    var marker = anchor.GetChild(0).GetComponent<Renderer>();
                    block.SetColor(BaseColorId, Color.Lerp(
                        marker.sharedMaterial.GetColor(BaseColorId), PulseColor, strength));
                    marker.SetPropertyBlock(block);
                }
                yield return null;
            }

            foreach (var anchor in anchors)
            {
                anchor.GetChild(0).GetComponent<Renderer>().SetPropertyBlock(null);
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

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
        [Tooltip("Slot_0..4 under Counter/SlotAnchors, assigned by scene wiring.")]
        [SerializeField] private Transform[] anchors = new Transform[0];

        public int Count => anchors.Length;

        public void Place(int index, ShopItemDefinition definition)
        {
            var anchor = anchors[index];
            var item = Instantiate(definition.CounterPrefab, anchor);
            // Primitive prefabs carry their size in localScale; rest the item
            // on the marker instead of intersecting it.
            item.transform.localPosition = new Vector3(0f, definition.CounterPrefab.transform.localScale.y / 2f, 0f);
            item.name = $"CounterItem_{definition.name}";
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

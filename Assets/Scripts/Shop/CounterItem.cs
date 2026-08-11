using PsyCurio.Shop.Interaction;
using UnityEngine;

namespace PsyCurio.Shop
{
    /// <summary>
    /// A copy on the counter. Clicking it removes it — the undo affordance:
    /// pick something by mistake, click it away. Added at spawn time by the
    /// controller, which also resolves this view back to its basket index.
    /// </summary>
    public sealed class CounterItem : MonoBehaviour, IClickable
    {
        private ShopController controller;

        public void Init(ShopController owner)
        {
            controller = owner;
        }

        public void OnClick()
        {
            controller.Remove(this);
        }
    }
}

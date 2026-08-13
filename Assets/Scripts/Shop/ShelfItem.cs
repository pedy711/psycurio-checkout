using PsyCurio.Shop.Interaction;
using UnityEngine;

namespace PsyCurio.Shop
{
    /// <summary>
    /// Makes a shelf display clickable. The display itself never moves — a
    /// click asks the controller to place a copy on the counter. Thin by
    /// design: no rules live here.
    /// </summary>
    public sealed class ShelfItem : MonoBehaviour, IClickable
    {
        [SerializeField] private ShopItemDefinition definition;
        [SerializeField] private ShopController controller;

        public void OnClick()
        {
            controller.TryPlace(definition, transform.position);
        }
    }
}

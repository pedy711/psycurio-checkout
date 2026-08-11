using PsyCurio.Shop.Domain;
using PsyCurio.Shop.Interaction;
using UnityEngine;

namespace PsyCurio.Shop
{
    /// <summary>
    /// Clicking the register asks the cashier to state what was chosen and the
    /// total. The sentence comes from the domain narrator; this class only
    /// forwards it.
    /// </summary>
    public sealed class CashRegister : MonoBehaviour, IClickable
    {
        [SerializeField] private ShopController controller;
        [SerializeField] private Cashier cashier;

        private readonly PurchaseNarrator narrator = new PurchaseNarrator();

        public void OnClick()
        {
            cashier.Say(narrator.Narrate(controller.Basket));
        }
    }
}

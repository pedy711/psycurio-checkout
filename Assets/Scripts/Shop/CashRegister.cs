using PsyCurio.Shop.Interaction;
using UnityEngine;

namespace PsyCurio.Shop
{
    /// <summary>
    /// Clicking the register asks the cashier to state what was chosen and the
    /// total. The narration itself lives on ShopController — the single bridge
    /// to the domain — so this class only forwards the click.
    /// </summary>
    public sealed class CashRegister : MonoBehaviour, IClickable
    {
        [SerializeField] private ShopController controller;

        public void OnClick()
        {
            controller.NarratePurchase();
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace PsyCurio.Shop.Ui
{
    /// <summary>Clears the counter and basket. Wired to the Button at runtime
    /// so the listener never goes stale in the serialized scene.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class ResetButton : MonoBehaviour
    {
        [SerializeField] private ShopController controller;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(controller.ResetShop);
        }
    }
}

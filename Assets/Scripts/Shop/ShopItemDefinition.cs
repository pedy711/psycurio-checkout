using PsyCurio.Shop.Domain;
using UnityEngine;

/// <summary>
/// Inspector-editable definition of a buyable item. Six of these live in
/// Assets/Items; a non-programmer adds items via Create > PsyCurio > Shop Item
/// and reprices them here without touching code. The asset's file name doubles
/// as the stable item id.
/// </summary>
[CreateAssetMenu(menuName = "PsyCurio/Shop Item", fileName = "NewShopItem")]
public sealed class ShopItemDefinition : ScriptableObject
{
    [Tooltip("Name the cashier speaks, e.g. \"Coffee\".")]
    [SerializeField] private string displayName = "";

    [Tooltip("Price in euros, e.g. 2.49.")]
    [Min(0f)]
    [SerializeField] private float priceEuros;

    [Tooltip("Prefab shown on the shelf and placed on the counter when bought.")]
    [SerializeField] private GameObject counterPrefab;

    public string DisplayName => displayName;

    public GameObject CounterPrefab => counterPrefab;

    /// <summary>Rounded once here so float euros can never leak into the domain.</summary>
    public int PriceCents => Mathf.RoundToInt(priceEuros * 100f);

    public ShopItem ToDomainItem()
    {
        return new ShopItem(name, displayName, PriceCents);
    }
}

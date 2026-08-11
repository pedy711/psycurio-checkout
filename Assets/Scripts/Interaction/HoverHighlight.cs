using System.Collections.Generic;
using UnityEngine;

namespace PsyCurio.Shop.Interaction
{
    /// <summary>
    /// Brightens all child renderers while hovered, via MaterialPropertyBlock
    /// so shared materials are never instantiated. Lives on every clickable
    /// prefab; ClickRouter drives it only when the object is truly clickable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HoverHighlight : MonoBehaviour, IHoverable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("0 = no change, 1 = fully white while hovered.")]
        [Range(0f, 1f)]
        [SerializeField] private float brighten = 0.3f;

        private readonly List<(Renderer renderer, Color baseColor)> targets =
            new List<(Renderer, Color)>();
        private MaterialPropertyBlock block;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            foreach (var childRenderer in GetComponentsInChildren<Renderer>())
            {
                targets.Add((childRenderer, childRenderer.sharedMaterial.GetColor(BaseColorId)));
            }
        }

        public void OnHoverEnter()
        {
            ApplyTint(brightened: true);
        }

        public void OnHoverExit()
        {
            ApplyTint(brightened: false);
        }

        private void ApplyTint(bool brightened)
        {
            foreach (var (childRenderer, baseColor) in targets)
            {
                if (childRenderer == null)
                {
                    continue;
                }

                if (brightened)
                {
                    block.SetColor(BaseColorId, Color.Lerp(baseColor, Color.white, brighten));
                    childRenderer.SetPropertyBlock(block);
                }
                else
                {
                    childRenderer.SetPropertyBlock(null);
                }
            }
        }
    }
}

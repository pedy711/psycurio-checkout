using System.Collections.Generic;
using UnityEngine;

namespace PsyCurio.Shop.Interaction
{
    /// <summary>
    /// Tints all child renderers toward a warm highlight while hovered, via
    /// MaterialPropertyBlock so shared materials are never instantiated. The
    /// tint must differ from white: textured materials keep a white base
    /// color, so a lerp toward white would be invisible on them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HoverHighlight : MonoBehaviour, IHoverable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color HighlightTint = new Color(1f, 0.82f, 0.4f);

        [Tooltip("0 = no change, 1 = fully the highlight tint while hovered.")]
        [Range(0f, 1f)]
        [SerializeField] private float brighten = 0.45f;

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
                    block.SetColor(BaseColorId, Color.Lerp(baseColor, HighlightTint, brighten));
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

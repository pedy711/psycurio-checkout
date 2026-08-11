namespace PsyCurio.Shop.Interaction
{
    /// <summary>
    /// Visual hover response. ClickRouter only raises these on objects that are
    /// also clickable, so nothing inert can look interactive.
    /// </summary>
    public interface IHoverable
    {
        void OnHoverEnter();

        void OnHoverExit();
    }
}

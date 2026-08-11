namespace PsyCurio.Shop.Interaction
{
    /// <summary>
    /// Something the patient can click. The only dispatcher is ClickRouter —
    /// no object reads the mouse itself, so interaction behaviour stays in one
    /// reviewable place.
    /// </summary>
    public interface IClickable
    {
        void OnClick();
    }
}

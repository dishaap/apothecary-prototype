namespace Game.UI
{
    /// <summary>
    /// Contract implemented by every UI Toolkit screen. Visibility is owned
    /// by a higher-level UI manager which calls these methods; screens never
    /// show or hide themselves in response to their own input.
    /// </summary>
    public interface IScreen
    {
        /// <summary>Makes the screen visible.</summary>
        void Show();

        /// <summary>Hides the screen.</summary>
        void Hide();

        /// <summary>Toggles the screen between shown and hidden.</summary>
        void Toggle();
    }
}

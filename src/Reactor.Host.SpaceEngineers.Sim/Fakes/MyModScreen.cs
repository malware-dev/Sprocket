// The surface is the game's, so all of it is declared whether the mod reads it or not: a screen
// host with half the properties would compile here and not in the mod.
// ReSharper disable UnusedAutoPropertyAccessor.Global

using VRageMath;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// The game's screen host, as the harness stands in for it.
    /// </summary>
    /// <remarks>
    /// The game's own is in Sandbox.Game, which the harness does not reference - see
    /// build/Reactor.GameReferences.props. The surface is the game's, so the mod's screen compiles
    /// against either; what happens when it opens is the harness's, which is nothing but saying so
    /// and telling the screen.
    /// </remarks>
    public abstract class MyModScreen
    {
        protected MyModScreen(string name, Vector2? position = null, Vector2? size = null,
            Color? backgroundColor = null, string backgroundTexture = null, bool isTopMost = false)
        {
            Name = name;
            Position = position ?? new Vector2(0.5f, 0.5f);
            Size = size;
            BackgroundColor = backgroundColor;
            BackgroundTexture = backgroundTexture;
            IsTopMost = isTopMost;
        }

        public string Name { get; private set; }

        public bool IsOpen { get; private set; }

        public bool HasFocus { get; private set; }

        public bool IsVisible { get; private set; }

        public Vector2 Position { get; set; }

        public Vector2? Size { get; set; }

        public Color? BackgroundColor { get; set; }

        public string BackgroundTexture { get; private set; }

        public bool IsTopMost { get; private set; }

        public bool CloseOnEscape { get; set; }

        public bool CloseButtonEnabled { get; set; }

        public bool DrawMouseCursor { get; set; }

        public bool EnabledBackgroundFade { get; set; }

        public bool SkipTransition { get; set; }

        public bool PausesGameInSinglePlayer { get; set; }

        public void Open()
        {
            IsOpen = true;
            IsVisible = true;
            HasFocus = true;
            OnOpened();
        }

        public void Close()
        {
            if (OnCancelling())
                CloseNow();
        }

        public void CloseNow()
        {
            IsOpen = false;
            IsVisible = false;
            HasFocus = false;
            OnClosed(false);
        }

        public void Hide()
        {
            IsVisible = false;
        }

        public void Unhide()
        {
            IsVisible = true;
        }

        protected virtual void OnOpened()
        {
        }

        protected virtual void OnLayoutChanged()
        {
        }

        protected virtual void OnUpdate(bool hasFocus)
        {
        }

        protected virtual void OnHandleInput()
        {
        }

        protected virtual bool OnCancelling()
        {
            return true;
        }

        protected virtual void OnDraw(float transitionAlpha)
        {
        }

        protected virtual void OnClosed(bool isUnloading)
        {
        }
    }
}

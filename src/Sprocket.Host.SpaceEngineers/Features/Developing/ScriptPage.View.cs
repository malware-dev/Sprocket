using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Developing
{
    sealed partial class ScriptPage
    {
        EditBox _listing;

        /// <summary>The script on a listing that fills the page and moves both ways.</summary>
        /// <remarks>
        /// Read rather than worked in, so deliberately plain: the program's own text on the page's own ground, in a face whose
        /// characters are all one width, because the indent of a step under its sequence only lines up that way. Numbered,
        /// because a listing is talked about by line number: a parser says which line it could not read.
        /// </remarks>
        protected override Element CreateView()
        {
            return new EditBox
            {
                IsReadOnly = true,
                Font = Lettering.Mono,
                FontSize = 20f,
                Color = Palette.Ink,
                Numbered = true,
                NumberColor = Palette.Dim,
                Padding = new Thickness(20f, 8f, 8f, 8f),
                BarWidth = Sizes.Bar,
                Thumb = Palette.Thumb,
                HeldThumb = Palette.ThumbHeld,
                LeastThumb = 32f
            }.As(out _listing);
        }
    }
}

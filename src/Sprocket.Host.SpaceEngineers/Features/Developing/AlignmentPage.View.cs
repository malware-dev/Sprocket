using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Images;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Developing
{
    sealed partial class AlignmentPage
    {
        static readonly Color Guide = new Color(255, 64, 64);

        static readonly string[] Marks =
        {
            Symbols.Left, Symbols.Play, Symbols.Delete, Symbols.Ring, Symbols.Disc, Symbols.Plus, Symbols.Minus,
            Symbols.Pencil, Symbols.Up, Symbols.Down
        };

        /// <summary>Rows of symbols at the sizes the screens use them, then lines of text in each face.</summary>
        protected override Element CreateView()
        {
            var rows = new VStack { Spacing = 16f, Padding = new Thickness(20f, 16f) };

            rows.Children.Add(SymbolRow(20f, 32f));
            rows.Children.Add(SymbolRow(32f, 48f));
            rows.Children.Add(SymbolRow(64f, 96f));

            rows.Children.Add(WordRow(MyFontEnum.White, 20f, 40f));
            rows.Children.Add(WordRow(MyFontEnum.White, 32f, 56f));
            rows.Children.Add(WordRow(Lettering.Mono, 20f, 40f));
            rows.Children.Add(WordRow("Monospace", 20f, 40f));

            return rows;
        }

        /// <summary>Every mark in <see cref="Marks" />, <paramref name="size" /> tall, each in a square <paramref name="box" /> across.</summary>
        static Element SymbolRow(float size, float box)
        {
            var row = new HStack { Spacing = 8f };
            for (var i = 0; i < Marks.Length; i++)
            {
                row.Children.Add(Boxed(new Glyph
                {
                    Symbol = Marks[i],
                    Font = Symbols.Font,
                    FontSize = size,
                    Tint = Palette.Ink
                }, box, box));
            }

            return row;
        }

        /// <summary>A few runs in <paramref name="font" />, <paramref name="size" /> tall, each in a box <paramref name="box" /> high.</summary>
        static Element WordRow(string font, float size, float box)
        {
            return new HStack
            {
                Spacing = 8f,
                Children =
                {
                    Boxed(Run(font, size, "H"), box, box),
                    Boxed(Run(font, size, "x"), box, box),
                    Boxed(Run(font, size, "Hxp"), box * 2f, box),
                    Boxed(Run(font, size, "Sequence 1"), box * 5f, box),
                    Boxed(Run(font, size, "stockpile"), box * 5f, box)
                }
            };
        }

        static Label Run(string font, float size, string text)
        {
            return new Label { Text = text, Font = font, FontSize = size, Color = Palette.Ink };
        }

        static Plumb Boxed(Element held, float wide, float high)
        {
            return new Plumb
            {
                Content = held,
                Background = Palette.Panel,
                Guide = Guide,
                Room = Palette.Dim,
                Placement = Fixed.Size(wide, high)
            };
        }
    }
}

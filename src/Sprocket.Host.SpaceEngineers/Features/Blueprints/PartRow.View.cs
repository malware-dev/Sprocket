using System;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Writing;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    partial class PartRow
    {
        VStack _column;
        Label _name;
        TextBox _nameBox;
        Label _refusal;
        Label _optional;
        RichText _about;
        Composer _aboutComposer;
        Slot _blocks;
        Label _problem;
        Switch _required;

        /// <summary>
        /// Makes the part's name and what it says about itself, with the blocks beside them; and the boxes they are
        /// written in, with the switch that makes the slot optional, for a blueprint being edited.
        /// </summary>
        void CreateView(Func<WindowLayer> windows)
        {
            Spacing = 20f;
            Padding = new Thickness(20f, 8f);

            Children.Add(new VStack
            {
                Spacing = 4f,
                Placement = Fixed.Wide(PartList.Widest),
                Children =
                {
                    new HStack
                    {
                        Spacing = 8f,
                        Children =
                        {
                            new Label { Style = Styles.Name }.As(out _name),
                            // Hidden, as the switch below is.
                            new Label { Style = Styles.Detail, IsVisible = false }.As(out _optional)
                        }
                    },
                    new TextBox { Style = Styles.Field, IsVisible = false }.As(out _nameBox),
                    new Label { Style = Styles.Problem, Wraps = true, IsVisible = false }.As(out _refusal),
                    new RichText { Style = Styles.SlotDescription, IsVisible = false }.As(out _about),
                    new Composer(72f, Styles.SlotDescription, windows) { IsVisible = false }.As(out _aboutComposer)
                }
            }.As(out _column));

            Children.Add(new Slot().As(out _blocks));
            Children.Add(new Label { Style = Styles.Problem, IsVisible = false }.As(out _problem));
            // Hidden: an empty optional slot can still stop its installation.
            Children.Add(new Switch { Style = Styles.Switch, IsVisible = false }.As(out _required));
        }
    }
}

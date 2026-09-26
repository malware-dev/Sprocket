using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    partial class BlueprintGroup
    {
        RichText _heading;
        VStack _list;

        /// <summary>Makes the group's heading and the place its rows go.</summary>
        void CreateView()
        {
            Children.Add(new VStack
            {
                Padding = new Thickness(4f, 32f, 0f, 8f),
                Children = { new RichText { Style = Styles.Section }.As(out _heading) }
            });
            Children.Add(new VStack { Spacing = 8f }.As(out _list));
        }
    }
}

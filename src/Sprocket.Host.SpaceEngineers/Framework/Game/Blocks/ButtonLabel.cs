using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>The button a member was given, counted from 0 as the game counts it, and the label where one was given.</summary>
    public sealed class ButtonLabel
    {
        /// <summary>The button, counted from 0, or -1 where none was given.</summary>
        public int Button = -1;

        /// <summary>The label, or empty to give the button back the game's own.</summary>
        public string Text = "";

        /// <summary>What a member taking a button, and perhaps a label, was given.</summary>
        public static ButtonLabel Read(IList<Literal> arguments)
        {
            var labelled = new ButtonLabel();
            if (arguments == null || arguments.Count == 0 || arguments[0].Kind != ValueKind.Integer
                || arguments[0].Integer < 1 || arguments[0].Integer > int.MaxValue)
                return labelled;

            labelled.Button = (int)arguments[0].Integer - 1;
            if (arguments.Count > 1)
                labelled.Text = arguments[1].Text ?? "";

            return labelled;
        }
    }
}

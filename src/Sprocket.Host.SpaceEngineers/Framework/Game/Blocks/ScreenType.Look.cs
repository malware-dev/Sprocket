using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class ScreenType
    {
        /// <summary>The screen a step sets up, and each setting it was set to.</summary>
        sealed class Look
        {
            /// <summary>The screen.</summary>
            public Picked Which;

            /// <summary>Where what it shows is among <see cref="Contents" />, or -1 to leave it.</summary>
            public int Content = -1;

            /// <summary>The app it runs, by the game's id for it, empty for none; null to leave it, as it is unless it is set to show an app.</summary>
            public string Runs;

            /// <summary>The color of its text, or null to leave it.</summary>
            public Color? TextColor;

            /// <summary>The color behind its text, or null to leave it.</summary>
            public Color? BackgroundColor;

            /// <summary>The font its text is in, or null to leave it.</summary>
            public string Font;

            /// <summary>How large its text is, or null to leave it.</summary>
            public float? FontSize;

            /// <summary>Where its alignment is among <see cref="Alignments" />, or -1 to leave it.</summary>
            public int Alignment = -1;

            /// <summary>The space around its text, in percent, or null to leave it.</summary>
            public float? Padding;

            /// <summary>The color an app draws in, or null to leave it.</summary>
            public Color? AppColor;

            /// <summary>The color behind an app, or null to leave it.</summary>
            public Color? AppBackgroundColor;

            /// <summary>How long each image shows, in seconds, or null to leave it.</summary>
            public float? Interval;

            /// <summary>Whether an image keeps its shape, or null to leave it.</summary>
            public bool? KeepsAspectRatio;

            /// <summary>The values of a step setting up a screen, after its screen, in the order <see cref="Asked" /> asks for it.</summary>
            public static Look Read(IList<Literal> arguments)
            {
                var look = new Look { Which = Picked.Read(arguments) };
                if (arguments == null)
                    return look;

                look.Content = Chosen(arguments, 1, Contents);
                look.Runs = look.Content == AppContent && arguments.Count > 2 ? arguments[2].Text ?? "" : null;
                look.TextColor = Colored(arguments, 3);
                look.BackgroundColor = Colored(arguments, 4);
                look.Font = Given(arguments, 5) ? arguments[5].Text : null;
                look.FontSize = Number(arguments, 6);
                look.Alignment = Chosen(arguments, 7, Alignments);
                look.Padding = Number(arguments, 8);
                look.AppColor = Colored(arguments, 9);
                look.AppBackgroundColor = Colored(arguments, 10);
                look.Interval = Number(arguments, 11);
                look.KeepsAspectRatio = Given(arguments, 12) && arguments[12].Kind == ValueKind.Flag
                    ? arguments[12].Flag
                    : (bool?)null;
                return look;
            }

            /// <summary>Whether the setting at <paramref name="at" /> holds a value, which an empty one does not.</summary>
            static bool Given(IList<Literal> arguments, int at)
            {
                return arguments != null && at < arguments.Count
                    && !(arguments[at].Kind == ValueKind.Text && string.IsNullOrEmpty(arguments[at].Text));
            }

            static int Chosen(IList<Literal> arguments, int at, string[] words)
            {
                return Given(arguments, at) ? Array.IndexOf(words, arguments[at].Text) : -1;
            }

            static Color? Colored(IList<Literal> arguments, int at)
            {
                if (!Given(arguments, at) || arguments[at].Kind != ValueKind.Color)
                    return null;

                var given = arguments[at];
                return new Color(given.Red, given.Green, given.Blue, given.Alpha);
            }

            static float? Number(IList<Literal> arguments, int at)
            {
                if (!Given(arguments, at) || (arguments[at].Kind != ValueKind.Real && arguments[at].Kind != ValueKind.Integer))
                    return null;

                return (float)arguments[at].Real;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An emotion controller, which shows a face on the screens chosen in its terminal.</summary>
    public sealed class EmotionController : Capability
    {
        /// <summary>Showing a chosen face, which is Sprocket's own and runs the game's action for that face.</summary>
        public const string ShowFace = "show_face";

        // The folder every face's texture is in, which tells a face's action from any other: the game's emotion controller
        // component makes one action for each such texture.
        const string Faces = @"\emotes\";

        static readonly OwnAction Shows = OwnAction.Of<IMyEmotionControllerBlock, Chosen>(Chosen.Read, Showed);

        readonly List<Offer> _offered = new List<Offer>();

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyEmotionControllerBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            var words = new List<string>();
            var means = new List<string>();

            _offered.Clear();
            Toolbars.Offered(block, _offered);
            foreach (var offer in _offered)
            {
                var id = offer.Id;
                if (id == null || id.IndexOf(Faces, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                words.Add(string.IsNullOrEmpty(offer.Text) ? id : offer.Text);
                means.Add(id);
                kind.Hiding(id);
            }

            var face = Field.Among("face", words, means);
            face.Entry = FieldInput.Text;
            face.Needed = true;

            kind.WithAction(ShowFace, "show face", Shows, face).In(Category.Screen);
        }

        /// <summary>Runs the game's own action for the face a step chose.</summary>
        static void Showed(IMyEmotionControllerBlock controller, Chosen chosen, ReaderMemory seen)
        {
            Toolbars.Perform(controller, chosen.Face, null);
        }

        /// <summary>The face a step chose.</summary>
        sealed class Chosen
        {
            /// <summary>The id of the game's action for the face.</summary>
            public string Face = "";

            /// <summary>The face a step showing one was given.</summary>
            public static Chosen Read(IList<Literal> arguments)
            {
                return new Chosen { Face = arguments != null && arguments.Count > 0 ? arguments[0].Text ?? "" : "" };
            }
        }
    }
}

using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System.Collections.Generic;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

// The chat component lives in the Ingame namespace only.
using IMyChatBroadcastControllerComponent = Sandbox.ModAPI.Ingame.IMyChatBroadcastControllerComponent;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A broadcast controller, which posts a step's own words or one of its saved messages to the game chat.</summary>
    public sealed class BroadcastController : Capability
    {
        /// <summary>Posting a step's own words to chat, which is Sprocket's own.</summary>
        public const string Say = "say";

        /// <summary>Posting one of the messages saved in the controller's terminal, which is Sprocket's own.</summary>
        public const string SendSaved = "transmit_message";

        // The game's controller saves eight messages, numbered from 1.
        const int Saved = 8;

        static readonly OwnAction Says = OwnAction.Of<IMyBroadcastController, Words>(Words.Read,
            (controller, words, seen) => Chat(controller).SendMessage(words.Text));

        static readonly OwnAction Sends = OwnAction.Of<IMyBroadcastController, Numbered>(Numbered.Read,
            (controller, numbered, seen) => Chat(controller).SendMessage(numbered.Index));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            IMyChatBroadcastControllerComponent chat;
            return block is IMyBroadcastController && block.Components.TryGet(out chat);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(Say, "say", Says, AskedText()).In(Category.Signals)
                .WithAction(SendSaved, "transmit message", Sends, AskedMessage()).In(Category.Signals)
                .Renaming("TransmitRandomMessage", false, "send random message")
                .Renaming("SendGps", false, "send GPS")
                .Renaming("BroadcastTarget", true, "broadcast target")
                .Renaming("UseAntenna", true, "use antenna")
                .Renaming("UseAntenna", false, "toggle use antenna")
                .Renaming("CustomName", true, "sender name")
                .Hiding("BroadcastTargetEveryone", "BroadcastTargetOwner", "BroadcastTargetFaction");

            for (var i = 1; i <= Saved; i++)
                kind.Hiding($"Transmit Message {i}");
        }

        /// <summary>The controller's chat component, which every block this claims has.</summary>
        static IMyChatBroadcastControllerComponent Chat(IMyBroadcastController controller)
        {
            IMyChatBroadcastControllerComponent chat;
            controller.Components.TryGet(out chat);
            return chat;
        }

        /// <summary>What to say.</summary>
        static Field AskedText()
        {
            var text = Field.Called("text");
            text.Entry = FieldInput.Text;
            text.Needed = true;
            return text;
        }

        /// <summary>Which saved message to send, numbered as the terminal numbers them.</summary>
        static Field AskedMessage()
        {
            var message = Field.Between("message", "1", Saved.ToString());
            message.Entry = FieldInput.Integer;
            message.Needed = true;
            return message;
        }

        /// <summary>The words a step says.</summary>
        sealed class Words
        {
            /// <summary>What is said.</summary>
            public string Text = "";

            /// <summary>The words a step saying something was given.</summary>
            public static Words Read(IList<Literal> arguments)
            {
                return new Words { Text = arguments != null && arguments.Count > 0 ? arguments[0].Text ?? "" : "" };
            }
        }

        /// <summary>The saved message a step sends.</summary>
        sealed class Numbered
        {
            /// <summary>The message's index, counted from 0 where the terminal counts from 1.</summary>
            public int Index;

            /// <summary>The message a step sending a saved one was given.</summary>
            public static Numbered Read(IList<Literal> arguments)
            {
                return new Numbered { Index = arguments != null && arguments.Count > 0 ? (int)arguments[0].Integer - 1 : -1 };
            }
        }
    }
}

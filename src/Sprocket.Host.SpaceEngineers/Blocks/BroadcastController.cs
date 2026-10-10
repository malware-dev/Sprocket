using System.Collections.Generic;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;

// The chat component lives in the Ingame namespace only.
using IMyChatBroadcastControllerComponent = Sandbox.ModAPI.Ingame.IMyChatBroadcastControllerComponent;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A broadcast controller, which posts to the game chat from the construct: in a step's own words, one of its saved
    /// messages, and the members of its terminal said in Sprocket's words.
    /// </summary>
    /// <remarks>
    /// Everything it posts goes through its chat component, which sends only while the block works and spaces messages
    /// out: one sent too soon waits, and the same text sent again while it waits goes once, with a count.
    /// </remarks>
    public sealed class BroadcastController : Capability
    {
        /// <summary>Posting a step's own words to chat, which is Sprocket's own.</summary>
        public const string Say = "say";

        /// <summary>Posting one of the messages saved in the controller's terminal, which is Sprocket's own.</summary>
        public const string SendSaved = "send-a-saved-message";

        /// <summary>How many messages the controller's terminal saves, numbered from 1.</summary>
        const int Saved = 8;

        static readonly Doing Says = Doing.Of<IMyBroadcastController, Words>(Words.Read,
            (controller, words, seen) => Chat(controller).SendMessage(words.Text));

        static readonly Doing Sends = Doing.Of<IMyBroadcastController, Numbered>(Numbered.Read,
            (controller, numbered, seen) => Chat(controller).SendMessage(numbered.Index));

        /// <inheritdoc />
        /// <remarks>Only one with the chat component, which is what every one of the game's has and what it posts through.</remarks>
        public override bool Claims(IMyTerminalBlock block)
        {
            IMyChatBroadcastControllerComponent chat;
            return block is IMyBroadcastController && block.Components.TryGet(out chat);
        }

        /// <inheritdoc />
        /// <remarks>
        /// The terminal's eight transmit actions are one action with the message's number, and its three buttons that pick
        /// who hears it are the value they set.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(Say, "say", Says, AskedText()).In(Category.Signals)
                .WithAction(SendSaved, "send a saved message", Sends, AskedMessage()).In(Category.Signals)
                .Renaming("TransmitRandomMessage", false, "send a random saved message")
                .Renaming("SendGps", false, "send its position")
                .Renaming("BroadcastTarget", true, "who hears it")
                .Renaming("UseAntenna", true, "using antennas")
                .Renaming("UseAntenna", false, "toggle using antennas")
                .Renaming("CustomName", true, "sender name")
                .Hiding("BroadcastTargetEveryone", "BroadcastTargetOwner", "BroadcastTargetFaction");

            for (var i = 1; i <= Saved; i++)
                kind.Hiding("Transmit Message " + i);
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
            text.Entry = Entry.Text;
            text.Needed = true;
            return text;
        }

        /// <summary>Which saved message to send, numbered as the terminal numbers them.</summary>
        static Field AskedMessage()
        {
            var message = Field.Between("message", "1", Saved.ToString());
            message.Entry = Entry.Whole;
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
            /// <summary>Where the message is among the component's, which counts from 0 where the terminal counts from 1.</summary>
            public int Index;

            /// <summary>The message a step sending a saved one was given.</summary>
            public static Numbered Read(IList<Literal> arguments)
            {
                return new Numbered { Index = arguments != null && arguments.Count > 0 ? (int)arguments[0].Whole - 1 : -1 };
            }
        }
    }
}

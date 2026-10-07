using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Values;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// A sound block, which plays the sound a step chooses from the game's own list.
    /// </summary>
    /// <remarks>
    /// Playing is not waited for. A dedicated server plays no sound at all and nothing tells a mod when
    /// one has finished, so there is nothing to wait on.
    /// </remarks>
    public sealed class SoundExtension : IBlockExtension
    {
        /// <summary>Playing a chosen sound, which is Sprocket's own.</summary>
        public const string Play = "play";

        /// <inheritdoc />
        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMySoundBlock;
        }

        /// <summary>Nothing to arrange: this block's events fire when they fire.</summary>
        public bool Raises(IMyTerminalBlock block, string eventId, string key)
        {
            return true;
        }

        /// <inheritdoc />
        /// <remarks>
        /// The game's own play is kept out of the list: it plays whichever sound was left selected,
        /// which is the one thing a step choosing its sound exists to avoid.
        /// </remarks>
        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(Play, "play", null, Sound(block)).In(Category.Sound)
                .Hiding("PlaySound");
        }

        /// <summary>
        /// The sound to play, from the list the terminal shows and by the names it shows them by.
        /// </summary>
        /// <remarks>
        /// Every sound block has the same list, which is every public sound category the game has
        /// loaded. Where none are loaded - the test harness - the block's own list of ids, each
        /// named by itself.
        /// </remarks>
        static Field Sound(IMyTerminalBlock block)
        {
            var words = new List<string>();
            var means = new List<string>();

            var definitions = MyDefinitionManager.Static;
            if (definitions != null)
            {
                foreach (var category in definitions.GetSoundCategoryDefinitions())
                {
                    if (!category.Public)
                        continue;

                    for (var i = 0; i < category.Sounds.Count; i++)
                    {
                        words.Add(category.Sounds[i].SoundText);
                        means.Add(category.Sounds[i].SoundId);
                    }
                }
            }

            if (means.Count == 0)
            {
                var sounds = block as IMySoundBlock;
                if (sounds != null)
                    sounds.GetSounds(means);

                words.AddRange(means);
            }

            var sound = Field.Among("sound", words, means);
            sound.Entry = Entry.Text;
            sound.Needed = true;
            return sound;
        }

        /// <inheritdoc />
        public bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value)
        {
            value = default(EntityValue);
            return false;
        }

        /// <inheritdoc />
        public bool TryInvoke(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            var sounds = block as IMySoundBlock;
            if (sounds == null || actionId != Play)
                return false;

            if (values != null && values.Count > 0 && values[0].Text.Length > 0)
                sounds.SelectedSound = values[0].Text;

            sounds.Play();
            return true;
        }

        /// <summary>Nothing to add to the floor: whether a sound is playing is known only where it is heard.</summary>
        public Status StatusOf(IMyTerminalBlock block)
        {
            return Status.None;
        }

        /// <inheritdoc />
        public bool Done(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            return false;
        }

        /// <inheritdoc />
        public Trouble TroubleWith(IMyTerminalBlock block, string actionId, IList<Literal> values, string stateId)
        {
            return Trouble.None;
        }

        /// <inheritdoc />
        public bool IsBusy(IMyTerminalBlock block)
        {
            return false;
        }

        /// <summary>Nothing: a sound is never waited on, so no run ends with one in flight.</summary>
        public void Stop(IMyTerminalBlock block, string actionId)
        {
        }

        /// <inheritdoc />
        public int Expected(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            return GridEntity.Unestimated;
        }
    }
}

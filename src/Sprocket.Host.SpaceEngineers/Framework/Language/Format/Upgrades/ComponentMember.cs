using Sprocket.Host.SpaceEngineers.Framework.Game.Members;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>One member a block's component gives it, by the game's id and by Sprocket's own, for <see cref="ComponentMembers" />.</summary>
    public sealed class ComponentMember
    {
        /// <summary>The member the game calls <paramref name="game" />, reached as <paramref name="form" />, on the blocks whose subtype holds <paramref name="subtype" />, which Sprocket calls <paramref name="own" />.</summary>
        public ComponentMember(string subtype, string game, ReachKind form, string own)
        {
            Subtype = subtype;
            Game = game;
            Form = form;
            Own = own;
        }

        /// <summary>A word of the subtype of the blocks this is for.</summary>
        public string Subtype { get; private set; }

        /// <summary>The game's id, as earlier programs persist it.</summary>
        public string Game { get; private set; }

        /// <summary>Whether the game's is an action or a value.</summary>
        public ReachKind Form { get; private set; }

        /// <summary>Sprocket's own id.</summary>
        public string Own { get; private set; }
    }
}

using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>
    /// Version 2 alpha 1: the functions that return a thing of a host type are named apart from the type, so a kind is told
    /// from a call by its name: <c>screen</c> is <c>get_screen</c> and <c>inventory</c> is <c>get_inventory</c>. A program
    /// written before it says <c>sprocket.screen(1) on [ board ]</c>, read as <c>sprocket.get_screen(1) on [ board ]</c>.
    /// </summary>
    public sealed class GetFunctions : FormatChange
    {
        // The functions' names as programs written before version 2 alpha 1 persist them, each beside its name now.
        static readonly string[] Before = { "screen", "inventory" };
        static readonly string[] Now = { "get_screen", "get_inventory" };

        /// <inheritdoc />
        public override int Arrived => 1;

        /// <inheritdoc />
        public override void Statements(List<Statement> constructs)
        {
            foreach (var construct in constructs)
                Respelled(construct);
        }

        static void Respelled(Statement statement)
        {
            for (var i = 0; i < statement.Count; i++)
                Respelled(statement, statement.ValueAt(i));

            foreach (var inner in statement.Body)
                Respelled(inner);
        }

        /// <summary>Renames every function <paramref name="value" /> holds, itself included, in its arguments and the items of a list.</summary>
        static void Respelled(Statement statement, Value value)
        {
            if (value.Form == ValueForm.List)
            {
                var items = statement.Within(value);
                for (var i = 0; i < items.Count; i++)
                    Respelled(statement, items[i]);
                return;
            }

            if (value.Form != ValueForm.Reference)
                return;

            var member = statement.Member(value);
            var own = member.Count == 1 || member.Count == 2 && member[0].Word == BlockKinds.Own;
            var at = own && statement.Arguments(value).Count > 0 ? Array.IndexOf(Before, member[member.Count - 1].Word) : -1;
            if (at >= 0)
                statement.RespellLast(value, Now[at]);

            var given = statement.Arguments(value);
            for (var i = 0; i < given.Count; i++)
                Respelled(statement, given[i]);
        }
    }
}

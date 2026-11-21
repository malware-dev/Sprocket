using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>
    /// Version 2 alpha 1: Sprocket's own namespace, <c>ext</c>, is named <c>sprocket</c>. A program written before it says
    /// <c>ext.close</c>, <c>ext.toolbar(1, 1, 4)</c> and <c>expects ext.screen</c>, read as <c>sprocket.close</c>,
    /// <c>sprocket.toolbar(1, 1, 4)</c> and <c>expects sprocket.screen</c>.
    /// </summary>
    /// <remarks>Only a member and the type a slot expects are renamed; a system's own namespace called <c>ext</c> stays as it is.</remarks>
    public sealed class OwnNamespace : FormatChange
    {
        // Sprocket's own namespace as programs written before version 2 alpha 1 persist it.
        const string Ext = "ext";

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
            // A slot's type: required slot screen expects ext.screen.
            var slot = statement.Key == Keyword.Required || statement.Key == Keyword.Optional;
            if (slot && statement.Says(2, Keyword.Expects) && statement.ValueAt(3).Form == ValueForm.Qualified)
            {
                var parts = statement.Parts(3);
                if (parts.Count == 2 && parts[0].Word == Ext)
                    statement.Respell(statement.ValueAt(3), BlockKinds.Own);
            }

            for (var i = 0; i < statement.Count; i++)
                Respelled(statement, statement.ValueAt(i));

            foreach (var inner in statement.Body)
                Respelled(inner);
        }

        /// <summary>Renames the namespace of every member <paramref name="value" /> holds, itself included, in its arguments, its blocks and the items of a list.</summary>
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
            if (member.Count == 2 && member[0].Form == ValueForm.Identifier && member[0].Word == Ext)
                statement.Respell(value, BlockKinds.Own);

            var given = statement.Arguments(value);
            for (var i = 0; i < given.Count; i++)
                Respelled(statement, given[i]);
        }
    }
}

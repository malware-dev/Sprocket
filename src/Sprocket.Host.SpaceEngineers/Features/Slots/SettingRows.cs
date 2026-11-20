using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;

namespace Sprocket.Host.SpaceEngineers.Features.Slots
{
    /// <summary>How a setting stands in the rows of the installation panels: its mark, and the value one installation gives it, as a slot's row says its blocks.</summary>
    public static class SettingRows
    {
        /// <summary>
        /// Shows <paramref name="part" />, a setting, in <paramref name="row" /> as <paramref name="instance" /> has it: the value it
        /// gives, its default where it gives none, and an empty part, marked as still to be given where the setting has
        /// no default and the row is not <paramref name="unused" />.
        /// </summary>
        public static void Show(PartRow row, Part part, SystemInstance instance, bool unused)
        {
            row.Mark = Symbols.Setting;

            Literal own;
            if (instance != null && instance.AnswerTo(part.Id, out own))
                row.Say(SettingEditor.Said(part, own), false);
            else if (part.HasDefault)
                row.Say(SettingEditor.Said(part, part.Default), false);
            else
                row.Say(SentenceText.Empty, !unused);
        }
    }
}

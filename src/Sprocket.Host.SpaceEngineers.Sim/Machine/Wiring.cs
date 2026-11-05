using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VRage.Game.ModAPI;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Sim.Machine;

/// <summary>
/// What the rig's toolbars hold, kept between runs.
/// </summary>
/// <remarks>
/// In the game a construct's toolbars are in the save, right beside the program, so the two come
/// back together. The playground kept only the program - so a button wired in one run was unwired in
/// the next, and Sprocket did the honest thing with that: dropped the source, leaving a sentence
/// reading "When … …" and a tester wondering what they had broken.
/// <para>
/// One line per filled slot, and the whole key rather than a reference to one, because a slot's
/// payload is the only record of which sequence a button runs.
/// </para>
/// </remarks>
public static class Wiring
{
    /// <summary>This store's own key, which is the harness's business and never Sprocket's.</summary>
    public static readonly Guid Key = new Guid("A7E3C1D0-9B42-4F58-8E16-3C7D5A2B9F04");

    /// <summary><paramref name="rig" /> written out as this store keeps it.</summary>
    public static string Save(Rig rig)
    {
        var text = new StringBuilder();
        var held = new List<TerminalActionParameter>();

        foreach (Fitting fitting in rig.Fitted)
        {
            Toolbar toolbar = fitting.Toolbar;
            if (toolbar == null)
                continue;

            for (var slot = 0; slot < toolbar.SlotCount; slot++)
            {
                MyToolbarItemInfo item;
                if (!toolbar.TryGetItem(slot, out item))
                    continue;

                held.Clear();
                toolbar.Parameters(slot, held);

                text.Append(fitting.Id.ToString(CultureInfo.InvariantCulture)).Append(' ')
                    .Append(slot.ToString(CultureInfo.InvariantCulture)).Append(' ')
                    .Append(item.BlockEntityId.ToString(CultureInfo.InvariantCulture)).Append(' ')
                    .Append(item.ActionId).Append(' ')
                    .Append(held.Count > 0 ? held[0].Value as string : "")
                    .Append('\n');
            }
        }

        return text.ToString();
    }

    /// <summary>Puts back what was written, and ignores anything that no longer makes sense.</summary>
    public static void Load(string saved, Rig rig)
    {
        if (string.IsNullOrEmpty(saved))
            return;

        foreach (var line in saved.Split('\n'))
        {
            var words = line.Split(new[] { ' ' }, 5);
            if (words.Length < 5)
                continue;

            long blockId;
            int slot;
            long actionBlockId;
            if (!long.TryParse(words[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out blockId)
                || !int.TryParse(words[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out slot)
                || !long.TryParse(words[2], NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out actionBlockId))
            {
                continue;
            }

            Toolbar toolbar = Of(rig, blockId);
            if (toolbar == null || slot >= toolbar.SlotCount)
                continue;

            var parameters = new List<TerminalActionParameter>();
            if (words[4].Length > 0)
                parameters.Add(TerminalActionParameter.Get(words[4]));

            Toolbar.Changes changes = toolbar.Changing();
            changes.Set(slot, MyToolbarItemInfo.BlockAction(actionBlockId, words[3]), parameters);
            changes.Apply();
        }
    }

    static Toolbar Of(Rig rig, long blockId)
    {
        foreach (Fitting fitting in rig.Fitted)
        {
            if (fitting.Id == blockId)
                return fitting.Toolbar;
        }

        return null;
    }
}

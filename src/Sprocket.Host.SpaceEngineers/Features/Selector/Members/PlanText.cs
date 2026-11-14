using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using System.Collections.Generic;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Selector.Members
{
    /// <summary>What the list of what blocks offer says is coming for them.</summary>
    public static class PlanText
    {
        /// <summary>The note naming each of <paramref name="plans" />, in the words <paramref name="texts" /> give, or empty where there are none.</summary>
        public static string Note(Texts texts, IList<PlannedMember> plans)
        {
            if (plans.Count == 0)
                return "";

            var named = new string[plans.Count];
            for (var i = 0; i < named.Length; i++)
                named[i] = Of(texts, plans[i]);

            return Template.Fill(texts.PickPlanned, "planned", string.Join(", ", named));
        }

        /// <summary>What <paramref name="plan" /> is called, in the words <paramref name="texts" /> give.</summary>
        static string Of(Texts texts, PlannedMember plan)
        {
            switch (plan)
            {
                case PlannedMember.SorterList: return texts.PlanSorterList;
                default: return plan.ToString();
            }
        }
    }
}

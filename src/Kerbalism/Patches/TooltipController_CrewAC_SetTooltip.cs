using HarmonyLib;
using KSP.UI.TooltipTypes;
using System.Text;

namespace KERBALISM
{
	[HarmonyPatch(typeof(TooltipController_CrewAC))]
	[HarmonyPatch("SetTooltip")]
	public class TooltipController_CrewAC_SetTooltip
	{
		static StringBuilder sb = new StringBuilder(256);

		static void Postfix(TooltipController_CrewAC __instance, ProtoCrewMember pcm)
		{
			var crewRules = DB.Kerbal(pcm.name).rules;
			sb.Length = 0;

			foreach (var rule in Profile.rules)
			{
				if (!rule.lifetime) continue;
				if (!crewRules.ContainsKey(rule.name)) continue;

				var level = crewRules[rule.name].problem / rule.fatal_threshold;
				sb.Append(String.BuildString("<b>Career ", rule.title, "</b>: ", HumanReadable.Percentage(level), "\n"));
			}

			if(sb.Length > 0)
			{
				__instance.descriptionString += String.BuildString("\n\n", sb.ToString());
			}
		}
	}
}

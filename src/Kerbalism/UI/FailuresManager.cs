using System.Collections.Generic;
using UnityEngine;


namespace KERBALISM
{


	static class FailuresManager
	{
		internal static void Failman(this Panel p, Vessel v)
		{
			// avoid corner-case when this is called in a lambda after scene changes
			v = FlightGlobals.FindVessel(v.id);

			// if vessel doesn't exist anymore, leave the panel empty
			if (v == null) return;

			// get data
			VesselData vd = v.KerbalismData();

			// if not a valid vessel, leave the panel empty
			if (!vd.IsSimulated) return;

			// set metadata
			p.Title(String.BuildString(String.Ellipsis(v.vesselName, Styles.ScaleStringLength(20)), " ", String.Color(Local.QualityManagement_title, String.Kolor.LightGrey)));//"Quality Management"
			p.Width(Styles.ScaleWidthFloat(355.0f));
			p.paneltype = Panel.PanelType.failures;

			string section = string.Empty;

			// get devices
			List<ReliabilityInfo> devices = vd.ReliabilityStatus();

			int deviceCount = 0;

			// for each device
			foreach (var ri in devices)
			{
				if(section != Group2Section(ri.group))
				{
					section = Group2Section(ri.group);
					p.AddSection(section);
				}

				string status = StatusString(ri);

				// render device entry
				p.AddContent(
					label: ri.title,
					value: status,
					hover: () => Highlighter.Set(ri.partId, Color.blue));
				deviceCount++;
			}

			// no devices case
			if (deviceCount == 0)
			{
				p.AddContent("<i>"+Local.QualityManagement_noqualityinfo +"</i>");//no quality info
			}
		}

		static string Group2Section(string group)
		{
			if (string.IsNullOrEmpty(group)) return Local.QualityManagement_Misc;//"Misc"
			return group;
		}

		static string StatusString(ReliabilityInfo ri)
		{
			if (ri.broken)
			{
				if (ri.critical) return String.Color(Local.QualityManagement_busted, String.Kolor.Red);//"busted"
				return String.Color(Local.QualityManagement_needsrepair, String.Kolor.Orange);//"needs repair"
			}
			if (ri.NeedsMaintenance())
			{
				return String.Color(Local.QualityManagement_needsservice, String.Kolor.Yellow);//"needs service"
			}

			if (ri.rel_duration > 0.75) return String.Color(Local.QualityManagement_operationduration, String.Kolor.Yellow);//"operation duration"
			if (ri.rel_ignitions > 0.95) return String.Color(Local.QualityManagement_ignitionlimit, String.Kolor.Yellow);//"ignition limit"
			
			return String.Color(Local.QualityManagement_good, String.Kolor.Green);//"good"
		}
	}


} // KERBALISM


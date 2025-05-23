using System;
using System.Collections.Generic;
using System.Text;

namespace KERBALISM
{


	static class Telemetry
	{
		internal static void TelemetryPanel(this Panel p, Vessel v)
		{
			// avoid corner-case when this is called in a lambda after scene changes
			v = FlightGlobals.FindVessel(v.id);

			// if vessel doesn't exist anymore, leave the panel empty
			if (v == null) return;

			// get vessel data
			VesselData vd = v.KerbalismData();

			// if not a valid vessel, leave the panel empty
			if (!vd.IsSimulated) return;

			// set metadata
			p.Title(String.BuildString(String.Ellipsis(v.vesselName, Styles.ScaleStringLength(20)), " ", String.Color(Local.TELEMETRY_title, String.Kolor.LightGrey)));//"TELEMETRY"
			p.Width(Styles.ScaleWidthFloat(355.0f));
			p.paneltype = Panel.PanelType.telemetry;

			// time-out simulation
			if (p.Timeout(vd)) return;

			// get resources
			VesselResources resources = ResourceCache.Get(v);

			// get crew
			var crew = Lib.CrewList(v);

			// draw the content
			Render_crew(p, crew);
			if (Features.Science) Render_science(p, v, vd);
			Render_greenhouse(p, vd);
			Render_supplies(p, v, vd, resources);
			Render_habitat(p, v, vd);
			Render_environment(p, v, vd);

			// collapse eva kerbal sections into one
			if (v.isEVA) p.Collapse(Local.TELEMETRY_EVASUIT);//"EVA SUIT"
		}


		static void Render_environment(Panel p, Vessel v, VesselData vd)
		{
			// don't show env panel in eva kerbals
			if (v.isEVA) return;

			// get all sensor readings
			HashSet<string> readings = new HashSet<string>();
			if (v.loaded)
			{
				foreach (var s in Lib.FindModules<Sensor>(v))
				{
					readings.Add(s.type);
				}
			}
			else
			{
				foreach (ProtoPartModuleSnapshot m in Lib.FindModules(v.protoVessel, "Sensor"))
				{
					readings.Add(Lib.Proto.GetString(m, "type"));
				}
			}
			readings.Remove(string.Empty);

			p.AddSection(Local.TELEMETRY_ENVIRONMENT);//"ENVIRONMENT"

			if (vd.SolarPanelsAverageExposure >= 0.0)
			{
				var exposureString = vd.SolarPanelsAverageExposure.ToString("P1");
				if (vd.SolarPanelsAverageExposure < 0.2) exposureString = String.Color(exposureString, String.Kolor.Orange);
				p.AddContent(Local.TELEMETRY_SolarPanelsAverageExposure, exposureString, "<b>"+Local.TELEMETRY_Exposureignoringbodiesocclusion +"</b>\n<i>"+Local.TELEMETRY_Exposureignoringbodiesocclusion_desc +"</i>");//"solar panels average exposure""Exposure ignoring bodies occlusion""Won't change on unloaded vessels\nMake sure to optimize it before switching
			}

			foreach (string type in readings)
			{
				p.AddContent(type.ToLower().Replace('_', ' '), Sensor.Telemetry_content(v, vd, type), Sensor.Telemetry_tooltip(v, vd, type));
			}
			if (readings.Count == 0) p.AddContent("<i>"+Local.TELEMETRY_nosensorsinstalled +"</i>");//no sensors installed
		}

		static void Render_habitat(Panel p, Vessel v, VesselData vd)
		{
			// if habitat feature is disabled, do not show the panel
			if (!Features.Habitat) return;

			// if vessel is unmanned, do not show the panel
			if (vd.CrewCount == 0) return;

			// render panel, add some content based on enabled features
			p.AddSection(Local.TELEMETRY_HABITAT);//"HABITAT"
			if (Features.Poisoning) p.AddContent(Local.TELEMETRY_co2level, String.Color(vd.Poisoning > Settings.PoisoningThreshold, HumanReadable.Percentage(vd.Poisoning, "F2"), String.Kolor.Yellow));//"co2 level"
			if (Features.Radiation && v.isEVA) p.AddContent(Local.TELEMETRY_radiation, HumanReadable.Radiation(vd.EnvironmentHabitatRadiation));//"radiation"

			if (!v.isEVA)
			{
				if (Features.Pressure) p.AddContent(Local.TELEMETRY_pressure, HumanReadable.Pressure(vd.Pressure * Sim.PressureAtSeaLevel()));//"pressure"
				if (Features.Shielding) p.AddContent(Local.TELEMETRY_shielding, Habitat.Shielding_to_string(vd.Shielding));//"shielding"
				if (Features.LivingSpace) p.AddContent(Local.TELEMETRY_livingspace, Habitat.Living_space_to_string(vd.LivingSpace));//"living space"
				if (Features.Comfort) p.AddContent(Local.TELEMETRY_comfort, vd.Comforts.Summary(), vd.Comforts.Tooltip());//"comfort"
				if (Features.Pressure) p.AddContent(Local.TELEMETRY_EVAsavailable, vd.EnvironmentBreathable ? Local.TELEMETRY_EnvBreathable : HumanReadable.Integer(vd.Evas), vd.EnvironmentBreathable ? Local.TELEMETRY_Breathableatm : Local.TELEMETRY_approx);//"EVA's available""infinite""breathable atmosphere""approx (derived from stored N2)"
			}
		}

		static void Render_science(Panel p, Vessel v, VesselData vd)
		{
			// don't show env panel in eva kerbals
			if (v.isEVA) return;

			p.AddSection(Local.TELEMETRY_TRANSMISSION);//"TRANSMISSION"

			// comm status
			if (vd.filesTransmitted.Count > 0)
			{
				double transmitRate = 0.0;
				StringBuilder tooltip = new StringBuilder();
				tooltip.Append(string.Format("<align=left /><b>{0,-15}\t{1}</b>\n", Local.TELEMETRY_TRANSMISSION_rate, Local.TELEMETRY_filetransmitted));//"rate""file transmitted"
				for (int i = 0; i < vd.filesTransmitted.Count; i++)
				{
					transmitRate += vd.filesTransmitted[i].transmitRate;
					tooltip.Append(string.Format("{0,-15}\t{1}", HumanReadable.DataRate(vd.filesTransmitted[i].transmitRate), String.Ellipsis(vd.filesTransmitted[i].subjectData.FullTitle, 40u)));
					if (i < vd.filesTransmitted.Count - 1) tooltip.Append("\n");
				}
				
				p.AddContent(Local.TELEMETRY_transmitting, String.BuildString(vd.filesTransmitted.Count.ToString(), vd.filesTransmitted.Count > 1 ? " files at " : " file at ",  HumanReadable.DataRate(transmitRate)), tooltip.ToString());//"transmitting"
			}
			else
			{
				p.AddContent(Local.TELEMETRY_maxtransmissionrate, HumanReadable.DataRate(vd.Connection.rate));//"max transmission rate"
			}

			p.AddContent(Local.TELEMETRY_target, vd.Connection.target_name);//"target"

			// total science gained by vessel
			p.AddContent(Local.TELEMETRY_totalsciencetransmitted, HumanReadable.Science(vd.scienceTransmitted, false));//"total science transmitted"
		}

		static void Render_supplies(Panel p, Vessel v, VesselData vd, VesselResources resources)
		{
			int supplies = 0;
			// for each supply
			foreach (Supply supply in Profile.supplies)
			{
				// get resource info
				ResourceInfo res = resources.GetResource(v, supply.resource);

				// only show estimate if the resource is present
				if (res.Capacity <= 1e-10) continue;

				// render panel title, if not done already
				if (supplies == 0) p.AddSection(Local.TELEMETRY_SUPPLIES);//"SUPPLIES"

				// determine label
				var resource = PartResourceLibrary.Instance.resourceDefinitions[supply.resource];
				string label = String.SpacesOnCaps(resource.displayName).ToLower();

				StringBuilder sb = new StringBuilder();
				
				sb.Append("<align=left />");
				if (res.AverageRate != 0.0)
				{
					sb.Append(String.Color(res.AverageRate > 0.0,
						String.BuildString("+", SI.HumanOrSIRate(System.Math.Abs(res.AverageRate), resource.id)), String.Kolor.PosRate,
						String.BuildString("-", SI.HumanOrSIRate(System.Math.Abs(res.AverageRate), resource.id)), String.Kolor.NegRate,
						true));
				}
				else
				{
					sb.Append("<b>");
					sb.Append(Local.TELEMETRY_nochange);//no change
					sb.Append("</b>");
				}

				if (res.AverageRate < 0.0 && res.Level < 0.0001)
				{
					sb.Append(" <i>");
					sb.Append(Local.TELEMETRY_empty);//(empty)
					sb.Append("</i>");
				}
				else if (res.AverageRate > 0.0 && res.Level > 0.9999)
				{
					sb.Append(" <i>");
					sb.Append(Local.TELEMETRY_full);//(full)
					sb.Append("</i>");

				}
				else sb.Append("   "); // spaces to prevent alignement issues

				sb.Append("\t");
				sb.Append(SI.HumanOrSIAmount(res.Amount, resource.id));
				sb.Append("/");
				sb.Append(SI.HumanOrSIAmount(res.Capacity, resource.id));
				sb.Append(" (");
				sb.Append(res.Level.ToString("P0"));
				sb.Append(")");

				List<SupplyData.ResourceBrokerRate> brokers = vd.Supply(supply.resource).ResourceBrokers;
				if (brokers.Count > 0)
				{
					sb.Append("\n<b>------------    \t------------</b>");
					foreach (SupplyData.ResourceBrokerRate rb in brokers)
					{
						sb.Append("\n");
						sb.Append(String.Color(rb.rate > 0.0,
							String.BuildString("+", SI.HumanOrSIRate(System.Math.Abs(rb.rate), resource.id), "   "), String.Kolor.PosRate, // spaces to mitigate alignement issues
							String.BuildString("-", SI.HumanOrSIRate(System.Math.Abs(rb.rate), resource.id), "   "), String.Kolor.NegRate, // spaces to mitigate alignement issues
							true)); 
						sb.Append("\t");
						sb.Append(rb.broker.Title);
					}
				}

				string rate_tooltip = sb.ToString();

				// finally, render resource supply
				p.AddContent(label, HumanReadable.Duration(res.DepletionTime()), rate_tooltip);
				++supplies;
			}
		}


		static void Render_crew(Panel p, List<ProtoCrewMember> crew)
		{
			// do nothing if there isn't a crew, or if there are no rules
			if (crew.Count == 0 || Profile.rules.Count == 0) return;

			// panel section
			p.AddSection(Local.TELEMETRY_VITALS);//"VITALS"

			// for each crew
			foreach (ProtoCrewMember kerbal in crew)
			{
				// get kerbal data from DB
				KerbalData kd = DB.Kerbal(kerbal.name);

				// analyze issues
				UInt32 health_severity = 0;
				UInt32 stress_severity = 0;

				// generate tooltip
				List<string> tooltips = new List<string>();
				foreach (Rule r in Profile.rules)
				{
					// get rule data
					RuleData rd = kd.Rule(r.name);

					// add to the tooltip
					tooltips.Add(String.BuildString("<b>", HumanReadable.Percentage(rd.problem / r.fatal_threshold), "</b>\t", r.title));

					// analyze issue
					if (rd.problem > r.danger_threshold)
					{
						if (!r.breakdown) health_severity = System.Math.Max(health_severity, 2);
						else stress_severity = System.Math.Max(stress_severity, 2);
					}
					else if (rd.problem > r.warning_threshold)
					{
						if (!r.breakdown) health_severity = System.Math.Max(health_severity, 1);
						else stress_severity = System.Math.Max(stress_severity, 1);
					}
				}
				string tooltip = String.BuildString("<align=left />", string.Join("\n", tooltips.ToArray()));

				// generate kerbal name
				string name = kerbal.name.ToLower().Replace(" kerman", string.Empty);

				// render selectable title
				p.AddContent(String.Ellipsis(name, Styles.ScaleStringLength(30)), kd.disabled ? String.Color(Local.TELEMETRY_HYBERNATED, String.Kolor.Cyan) : string.Empty);//"HYBERNATED"
				p.AddRightIcon(health_severity == 0 ? Textures.health_white : health_severity == 1 ? Textures.health_yellow : Textures.health_red, tooltip);
				p.AddRightIcon(stress_severity == 0 ? Textures.brain_white : stress_severity == 1 ? Textures.brain_yellow : Textures.brain_red, tooltip);
			}
		}

		static void Render_greenhouse(Panel p, VesselData vd)
		{
			// do nothing without greenhouses
			if (vd.Greenhouses.Count == 0) return;

			// panel section
			p.AddSection(Local.TELEMETRY_GREENHOUSE);//"GREENHOUSE"

			// for each greenhouse
			for (int i = 0; i < vd.Greenhouses.Count; ++i)
			{
				var greenhouse = vd.Greenhouses[i];

				// state string
				string state = greenhouse.issue.Length > 0
				  ? String.Color(greenhouse.issue, String.Kolor.Yellow)
				  : greenhouse.growth >= 0.99
				  ? String.Color(Local.TELEMETRY_readytoharvest, String.Kolor.Green)//"ready to harvest"
				  : Local.TELEMETRY_growing;//"growing"

				// tooltip with summary
				string tooltip = greenhouse.growth < 0.99 ? String.BuildString
				(
				  "<align=left />",
				  Local.TELEMETRY_timetoharvest, "\t<b>", HumanReadable.Duration(greenhouse.tta), "</b>\n",//"time to harvest"
				  Local.TELEMETRY_growth, "\t\t<b>", HumanReadable.Percentage(greenhouse.growth), "</b>\n",//"growth"
				  Local.TELEMETRY_naturallighting, "\t<b>", HumanReadable.Flux(greenhouse.natural), "</b>\n",//"natural lighting"
				  Local.TELEMETRY_artificiallighting, "\t<b>", HumanReadable.Flux(greenhouse.artificial), "</b>"//"artificial lighting"
				) : string.Empty;

				// render it
				p.AddContent(String.BuildString(Local.TELEMETRY_crop, " #", (i + 1).ToString()), state, tooltip);//"crop"

				// issues too, why not
				p.AddRightIcon(greenhouse.issue.Length == 0 ? Textures.plant_white : Textures.plant_yellow, tooltip);
			}
		}
	}


} // KERBALISM

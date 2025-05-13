namespace KERBALISM
{
	static class VesselConfig
	{
		internal static void Config(this Panel p, Vessel v)
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
			p.Title(String.BuildString(String.Ellipsis(v.vesselName, Styles.ScaleStringLength(20)), " ", String.Color(Local.VESSELCONFIG_title, String.Kolor.LightGrey)));//"VESSEL CONFIG"
			p.Width(Styles.ScaleWidthFloat(355.0f));
			p.paneltype = Panel.PanelType.config;

			// toggle rendering
			string tooltip;
			p.AddSection(Local.VESSELCONFIG_RENDERING);//"RENDERING"

			tooltip = Local.VESSELCONFIG_ShowVessel_desc;
			p.AddContent(Local.VESSELCONFIG_ShowVessel, string.Empty, tooltip);
			p.AddRightIcon(vd.configShowVessel ? Textures.toggle_green : Textures.toggle_red, tooltip, () => p.Toggle(ref vd.configShowVessel));

			if (Features.Reliability)
			{
				tooltip = Local.VESSELCONFIG_Highlightfailed_desc;//"Highlight failed components"
				p.AddContent(Local.VESSELCONFIG_Highlightfailed, string.Empty, tooltip);//"highlight malfunctions"
				p.AddRightIcon(vd.configHighlights ? Textures.toggle_green : Textures.toggle_red, tooltip, () => p.Toggle(ref vd.configHighlights));
			}

			// toggle messages
			p.AddSection(Local.VESSELCONFIG_MESSAGES);//"MESSAGES"
			tooltip = Local.VESSELCONFIG_EClow;//"Receive a message when\nElectricCharge level is low"
			p.AddContent(Local.VESSELCONFIG_battery, string.Empty, tooltip);//"battery"
			p.AddRightIcon(vd.configElectricCharge ? Textures.toggle_green : Textures.toggle_red, tooltip, () => p.Toggle(ref vd.configElectricCharge));
			if (Features.Supplies)
			{
				tooltip = Local.VESSELCONFIG_Supplylow;//"Receive a message when\nsupply resources level is low"
				p.AddContent(Local.VESSELCONFIG_supply, string.Empty, tooltip);//"supply"
				p.AddRightIcon(vd.configSupplies ? Textures.toggle_green : Textures.toggle_red, tooltip, () => p.Toggle(ref vd.configSupplies));
			}
			if (API.Comm.handlers.Count > 0 || HighLogic.fetch.currentGame.Parameters.Difficulty.EnableCommNet)
			{
				tooltip = Local.VESSELCONFIG_Signallost;//"Receive a message when signal is lost or obtained"
				p.AddContent(Local.VESSELCONFIG_signal, string.Empty, tooltip);//"signal"
				p.AddRightIcon(vd.configSignal ? Textures.toggle_green : Textures.toggle_red, tooltip, () => p.Toggle(ref vd.configSignal));
			}
			if (Features.Reliability)
			{
				tooltip = Local.VESSELCONFIG_Componentfail;//"Receive a message\nwhen a component fail"
				p.AddContent(Local.VESSELCONFIG_reliability, string.Empty, tooltip);//"reliability"
				p.AddRightIcon(vd.configMalfunctions ? Textures.toggle_green : Textures.toggle_red, tooltip, () => p.Toggle(ref vd.configMalfunctions));
			}
			if (Features.SpaceWeather)
			{
				tooltip = Local.VESSELCONFIG_CMEevent;//"Receive a message\nduring CME events"
				p.AddContent(Local.VESSELCONFIG_storm, string.Empty, tooltip);//"storm"
				p.AddRightIcon(vd.configStorms ? Textures.toggle_green : Textures.toggle_red, tooltip, () => p.Toggle(ref vd.configStorms));
			}
			if (Features.Automation)
			{
				tooltip = Local.VESSELCONFIG_ScriptExe;//"Receive a message when\nscripts are executed"
				p.AddContent(Local.VESSELCONFIG_script, string.Empty, tooltip);//"script"
				p.AddRightIcon(vd.configScripts ? Textures.toggle_green : Textures.toggle_red, tooltip, () => p.Toggle(ref vd.configScripts));
			}
		}
	}


} // KERBALISM


namespace KERBALISM
{
	class CommsMessages
	{
		internal static void Update(Vessel v, VesselData vd, double elapsed_s)
		{
			if (!Lib.IsVessel(v))
				return;

			// do nothing if network is not ready
			if (vd.CommHandler == null || !vd.CommHandler.IsReady)
				return;

			// maintain and send messages
			// - do not send messages during/after solar storms
			// - do not send messages for EVA kerbals
			if (!v.isEVA && v.situation != Vessel.Situations.PRELAUNCH)
			{
				if (!vd.messageSignal && !vd.Connection.linked)
				{
					vd.messageSignal = true;
					if (vd.configSignal)
					{
						string subtext = Local.UI_transmissiondisabled;

						switch (vd.Connection.Status)
						{
							case LinkStatus.plasma:
								subtext = Local.UI_Plasmablackout;
								break;
							case LinkStatus.storm:
								subtext = Local.UI_Stormblackout;
								break;
							default:
								if (vd.CrewCount == 0)
								{
									switch (Settings.UnlinkedControl)
									{
										case UnlinkedCtrl.none:
											subtext = Local.UI_noctrl;
											break;
										case UnlinkedCtrl.limited:
											subtext = Local.UI_limitedcontrol;
											break;
									}
								}
								break;
						}

						Message.Post(Severity.warning, String.BuildString(Local.UI_signallost, " <b>", v.vesselName, "</b>"), subtext);
					}
				}
				else if (vd.messageSignal && vd.Connection.linked)
				{
					vd.messageSignal = false;
					if (vd.configSignal)
					{
						Message.Post(Severity.relax, String.BuildString("<b>", v.vesselName, "</b> ", Local.UI_signalback),
						  vd.Connection.Status == (int)LinkStatus.direct_link ? Local.UI_directlink :
							String.BuildString(Local.UI_relayby, " <b>", vd.Connection.target_name, "</b>"));
					}
				}
			}
		}
	}
}

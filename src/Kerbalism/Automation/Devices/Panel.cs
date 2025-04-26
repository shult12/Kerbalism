using KSP.Localization;
using System;

namespace KERBALISM
{
	sealed class PanelDevice : LoadedDevice<SolarPanelFixer>
	{
		internal PanelDevice(SolarPanelFixer module) : base(module) { }

		internal override string Name
		{
			get
			{
				if (module.SolarPanel.IsRetractable())
					return Local.SolarPanel_deployable;//"solar panel (deployable)"
				else
					return Local.SolarPanel_nonretractable;//"solar panel (non retractable)"
			}
		}

		internal override string Status
		{
			get
			{
				switch (module.state)
				{
					case SolarPanelFixer.PanelState.Retracted: return Lib.Color(Local.Generic_RETRACTED, Lib.Kolor.Yellow);
					case SolarPanelFixer.PanelState.Extending: return Local.Generic_EXTENDING;
					case SolarPanelFixer.PanelState.Extended: return Lib.Color(Local.Generic_EXTENDED, Lib.Kolor.Green);
					case SolarPanelFixer.PanelState.Retracting: return Local.Generic_RETRACTING;
				}
				return Local.Statu_unknown;//"unknown"
			}
		}

		internal override bool IsVisible => module.SolarPanel.SupportAutomation(module.state);

		internal override void Ctrl(bool value)
		{
			if (value && module.state == SolarPanelFixer.PanelState.Retracted) module.ToggleState();
			if (!value && module.state == SolarPanelFixer.PanelState.Extended) module.ToggleState();
		}

		internal override void Toggle()
		{
			if (module.state == SolarPanelFixer.PanelState.Retracted || module.state == SolarPanelFixer.PanelState.Extended)
				module.ToggleState();
		}
	}

	sealed class ProtoPanelDevice : ProtoDevice<SolarPanelFixer>
	{
		internal ProtoPanelDevice(SolarPanelFixer prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		internal override string Name
		{
			get
			{
				if (prefab.SolarPanel.IsRetractable())
					return Local.SolarPanel_deployable;//"solar panel (deployable)"
				else
					return Local.SolarPanel_nonretractable;//"solar panel (non retractable)"
			}
		}

		internal override uint PartId => protoPart.flightID;

		internal override string Status
		{
			get
			{
				string state = Lib.Proto.GetString(protoModule, "state");
				switch (state)
				{
					case "Retracted": return Lib.Color(Local.Generic_RETRACTED, Lib.Kolor.Yellow);
					case "Extended": return Lib.Color(Local.Generic_EXTENDED, Lib.Kolor.Green);
				}
				return Local.Statu_unknown;//"unknown"
			}
		}

		internal override bool IsVisible => prefab.SolarPanel.SupportProtoAutomation(protoModule);

		internal override void Ctrl(bool value)
		{
			SolarPanelFixer.PanelState state = (SolarPanelFixer.PanelState)Enum.Parse(typeof(SolarPanelFixer.PanelState), Lib.Proto.GetString(protoModule, "state"));
			if ((value && state == SolarPanelFixer.PanelState.Retracted)
				||
				(!value && state == SolarPanelFixer.PanelState.Extended))
			SolarPanelFixer.ProtoToggleState(prefab, protoModule, state);
		}

		internal override void Toggle()
		{
			SolarPanelFixer.PanelState state = (SolarPanelFixer.PanelState)Enum.Parse(typeof(SolarPanelFixer.PanelState), Lib.Proto.GetString(protoModule, "state"));
			SolarPanelFixer.ProtoToggleState(prefab, protoModule, state);
		}
	}


} // KERBALISM

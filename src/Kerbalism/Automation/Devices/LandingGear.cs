using KSP.Localization;
using ModuleWheels;

namespace KERBALISM
{
	class LandingGearEC : DeviceEC
	{
		internal LandingGearEC(ModuleWheelDeployment landingGear, double extra_Deploy)
		{
			this.landingGear = landingGear;
			this.extra_Deploy = extra_Deploy;
		}

		protected override bool IsConsuming
		{
			get
			{
				if (landingGear.stateString == Localizer.Format("#autoLOC_6002270") || landingGear.stateString == Localizer.Format("#autoLOC_234856"))
				{
					actualCost = extra_Deploy;
					return true;
				}
				return false;
			}
		}

		internal override void GUI_Update(bool isEnabled)
		{
			Logging.LogDebugStack("Buttons is '{0}' for '{1}' landingGear", Logging.LogLevel.Message, (isEnabled == true ? "ON" : "OFF"), landingGear.part.partInfo.title);
			landingGear.Events["EventToggle"].active = isEnabled;
		}

		internal override void FixModule(bool isEnabled)
		{
			ToggleActions(landingGear, isEnabled);
		}

		ModuleWheelDeployment landingGear;
	}
}

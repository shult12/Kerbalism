using System.Collections.Generic;

namespace KERBALISM
{
	abstract class DeviceEC
	{
		internal KeyValuePair<bool, double> GetConsume()
		{
			return new KeyValuePair<bool, double>(IsConsuming, actualCost);
		}

		protected abstract bool IsConsuming { get; }

		internal abstract void GUI_Update(bool isEnabled);

		internal abstract void FixModule(bool isEnabled);

		protected void ToggleActions(PartModule partModule, bool value)
		{
			Lib.LogDebugStack("Part '{0}'.'{1}', setting actions to {2}", Lib.LogLevel.Message, partModule.part.partInfo.title, partModule.moduleName, value ? "ON" : "OFF");
			foreach (BaseAction ac in partModule.Actions)
			{
				ac.active = value;
			}
		}

		// Return
		protected double actualCost;
		protected double extra_Cost;
		protected double extra_Deploy;
	}
}

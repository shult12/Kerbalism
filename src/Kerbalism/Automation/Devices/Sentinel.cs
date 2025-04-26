using KSP.Localization;
using SentinelMission;

namespace KERBALISM
{
	sealed class SentinelDevice : LoadedDevice<KerbalismSentinel>
	{
		internal SentinelDevice(KerbalismSentinel module) : base(module) { }

		internal override string Name => SentinelUtilities.SentinelPartTitle;

		internal override string Status
		{
			get
			{
				if (!module.isTrackingEnabled)
					return Local.Generic_DISABLED;

				return module.status;
			}
		}

		internal override void Ctrl(bool value)
		{
			if (value)
				module.StartTracking();
			else
				module.StopTracking();
		}

		internal override void Toggle() => Ctrl(!module.isTrackingEnabled);
	}

	sealed class ProtoSentinelDevice : ProtoDevice<KerbalismSentinel>
	{
		internal ProtoSentinelDevice(KerbalismSentinel prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule, Vessel vessel)
			: base(prefab, protoPart, protoModule)
		{
			this.vessel = vessel;
		}

		readonly Vessel vessel;

		internal override string Name => SentinelUtilities.SentinelPartTitle;

		internal override string Status
		{
			get
			{
				if (!Lib.Proto.GetBool(protoModule, "isTrackingEnabled"))
					return Local.Generic_DISABLED;

				if (Lib.Proto.GetBool(protoModule, "isTracking"))
				{
					if (SentinelUtilities.FindInnerAndOuterBodies(vessel, out var innerBody, out var outerBody))
					{
						return (SentinelUtilities.SentinelCanScan(vessel, innerBody, outerBody) ? Localizer.Format("#autoLOC_6002291", outerBody.displayName) : Localizer.Format("#autoLOC_6002292", outerBody.displayName));
					}
					else
					{
						return Localizer.Format("#autoLOC_6002290");
					}
				}

				VesselData vd = vessel.KerbalismData();

				if (!vd.Connection.linked || vd.Connection.rate < prefab.comms_rate)
				{
					return "Comms connection too weak";
				}

				ResourceInfo ec = ResourceCache.GetResource(vessel, "ElectricCharge");

				if (ec.Amount <= double.Epsilon)
				{
					return Local.Module_Experiment_issue4; // "no Electricity"
				}

				return Localizer.Format("#autoLOC_6002296");
			}
		}

		internal override void Ctrl(bool value) => Lib.Proto.Set(protoModule, "isTrackingEnabled", value);

		internal override void Toggle() => Ctrl(!Lib.Proto.GetBool(protoModule, "isTrackingEnabled"));
	}
}

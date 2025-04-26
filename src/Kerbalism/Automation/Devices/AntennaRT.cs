using KSP.Localization;
using static ModuleDeployablePart;

namespace KERBALISM
{
	sealed class AntennaRTDevice : LoadedDevice<PartModule>
	{
		internal AntennaRTDevice(PartModule module) : base(module) { }

		internal override string Name => "antenna";

		internal override string Status
		{
			get
			{
				return Lib.ReflectionValue<bool>(module, "IsRTActive")
					? Lib.Color(Local.Generic_ACTIVE, Lib.Kolor.Green)
					: Lib.Color(Local.Generic_INACTIVE, Lib.Kolor.Yellow);
			}
		}

		internal override void Ctrl(bool value) => Lib.ReflectionValue(module, "IsRTActive", value);

		internal override void Toggle() => Ctrl(!Lib.ReflectionValue<bool>(module, "IsRTActive"));
	}

	sealed class ProtoAntennaRTDevice : ProtoDevice<PartModule>
	{
		internal ProtoAntennaRTDevice(PartModule prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		internal override string Name => "antenna";

		internal override string Status
		{
			get
			{
				return Lib.Proto.GetBool(protoModule, "IsRTActive")
						  ? Lib.Color(Local.Generic_ACTIVE, Lib.Kolor.Green)
						  : Lib.Color(Local.Generic_INACTIVE, Lib.Kolor.Yellow);
			}
		}

		internal override void Ctrl(bool value) => Lib.Proto.Set(protoModule, "IsRTActive", value);

		internal override void Toggle() => Ctrl(!Lib.Proto.GetBool(protoModule, "IsRTActive"));
	}
}

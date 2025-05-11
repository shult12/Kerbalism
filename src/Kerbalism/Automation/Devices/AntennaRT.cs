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
				return Reflection.ReflectionValue<bool>(module, "IsRTActive")
					? String.Color(Local.Generic_ACTIVE, String.Kolor.Green)
					: String.Color(Local.Generic_INACTIVE, String.Kolor.Yellow);
			}
		}

		internal override void Ctrl(bool value) => Reflection.ReflectionValue(module, "IsRTActive", value);

		internal override void Toggle() => Ctrl(!Reflection.ReflectionValue<bool>(module, "IsRTActive"));
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
						  ? String.Color(Local.Generic_ACTIVE, String.Kolor.Green)
						  : String.Color(Local.Generic_INACTIVE, String.Kolor.Yellow);
			}
		}

		internal override void Ctrl(bool value) => Lib.Proto.Set(protoModule, "IsRTActive", value);

		internal override void Toggle() => Ctrl(!Lib.Proto.GetBool(protoModule, "IsRTActive"));
	}
}

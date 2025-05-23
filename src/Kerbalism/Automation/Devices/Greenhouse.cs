namespace KERBALISM
{
	sealed class GreenhouseDevice : LoadedDevice<Greenhouse>
	{
		internal GreenhouseDevice(Greenhouse module) : base(module) { }

		internal override string Status => String.Color(module.active, Local.Generic_ENABLED, String.Kolor.Green, Local.Generic_DISABLED, String.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			if (module.active != value) module.Toggle();
		}

		internal override void Toggle()
		{
			Ctrl(!module.active);
		}
	}

	sealed class ProtoGreenhouseDevice : ProtoDevice<Greenhouse>
	{
		internal ProtoGreenhouseDevice(Greenhouse prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		internal override string Status => String.Color(Lib.Proto.GetBool(protoModule, "active"), Local.Generic_ENABLED, String.Kolor.Green, Local.Generic_DISABLED, String.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			Lib.Proto.Set(protoModule, "active", value);
		}

		internal override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "active"));
		}
	}


} // KERBALISM

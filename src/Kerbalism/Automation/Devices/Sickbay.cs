namespace KERBALISM
{
	sealed class SickbayDevice : LoadedDevice<Sickbay>
	{
		internal SickbayDevice(Sickbay module) : base(module) { }

		internal override string Status
			=> Lib.Color(module.running, Local.Generic_RUNNING, Lib.Kolor.Green, Local.Generic_STOPPED, Lib.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			module.running = value;
		}

		internal override void Toggle()
		{
			Ctrl(!module.running);
		}

		internal override bool IsVisible => module.slots > 0;
	}

	sealed class ProtoSickbayDevice : ProtoDevice<Sickbay>
	{
		internal ProtoSickbayDevice(Sickbay prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		internal override string Status
			=> Lib.Color(Lib.Proto.GetBool(protoModule, "running"), Local.Generic_RUNNING, Lib.Kolor.Green, Local.Generic_STOPPED, Lib.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			Lib.Proto.Set(protoModule, "running", value);
		}

		internal override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "running"));
		}

		internal override bool IsVisible => Lib.Proto.GetUInt(protoModule, "slots", 0) > 0;
	}


} // KERBALISM




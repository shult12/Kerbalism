namespace KERBALISM
{


	sealed class RingDevice : LoadedDevice<GravityRing>
	{
		internal RingDevice(GravityRing module) : base(module) { }

		internal override string Name => "gravity ring";

		internal override string Status => String.Color(module.deployed, Local.Generic_DEPLOYED, String.Kolor.Green, Local.Generic_RETRACTED, String.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			if (module.deployed != value)
			{
				module.Toggle();
			}
		}

		internal override void Toggle()
		{
			Ctrl(!module.deployed);
		}
	}


	sealed class ProtoRingDevice : ProtoDevice<GravityRing>
	{
		internal ProtoRingDevice(GravityRing prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		internal override string Name => "gravity ring";

		internal override string Status => String.Color(Lib.Proto.GetBool(protoModule, "deployed"), Local.Generic_DEPLOYED, String.Kolor.Green, Local.Generic_RETRACTED, String.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			Lib.Proto.Set(protoModule, "deployed", value);
		}

		internal override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "deployed"));
		}
	}


} // KERBALISM

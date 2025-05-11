namespace KERBALISM
{

	sealed class ScannerDevice : LoadedDevice<PartModule>
	{
		internal ScannerDevice(PartModule module) : base(module) { }

		internal override string Status => String.Color(Reflection.ReflectionValue<bool>(module, "scanning"), Local.Generic_ENABLED, String.Kolor.Green, Local.Generic_DISABLED, String.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			bool scanning = Reflection.ReflectionValue<bool>(module, "scanning");
			if (scanning && !value) module.Events["stopScan"].Invoke();
			else if (!scanning && value) module.Events["startScan"].Invoke();
		}

		internal override void Toggle()
		{
			Ctrl(!Reflection.ReflectionValue<bool>(module, "scanning"));
		}
	}

	sealed class ProtoScannerDevice : ProtoDevice<PartModule>
	{
		readonly Vessel vessel;

		internal ProtoScannerDevice(PartModule prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule, Vessel v)
			: base(prefab, protoPart, protoModule)
		{
			this.vessel = v;
		}

		internal override string Status => String.Color(Lib.Proto.GetBool(protoModule, "scanning"), Local.Generic_ENABLED, String.Kolor.Green, Local.Generic_DISABLED, String.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			bool scanning = Lib.Proto.GetBool(protoModule, "scanning");
			if (scanning && !value) SCANsat.StopScanner(vessel, protoModule, prefab.part);
			else if (!scanning && value) SCANsat.ResumeScanner(vessel, protoModule, prefab.part);
		}

		internal override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "scanning"));
		}
	}


} // KERBALISM


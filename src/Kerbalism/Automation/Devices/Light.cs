using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;


namespace KERBALISM
{
	sealed class LightDevice : LoadedDevice<ModuleLight>
	{
		internal LightDevice(ModuleLight module) : base(module) { }

		internal override string Name => "light";

		internal override string Status => Lib.Color(module.isOn, Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			if (value) module.LightsOn();
			else module.LightsOff();
		}

		internal override void Toggle()
		{
			Ctrl(!module.isOn);
		}
	}


	sealed class ProtoLightDevice : ProtoDevice<ModuleLight>
	{
		internal ProtoLightDevice(ModuleLight prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		internal override string Name => "light";

		internal override string Status => Lib.Color(Lib.Proto.GetBool(protoModule, "isOn"), Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);

		internal override void Ctrl(bool value)
		{
			Lib.Proto.Set(protoModule, "isOn", value);
		}

		internal override void Toggle()
		{
			Ctrl(!Lib.Proto.GetBool(protoModule, "isOn"));
		}
	}


} // KERBALISM

namespace KERBALISM
{
	class VesselDeviceTransmit : VesselDevice
	{
		internal VesselDeviceTransmit(Vessel v, VesselData vd) : base(v, vd) { }

		internal override string Name => "data transmission";

		internal override string Status => String.Color(vesselData.deviceTransmit, Local.Generic_ENABLED, String.Kolor.Green, Local.Generic_DISABLED, String.Kolor.Yellow);

		internal override void Ctrl(bool value) => vesselData.deviceTransmit = value;

		internal override void Toggle() => vesselData.deviceTransmit = !vesselData.deviceTransmit;
	}
}

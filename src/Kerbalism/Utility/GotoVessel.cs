namespace KERBALISM
{
	static class GotoVessel
	{
		[KSPField(isPersistant = true)] public static int version = 0; //TODO: doesn't seem to work
		internal static void JumpToVessel(Vessel v)
		{
			string _saveGame = GamePersistence.SaveGame("Goto_" + version.ToString(), HighLogic.SaveFolder, SaveMode.OVERWRITE);

			// Keep until 3 backups of Goto
			if (version++ > 3) version = 0;

			if (Lib.IsFlight())
			{
				FlightGlobals.SetActiveVessel(v);
			}
			else
			{
				int _idx = HighLogic.CurrentGame.flightState.protoVessels.FindLastIndex(pv => pv.vesselID == v.id);

				if (_idx != -1)
				{
					FlightDriver.StartAndFocusVessel(_saveGame, _idx);
				}
				else
				{
					Logging.Log("Invalid vessel Id:" + _idx);
				}
			}
		}

		internal static void SetVesselAsTarget(Vessel v)
		{
			if (v != FlightGlobals.ActiveVessel) FlightGlobals.fetch.SetVesselTarget(v);
		}
	}
}

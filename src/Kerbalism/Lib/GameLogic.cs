namespace KERBALISM
{
	static class GameLogic
	{
		///<summary>return true if the current scene is flight</summary>
		internal static bool IsFlight()
		{
			return HighLogic.LoadedSceneIsFlight;
		}

		///<summary>return true if the current scene is editor</summary>
		internal static bool IsEditor()
		{
			return HighLogic.LoadedSceneIsEditor;
		}

		///<summary>return true if the current scene is not the main menu</summary>
		internal static bool IsGame()
		{
			return HighLogic.LoadedSceneIsGame;
		}

		///<summary>return true if game is paused</summary>
		internal static bool IsPaused()
		{
			return FlightDriver.Pause || Planetarium.Pause;
		}

		///<summary>return true if a tutorial scenario or making history mission is active</summary>
		internal static bool IsScenario()
		{
			return HighLogic.CurrentGame.Mode == Game.Modes.SCENARIO
				|| HighLogic.CurrentGame.Mode == Game.Modes.SCENARIO_NON_RESUMABLE
				|| HighLogic.CurrentGame.Mode == Game.Modes.MISSION_BUILDER
				|| HighLogic.CurrentGame.Mode == Game.Modes.MISSION;
		}

		///<summary>disable the module and return true if a tutorial scenario is active</summary>
		internal static bool DisableScenario(PartModule m)
		{
			if (IsScenario())
			{
				m.enabled = false;
				m.isEnabled = false;
				return true;
			}
			return false;
		}

		///<summary>if current game is neither science or career, disable the module and return false</summary>
		internal static bool ModuleEnableInScienceAndCareer(PartModule m)
		{
			switch (HighLogic.CurrentGame.Mode)
			{
				case Game.Modes.CAREER:
				case Game.Modes.SCIENCE_SANDBOX:
					return true;
				default:
					m.enabled = false;
					m.isEnabled = false;
					return false;
			}
		}

	}
}

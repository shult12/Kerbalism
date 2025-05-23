namespace KERBALISM
{

	class PlannerController : PartModule
	{
		// config
		[KSPField] public bool toggle = true;                       // true to show the toggle button in editor
		[KSPField] public string title = string.Empty;              // name to show on the button

		// persistence
		[KSPField(isPersistant = true)] public bool considered;     // true to consider the part modules in planner


		public override void OnStart(StartState state)
		{
			// don't break tutorial scenarios
			if (GameLogic.DisableScenario(this)) return;

			if (GameLogic.IsEditor())
			{
				Events["Toggle"].active = toggle;
			}
			else
			{
				enabled = false;
				isEnabled = false;
			}
		}

		void Update()
		{
			if (!part.IsPAWVisible())
				return;

			Events["Toggle"].guiName = UI.StatusToggle
			(
			  Local.StatuToggle_Simulate.Format(title),//string.Format("Simulate {0} in planner", title)
			  considered ? "<b><color=#00ff00>"+ Local.PlannerController_yes + "</color></b>" : "<b><color=#ffff00>"+ Local.PlannerController_no + "</color></b>"//yes  no
			);
		}

		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "_", active = true)]
		public void Toggle()
		{
			considered = !considered;

			// refresh VAB/SPH ui
			if (GameLogic.IsEditor()) GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
		}
	}


} // KERBALISM


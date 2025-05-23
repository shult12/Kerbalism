using ModuleWheels;
using System;
using System.Collections.Generic;

namespace KERBALISM
{
	class Deploy : PartModule
	{
		[KSPField] public string type;                      // component name
		[KSPField] public double extra_Cost = 0;            // extra energy cost to keep the part active
		[KSPField] public double extra_Deploy = 0;          // extra eergy cost to do a deploy(animation)

		// Support Reliability
		[KSPField(isPersistant = true, guiName = "#KERBALISM_Deploy_isBroken", guiUnits = "", guiFormat = "")]//IsBroken
		public bool isBroken;                               // is it broken
		bool lastBrokenState;                        // broken state has changed since last update?
		bool lastFixedBrokenState;                   // broken state has changed since last fixed update?

		[KSPField(guiName = "#KERBALISM_Deploy_actualCost", guiUnits = "/s", guiFormat = "F3")]//EC Usage
		public double actualCost = 0;                       // Energy Consume

		// Vessel info
		bool hasEnergy;                              // Check if vessel has energy, otherwise will disable animations and functions
		bool isConsuming;                            // Module is consuming energy
		bool hasEnergyChanged;                       // Energy state has changed since last update?
		bool hasFixedEnergyChanged;                  // Energy state has changed since last fixed update?
		ResourceInfo resources;

		PartModule module;                           // component cache, the Reliability.cs is one to many, instead the Deploy will be one to one
		KeyValuePair<bool, double> modReturn;        // Return from DeviceEC

		public override void OnStart(StartState state)
		{
			// don't break tutorial scenarios & do something only in Flight scenario
			if (GameLogic.DisableScenario(this) || !GameLogic.IsFlight()) return;

			// cache list of modules
			module = part.FindModulesImplementing<PartModule>().FindLast(k => k.moduleName == type);

			// get energy from cache
			resources = ResourceCache.GetResource(vessel, "ElectricCharge");
			hasEnergy = resources.Amount > double.Epsilon;

			// Force the update to run at least once
			lastBrokenState = !isBroken;
			hasEnergyChanged = !hasEnergy;
			hasFixedEnergyChanged = !hasEnergy;

#if DEBUG
			// setup UI
			Fields["actualCost"].guiActive = true;
			Fields["isBroken"].guiActive = true;
#endif
		}

		public override void OnUpdate()
		{
			if (!GameLogic.IsFlight() || module == null) return;

			// get energy from cache
			resources = ResourceCache.GetResource(vessel, "ElectricCharge");
			hasEnergy = resources.Amount > double.Epsilon;

			// Update UI only if hasEnergy has changed or if is broken state has changed
			if (isBroken)
			{
				if (isBroken != lastBrokenState)
				{
					lastBrokenState = isBroken;
					Update_UI(!isBroken);
				}
			}
			else if (hasEnergyChanged != hasEnergy)
			{
				Logging.LogDebugStack("Energy state has changed: {0}", Logging.LogLevel.Message, hasEnergy);

				hasEnergyChanged = hasEnergy;
				lastBrokenState = false;
				// Update UI
				Update_UI(hasEnergy);
			}
			// Constantly Update UI for special modules
			if (isBroken) Constant_OnGUI(!isBroken);
			else Constant_OnGUI(hasEnergy);

			if (!hasEnergy || isBroken)
			{
				actualCost = 0;
				isConsuming = false;
			}
			else
			{
				isConsuming = GetIsConsuming();
			}
		}

		void FixedUpdate()
		{
			if (!GameLogic.IsFlight() || module == null) return;

			if (isBroken)
			{
				if (isBroken != lastFixedBrokenState)
				{
					lastFixedBrokenState = isBroken;
					FixModule(!isBroken);
				}
			}
			else if (hasFixedEnergyChanged != hasEnergy)
			{
				hasFixedEnergyChanged = hasEnergy;
				lastFixedBrokenState = false;
				// Update module
				FixModule(hasEnergy);
			}

			// If isConsuming
			if (isConsuming && resources != null) resources.Consume(actualCost * Kerbalism.elapsed_s, ResourceBroker.Deploy);
		}

		bool GetIsConsuming()
		{
			try
			{
				switch (type)
				{
					case "ModuleWheelDeployment":
						modReturn = new LandingGearEC(module as ModuleWheelDeployment, extra_Deploy).GetConsume();
						actualCost = modReturn.Value;
						return modReturn.Key;
				}
			}
			catch (Exception e)
			{
				Logging.Log("'" + part.partInfo.title + "' : " + e.Message);
			}
			actualCost = extra_Deploy;
			return true;
		}

		void Update_UI(bool isEnabled)
		{
			try
			{
				switch (type)
				{
					case "ModuleWheelDeployment":
						new LandingGearEC(module as ModuleWheelDeployment, extra_Deploy).GUI_Update(isEnabled);
						break;
				}
			}
			catch (Exception e)
			{
				Logging.Log("'" + part.partInfo.title + "' : " + e.Message);
			}
		}

		void FixModule(bool isEnabled)
		{
			try
			{
				switch (type)
				{
					case "ModuleWheelDeployment":
						new LandingGearEC(module as ModuleWheelDeployment, extra_Deploy).FixModule(isEnabled);
						break;
				}
			}
			catch (Exception e)
			{
				Logging.Log("'" + part.partInfo.title + "' : " + e.Message);
			}
		}

		// Some modules need to constantly update the UI 
		void Constant_OnGUI(bool isEnabled)
		{
			// wtf?
			/*
			try
			{
			}
			catch (Exception e)
			{
				Logging.Log("'" + part.partInfo.title + "' : " + e.Message);
			}
			*/
		}

		void ToggleActions(PartModule partModule, bool value)
		{
			//Logging.LogDebugStack("Part '{0}'.'{1}', setting actions to {2}", partModule.part.partInfo.title, partModule.moduleName, value ? "ON" : "OFF");
			foreach (BaseAction ac in partModule.Actions)
			{
				ac.active = value;
			}
		}

		static void BackgroundUpdate(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot m, Deploy deploy, ResourceInfo ec, double elapsed_s)
		{
			if (deploy.isConsuming) ec.Consume(deploy.extra_Cost * elapsed_s, ResourceBroker.Deploy);
		}
	}
}

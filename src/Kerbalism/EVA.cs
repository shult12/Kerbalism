using System.Collections.Generic;
using UnityEngine;


namespace KERBALISM
{


	static class EVA
	{
		internal static void Update(Vessel v)
		{
			// do nothing if not an eva kerbal
			if (!v.isEVA) return;

			// get KerbalEVA module
			KerbalEVA kerbal = Lib.FindModules<KerbalEVA>(v)[0];

			// get resource handler
			ResourceInfo ec = ResourceCache.GetResource(v, "ElectricCharge");

			// determine if headlamps need ec
			// - not required if there is no EC capacity in eva kerbal (no ec supply in profile)
			// - not required if no EC cost for headlamps is specified (set by the user)
			bool need_ec = ec.Capacity > double.Epsilon && Settings.HeadLampsCost > double.Epsilon;

			// consume EC for the headlamps
			if (need_ec && kerbal.lampOn)
			{
				ec.Consume(Settings.HeadLampsCost * Kerbalism.elapsed_s, ResourceBroker.Light);
			}

			// force the headlamps on/off
			HeadLamps(kerbal, kerbal.lampOn && (!need_ec || ec.Amount > double.Epsilon));

			// if dead
			if (IsDeadEVA(v))
			{
				// enforce freezed state
				Freeze(kerbal);

				// disable modules
				DisableModules(kerbal);

				// remove plant flag action
				kerbal.flagItems = 0;
			}
		}


		// return true if the vessel is a kerbal eva, and is flagged as dead
		internal static bool IsDeadEVA(Vessel v)
		{
			if (!v.isEVA) return false;
			List<ProtoCrewMember> crew = Lib.CrewList(v);
			if (crew.Count == 0) return true;
			return DB.Kerbal(crew[0].name).eva_dead;
		}


		// set headlamps on/off
		internal static void HeadLamps(KerbalEVA kerbal, bool b)
		{
			// set the lights intensity
			kerbal.headLamp.GetComponent<Light>().intensity = b ? 1.0f : 0.0f;

			// set the flare effects
			foreach (var comp in kerbal.GetComponentsInChildren<Renderer>())
			{
				if (comp.name == "flare1" || comp.name == "flare2")
				{
					comp.enabled = b;
				}
			}
		}


		static void DisableModules(KerbalEVA kerbal)
		{
			for (int i = 0; i < kerbal.part.Modules.Count; ++i)
			{
				// get module
				PartModule m = kerbal.part.Modules[i];

				// ignore KerbalEVA itself
				if (m.moduleName == "KerbalEVA") continue;

				// keep the flag decal
				if (m.moduleName == "FlagDecal") continue;

				// disable all other modules
				m.isEnabled = false;
				m.enabled = false;
			}
		}


		static void Freeze(KerbalEVA kerbal)
		{
			// set kerbal to the 'freezed' unescapable state
			// how it works:
			// - kerbal animations and ragdoll state are driven by a finite-state-machine (FSM)
			// - this function is called every frame for all active eva kerbals flagged as dead
			// - if the FSM current state is already 'freezed', we do nothing and this function is a no-op
			// - we create an 'inescapable' state called 'freezed'
			// - we switch the FSM to that state using an ad-hoc event from current state
			// - once the 'freezed' state is set, the FSM cannot switch to any other states
			// - the animator of the object is stopped to stop any left-over animations from previous state

			// do nothing if already freezed
			if (!string.IsNullOrEmpty(kerbal.fsm.currentStateName) && kerbal.fsm.currentStateName != "freezed")
			{
				// create freezed state
				KFSMState freezed = new KFSMState("freezed");

				// create freeze event
				KFSMEvent eva_freeze = new KFSMEvent("EVAfreeze")
				{
					GoToStateOnEvent = freezed,
					updateMode = KFSMUpdateMode.MANUAL_TRIGGER
				};
				kerbal.fsm.AddEvent(eva_freeze, kerbal.fsm.CurrentState);

				// trigger freeze event
				kerbal.fsm.RunEvent(eva_freeze);

				// stop animations
				kerbal.GetComponent<Animation>().Stop();
			}
		}
	}


} // KERBALISM

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace KERBALISM
{
	static class Background
	{
		class BackgroundDelegate
		{
			static readonly Type[] signature =
			{
				typeof(Vessel),
				typeof(ProtoPartSnapshot),
				typeof(ProtoPartModuleSnapshot),
				typeof(PartModule),
				typeof(Part),
				typeof(Dictionary<string, double>),
				typeof(List<KeyValuePair<string, double>>),
				typeof(double)
			};

			static readonly Dictionary<Type, BackgroundDelegate> supportedModules = new Dictionary<Type, BackgroundDelegate>();
			static readonly List<Type> unsupportedModules = new List<Type>();

#if KSP18
			// non-generic actions are too new to be used in pre-KSP18
			internal Func<Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part, Dictionary<string, double>, List<KeyValuePair<string, double>>, double, string> function;
#else
			internal MethodInfo methodInfo;
#endif
			BackgroundDelegate(MethodInfo methodInfo)
			{
#if KSP18
				function = (Func<Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part, Dictionary<string, double>, List<KeyValuePair<string, double>>, double, string>)
					Delegate.CreateDelegate(typeof(Func<Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part, Dictionary<string, double>, List<KeyValuePair<string, double>>, double, string>), methodInfo);
#else
				this.methodInfo = methodInfo;
#endif
			}

			internal string Invoke(
				Vessel vessel,
				ProtoPartSnapshot protoPart,
				ProtoPartModuleSnapshot protoPartModule,
				PartModule partModule,
				Part part,
				Dictionary<string, double> availableResources,
				List<KeyValuePair<string, double>> resourceChangeRequest,
				double elapsedSeconds)
			{
				// TODO optimize this for performance
#if KSP18
				string result = function(vessel, protoPart, protoPartModule, partModule, part, availableResources, resourceChangeRequest, elapsedSeconds);

				if (string.IsNullOrEmpty(result))
					result = partModule.moduleName;

				return result;
#else
				object result = methodInfo.Invoke(null, new object[] { vessel, protoPart, protoPartModule, partModule, part, availableResources, resourceChangeRequest, elapsedSeconds });

				if (result == null)
					return partModule.moduleName;

				return result.ToString();
#endif
			}

			internal static BackgroundDelegate Instance(PartModule partModule)
			{
				BackgroundDelegate result;

				Type type = partModule.GetType();
				supportedModules.TryGetValue(type, out result);

				if (result != null)
					return result;

				if (unsupportedModules.Contains(type))
					return null;

				MethodInfo methodInfo = type.GetMethod("BackgroundUpdate", signature);
				if (methodInfo == null)
				{
					unsupportedModules.Add(type);
					return null;
				}

				result = new BackgroundDelegate(methodInfo);
				supportedModules[type] = result;
				return result;
			}
		}

		enum ModuleType
		{
			Reliability = 0,
			Experiment,
			Greenhouse,
			GravityRing,
			Harvester,
			Laboratory,
			Command,
			Generator,
			Converter,
			Drill,
			AsteroidDrill,
			StockLab,
			Light,
			Scanner,
			FissionGenerator,
			RadioisotopeGenerator,
			CryoTank,
			Unknown,
			FNGenerator,
			NonRechargeBattery,
			KerbalismProcess,
			SolarPanelFixer,
			KerbalismSentinel,

			/// <summary>Module implementing the kerbalism background API</summary>
			APIModule
		}

		static ModuleType GetModuleType(string module)
		{
			switch (module)
			{
				case "Reliability": return ModuleType.Reliability;
				case "Experiment": return ModuleType.Experiment;
				case "Greenhouse": return ModuleType.Greenhouse;
				case "GravityRing": return ModuleType.GravityRing;
				case "Harvester": return ModuleType.Harvester;
				case "Laboratory": return ModuleType.Laboratory;
				case "ModuleCommand": return ModuleType.Command;
				case "ModuleGenerator": return ModuleType.Generator;

				case "ModuleResourceConverter":
				case "ModuleKPBSConverter":
				case "FissionReactor": return ModuleType.Converter;

				// Kerbalism default profile uses the Harvester module (both for air and ground harvesting)
				// Other profiles use the stock ModuleResourceHarvester (only for ground harvesting)
				case "ModuleResourceHarvester": return ModuleType.Drill;
				case "ModuleAsteroidDrill": return ModuleType.AsteroidDrill;
				case "ModuleScienceConverter": return ModuleType.StockLab;

				case "ModuleLight":
				case "ModuleColoredLensLight":
				case "ModuleMultiPointSurfaceLight": return ModuleType.Light;

				case "KerbalismScansat": return ModuleType.Scanner;
				case "FissionGenerator": return ModuleType.FissionGenerator;
				case "ModuleRadioisotopeGenerator": return ModuleType.RadioisotopeGenerator;
				case "ModuleCryoTank": return ModuleType.CryoTank;
				case "FNGenerator": return ModuleType.FNGenerator;
				case "KerbalismProcess": return ModuleType.KerbalismProcess;
				case "SolarPanelFixer": return ModuleType.SolarPanelFixer;
				case "KerbalismSentinel": return ModuleType.KerbalismSentinel;
			}
			return ModuleType.Unknown;
		}

		class BackgroundPartModule
		{
			internal ProtoPartSnapshot protoPart;
			internal ProtoPartModuleSnapshot protoPartModule;
			internal PartModule partModule;
			internal Part part;
			internal ModuleType type;
		}

		internal static void Update(Vessel vessel, VesselData vesselData, VesselResources resources, double elapsedSeconds)
		{
			if (!Lib.IsVessel(vessel))
				return;

			// get most used resource handlers
			ResourceInfo electricCharge = resources.GetResource(vessel, "ElectricCharge");

			List<ResourceInfo> allResources = resources.GetAllResources(vessel);
			Dictionary<string, double> availableResources = new Dictionary<string, double>();

			foreach (ResourceInfo resource in allResources)
				availableResources[resource.ResourceName] = resource.Amount;

			List<KeyValuePair<string, double>> resourceChangeRequests = new List<KeyValuePair<string, double>>();

			foreach (BackgroundPartModule entry in GetBackgroundPartModules(vessel))
			{
				switch(entry.type)
				{
					case ModuleType.Reliability:
						Reliability.BackgroundUpdate(vessel, entry.protoPart, entry.protoPartModule, entry.partModule as Reliability, elapsedSeconds);
						break;

					case ModuleType.Experiment: // experiments use the prefab as a singleton instead of a static method
						(entry.partModule as Experiment).BackgroundUpdate(vessel, vesselData, entry.protoPartModule, electricCharge, resources, elapsedSeconds);
						break;

					case ModuleType.Greenhouse:
						Greenhouse.BackgroundUpdate(vessel, entry.protoPartModule, entry.partModule as Greenhouse, vesselData, resources, elapsedSeconds);
						break;

					case ModuleType.GravityRing:
						GravityRing.BackgroundUpdate(vessel, entry.protoPart, entry.protoPartModule, entry.partModule as GravityRing, electricCharge, elapsedSeconds);
						break;

					case ModuleType.Harvester: // Kerbalism ground and air harvester protoPartModule
						Harvester.BackgroundUpdate(vessel, entry.protoPartModule, entry.partModule as Harvester, elapsedSeconds);
						break;

					case ModuleType.Laboratory:
						Laboratory.BackgroundUpdate(vessel, entry.protoPart, entry.protoPartModule, entry.partModule as Laboratory, electricCharge, elapsedSeconds);
						break;

					case ModuleType.Command:
						ProcessCommand(vessel, entry.protoPart, entry.partModule as ModuleCommand, resources, elapsedSeconds);
						break;

					case ModuleType.Generator:
						ProcessGenerator(entry.protoPartModule, entry.partModule as ModuleGenerator, resources, elapsedSeconds);
						break;

					case ModuleType.Converter:
						ProcessConverter(vessel, entry.protoPartModule, entry.partModule as ModuleResourceConverter, resources, elapsedSeconds);
						break;

					case ModuleType.Drill: // Stock ground harvester protoPartModule
						ProcessDrill(vessel, entry.protoPartModule, entry.partModule as ModuleResourceHarvester, resources, elapsedSeconds);
						break;

					/*case ModuleType.AsteroidDrill: // Stock asteroid harvester protoPartModule
						ProcessAsteroidDrill(vessel, e.protoPart, e.protoPartModule, e.module as ModuleAsteroidDrill, resources, elapsedSeconds);
						break;*/

					case ModuleType.StockLab:
						ProcessStockLab(entry.protoPartModule, entry.partModule as ModuleScienceConverter, electricCharge, elapsedSeconds);
						break;

					case ModuleType.Light:
						ProcessLight(entry.protoPartModule, entry.partModule as ModuleLight, electricCharge, elapsedSeconds);
						break;

					case ModuleType.Scanner:
						KerbalismScansat.BackgroundUpdate(vessel, entry.protoPart, entry.protoPartModule, entry.partModule as KerbalismScansat, entry.part, vesselData, electricCharge, elapsedSeconds);
						break;

					case ModuleType.FissionGenerator:
						ProcessFissionGenerator(entry.protoPart, entry.partModule, electricCharge, elapsedSeconds);
						break;

					case ModuleType.RadioisotopeGenerator:
						ProcessRadioisotopeGenerator(vessel, entry.partModule, electricCharge, elapsedSeconds);
						break;

					case ModuleType.CryoTank:
						ProcessCryoTank(vessel, entry.protoPart, entry.protoPartModule, entry.partModule, resources, electricCharge, elapsedSeconds);
						break;

					case ModuleType.FNGenerator:
						ProcessFNGenerator(entry.protoPartModule, electricCharge, elapsedSeconds);
						break;

					case ModuleType.SolarPanelFixer:
						SolarPanelFixer.BackgroundUpdate(vessel, entry.protoPartModule, entry.partModule as SolarPanelFixer, vesselData, electricCharge, elapsedSeconds);
						break;

					case ModuleType.KerbalismSentinel:
						KerbalismSentinel.BackgroundUpdate(vessel, entry.protoPartModule, entry.partModule as KerbalismSentinel, vesselData, electricCharge, elapsedSeconds);
						break;

					case ModuleType.APIModule:
						ProcessApiModule(vessel, entry.protoPart, entry.protoPartModule, entry.part, entry.partModule, resources, availableResources, resourceChangeRequests, elapsedSeconds);
						break;
				}
			}
		}

		static List<BackgroundPartModule> GetBackgroundPartModules(Vessel vessel)
		{
			List<BackgroundPartModule> result = Cache.VesselObjectsCache<List<BackgroundPartModule>>(vessel, "background");
			if (result != null)
				return result;

			result = new List<BackgroundPartModule>();

			// store data required to support multiple modules of same type in a part
			var prefabData = new Dictionary<string, Lib.Module_prefab_data>();

			// for each part
			foreach (ProtoPartSnapshot protoPart in vessel.protoVessel.protoPartSnapshots)
			{
				// get part prefab (required for module properties)
				Part part = PartLoader.getPartInfoByName(protoPart.partName).partPrefab;

				// get all module prefabs
				List<PartModule> partModules = part.FindModulesImplementing<PartModule>();

				// clear module indexes
				prefabData.Clear();

				// for each module
				foreach (ProtoPartModuleSnapshot protoPartModule in protoPart.modules)
				{
					// TODO : this is to migrate pre-3.1 saves using WarpFixer to the new SolarPanelFixer. At some point in the future we can remove this code.
					if (protoPartModule.moduleName == "WarpFixer")
						MigrateWarpFixer(vessel, part, protoPart, protoPartModule);

					// get the module prefab
					// if the prefab doesn't contain this module, skip it
					PartModule partModule = Lib.ModulePrefab(partModules, protoPartModule.moduleName, prefabData);
					if (!partModule)
						continue;

					// if the module is disabled, skip it
					// note: this must be done after ModulePrefab is called, so that indexes are right
					if (!Lib.Proto.GetBool(protoPartModule, "isEnabled"))
						continue;

					// get module type
					// if the type is unknown, skip it
					ModuleType type = GetModuleType(protoPartModule.moduleName);
					if (type == ModuleType.Unknown)
					{
						BackgroundDelegate backgroundDelegate = BackgroundDelegate.Instance(partModule);

						if (backgroundDelegate != null)
							type = ModuleType.APIModule;
						else
							continue;
					}

					var entry = new BackgroundPartModule
					{
						protoPart = protoPart,
						protoPartModule = protoPartModule,
						partModule = partModule,
						part = part,
						type = type
					};
					result.Add(entry);
				}
			}

			Cache.SetVesselObjectsCache(vessel, "background", result);
			return result;
		}

		static void ProcessApiModule(
			Vessel vessel,
			ProtoPartSnapshot protoPart,
			ProtoPartModuleSnapshot protoPartModule,
			Part part,
			PartModule partModule,
			VesselResources resources,
			Dictionary<string, double> availableResources,
			List<KeyValuePair<string, double>> resourceChangeRequests,
			double elapsedSeconds)
		{
			resourceChangeRequests.Clear();

			try
			{
				string title = BackgroundDelegate.Instance(partModule).Invoke(vessel, protoPart, protoPartModule, partModule, part, availableResources, resourceChangeRequests, elapsedSeconds);

				foreach(KeyValuePair<string, double> changeRequest in resourceChangeRequests)
				{
					if (changeRequest.Value > 0)
						resources.Produce(vessel, changeRequest.Key, changeRequest.Value * elapsedSeconds, ResourceBroker.GetOrCreate(title));
					else if (changeRequest.Value < 0)
						resources.Consume(vessel, changeRequest.Key, -changeRequest.Value * elapsedSeconds, ResourceBroker.GetOrCreate(title));
				}
			}
			catch (Exception ex)
			{
				Logging.Log("BackgroundUpdate in PartModule " + partModule.moduleName + " excepted: " + ex.Message + "\n" + ex.ToString());
			}
		}

		static void ProcessFNGenerator(ProtoPartModuleSnapshot protoPartModule, ResourceInfo electricCharge, double elapsedSeconds)
		{
			string maxPowerString = Lib.Proto.GetString(protoPartModule, "MaxPowerStr");
			double maxPower;

			if (maxPowerString.Contains("GW"))
				maxPower = double.Parse(maxPowerString.Replace(" GW", "")) * 1000000;
			else if (maxPowerString.Contains("MW"))
				maxPower = double.Parse(maxPowerString.Replace(" MW", "")) * 1000;
			else
				maxPower = double.Parse(maxPowerString.Replace(" KW", ""));

			electricCharge.Produce(maxPower * elapsedSeconds, ResourceBroker.KSPIEGenerator);
		}

		static void ProcessCommand(Vessel vessel, ProtoPartSnapshot protoPart, ModuleCommand command, VesselResources resources, double elapsedSeconds)
		{
			// do not consume if this is a MCM with no crew
			// rationale: for consistency, the game doesn't consume resources for MCM without crew in loaded vessels
			//            this make some sense: you left a vessel with some battery and nobody on board, you expect it to not consume EC
			if (command.minimumCrew == 0 || protoPart.protoModuleCrew.Count > 0)
			{
				// for each input resource
				foreach (ModuleResource inputResource in command.resHandler.inputResources)
				{
					// consume the resource
					resources.Consume(vessel, inputResource.name, inputResource.rate * elapsedSeconds, ResourceBroker.Command);
				}
			}
		}

		static void ProcessGenerator(ProtoPartModuleSnapshot protoPartModule, ModuleGenerator generator, VesselResources resources, double elapsedSeconds)
		{
			// if active
			if (Lib.Proto.GetBool(protoPartModule, "generatorIsActive"))
			{
				// create and commit recipe
				ResourceRecipe recipe = new ResourceRecipe(ResourceBroker.StockConverter);

				foreach (ModuleResource inputResource in generator.resHandler.inputResources)
				{
					recipe.AddInput(inputResource.name, inputResource.rate * elapsedSeconds);
				}
				foreach (ModuleResource outputResource in generator.resHandler.outputResources)
				{
					recipe.AddOutput(outputResource.name, outputResource.rate * elapsedSeconds, true);
				}
				resources.AddRecipe(recipe);
			}
		}


		static void ProcessConverter(Vessel vessel, ProtoPartModuleSnapshot protoPartModule, ModuleResourceConverter converter, VesselResources resources, double elapsedSeconds)
		{
			// note: ignore stock temperature mechanic of converters
			// note: ignore auto shutdown
			// note: non-mandatory resources 'dynamically scale the ratios', that is exactly what mandatory resources do too (DERP ALERT)
			// note: 'undo' stock behavior by forcing lastUpdateTime to now (to minimize overlapping calculations from this and stock post-facto simulation)

			// if active
			if (Lib.Proto.GetBool(protoPartModule, "IsActivated"))
			{
				// determine if vessel is full of all output resources
				// note: comparing against previous amount
				bool full = true;
				foreach (ResourceRatio outputResource in converter.outputList)
				{
					ResourceInfo resource = resources.GetResource(vessel, outputResource.ResourceName);
					full &= resource.Level >= converter.FillAmount - double.Epsilon;
				}

				// if not full
				if (!full)
				{
					// deduce crew bonus
					int expLevel = -1;
					if (converter.UseSpecialistBonus)
					{
						foreach (ProtoCrewMember crewMember in Lib.CrewList(vessel))
						{
							if (crewMember.experienceTrait.Effects.Find(x => x.Name == converter.ExperienceEffect) != null)
							{
								expLevel = System.Math.Max(expLevel, crewMember.experienceLevel);
							}
						}
					}
					double expBonus = expLevel < 0
					  ? converter.EfficiencyBonus * converter.SpecialistBonusBase
					  : converter.EfficiencyBonus * (converter.SpecialistBonusBase + (converter.SpecialistEfficiencyFactor * (expLevel + 1)));

					// create and commit recipe
					ResourceRecipe recipe = new ResourceRecipe(ResourceBroker.StockConverter);

					foreach (ResourceRatio inputResource in converter.inputList)
					{
						recipe.AddInput(inputResource.ResourceName, inputResource.Ratio * expBonus * elapsedSeconds);
					}
					foreach (ResourceRatio outputResource in converter.outputList)
					{
						recipe.AddOutput(outputResource.ResourceName, outputResource.Ratio * expBonus * elapsedSeconds, outputResource.DumpExcess);
					}
					resources.AddRecipe(recipe);
				}

				// undo stock behavior by forcing last_update_time to now
				Lib.Proto.Set(protoPartModule, "lastUpdateTime", Planetarium.GetUniversalTime());
			}
		}


		static void ProcessDrill(Vessel vessel, ProtoPartModuleSnapshot protoPartModule, ModuleResourceHarvester harvester, VesselResources resources, double elapsedSeconds)
		{
			// note: ignore stock temperature mechanic of harvesters
			// note: ignore auto shutdown
			// note: ignore depletion (stock seem to do the same)
			// note: 'undo' stock behavior by forcing lastUpdateTime to now (to minimize overlapping calculations from this and stock post-facto simulation)

			// if active
			if (Lib.Proto.GetBool(protoPartModule, "IsActivated"))
			{
				// do nothing if full
				// note: comparing against previous amount
				if (resources.GetResource(vessel, harvester.ResourceName).Level < harvester.FillAmount - double.Epsilon)
				{
					// deduce crew bonus
					int expLevel = -1;
					if (harvester.UseSpecialistBonus)
					{
						foreach (ProtoCrewMember crewMember in Lib.CrewList(vessel))
						{
							if (crewMember.experienceTrait.Effects.Find(x => x.Name == harvester.ExperienceEffect) != null)
							{
								expLevel = System.Math.Max(expLevel, crewMember.experienceLevel);
							}
						}
					}
					double expBonus = expLevel < 0
					  ? harvester.EfficiencyBonus * harvester.SpecialistBonusBase
					  : harvester.EfficiencyBonus * (harvester.SpecialistBonusBase + (harvester.SpecialistEfficiencyFactor * (expLevel + 1)));

					// detect amount of ore in the ground
					AbundanceRequest request = new AbundanceRequest
					{
						Altitude = vessel.altitude,
						BodyId = vessel.mainBody.flightGlobalsIndex,
						CheckForLock = false,
						Latitude = vessel.latitude,
						Longitude = vessel.longitude,
						ResourceType = (HarvestTypes)harvester.HarvesterType,
						ResourceName = harvester.ResourceName
					};
					double abundance = ResourceMap.Instance.GetAbundance(request);

					// if there is actually something (should be if active when unloaded)
					if (abundance > harvester.HarvestThreshold)
					{
						// create and commit recipe
						ResourceRecipe recipe = new ResourceRecipe(ResourceBroker.StockDrill);

						foreach (ResourceRatio inputResource in harvester.inputList)
						{
							recipe.AddInput(inputResource.ResourceName, inputResource.Ratio * elapsedSeconds);
						}
						recipe.AddOutput(harvester.ResourceName, abundance * harvester.Efficiency * expBonus * elapsedSeconds, true);
						resources.AddRecipe(recipe);
					}
				}

				// undo stock behavior by forcing last_update_time to now
				Lib.Proto.Set(protoPartModule, "lastUpdateTime", Planetarium.GetUniversalTime());
			}
		}

		// Doesn't work since squad refactored the ModuleAsteroidInfo / ModuleAsteroidResource for Comets (in 1.10 ?), and was probably not working even before that.
		static void ProcessAsteroidDrill(Vessel vessel, ProtoPartModuleSnapshot protoPartModule, ModuleAsteroidDrill asteroidDrill, VesselResources resources, double elapsedSeconds)
		{
			// note: untested
			// note: ignore stock temperature mechanic of asteroid drills
			// note: ignore auto shutdown
			// note: 'undo' stock behavior by forcing lastUpdateTime to now (to minimize overlapping calculations from this and stock post-facto simulation)

			// if active
			if (Lib.Proto.GetBool(protoPartModule, "IsActivated"))
			{
				// get asteroid data
				ProtoPartModuleSnapshot asteroidInfo = null;
				ProtoPartModuleSnapshot asteroidResource = null;

				foreach (ProtoPartSnapshot protoPart in vessel.protoVessel.protoPartSnapshots)
				{
					if (asteroidInfo == null)
						asteroidInfo = protoPart.modules.Find(x => x.moduleName == "ModuleAsteroidInfo");

					if (asteroidResource == null)
						asteroidResource = protoPart.modules.Find(x => x.moduleName == "ModuleAsteroidResource");
				}

				// if there is actually an asteroid attached to this active asteroid drill (it should)
				if (asteroidInfo != null && asteroidResource != null)
				{
					// get some data
					double massThreshold = Lib.Proto.GetDouble(asteroidInfo, "massThresholdVal");
					double mass = Lib.Proto.GetDouble(asteroidInfo, "currentMassVal");
					double abundance = Lib.Proto.GetDouble(asteroidResource, "abundance");
					string resourceName = Lib.Proto.GetString(asteroidResource, "resourceName");
					double resourceDensity = PartResourceLibrary.Instance.GetDefinition(resourceName).density;

					// if asteroid isn't depleted
					if (mass > massThreshold && abundance > double.Epsilon)
					{
						// deduce crew bonus
						int expLevel = -1;
						if (asteroidDrill.UseSpecialistBonus)
						{
							foreach (ProtoCrewMember crewMember in Lib.CrewList(vessel))
							{
								if (crewMember.experienceTrait.Effects.Find(x => x.Name == asteroidDrill.ExperienceEffect) != null)
								{
									expLevel = System.Math.Max(expLevel, crewMember.experienceLevel);
								}
							}
						}
						double expBonus = expLevel < 0
						? asteroidDrill.EfficiencyBonus * asteroidDrill.SpecialistBonusBase
						: asteroidDrill.EfficiencyBonus * (asteroidDrill.SpecialistBonusBase + (asteroidDrill.SpecialistEfficiencyFactor * (expLevel + 1)));

						// determine resource extracted
						double resourceAmount = abundance * asteroidDrill.Efficiency * expBonus * elapsedSeconds;

						// transform EC into mined resource
						ResourceRecipe recipe = new ResourceRecipe(ResourceBroker.StockDrill);
						recipe.AddInput("ElectricCharge", asteroidDrill.PowerConsumption * elapsedSeconds);
						recipe.AddOutput(resourceName, resourceAmount, true);
						resources.AddRecipe(recipe);

						// if there was ec
						// note: comparing against amount in previous simulation step
						if (resources.GetResource(vessel, "ElectricCharge").Amount > double.Epsilon)
						{
							// consume asteroid mass
							Lib.Proto.Set(asteroidInfo, "currentMassVal", mass - resourceDensity * resourceAmount);
						}
					}
				}
				// undo stock behavior by forcing last_update_time to now
				Lib.Proto.Set(protoPartModule, "lastUpdateTime", Planetarium.GetUniversalTime());
			}
		}


		static void ProcessStockLab(ProtoPartModuleSnapshot protoPartModule, ModuleScienceConverter lab, ResourceInfo electricCharge, double elapsedSeconds)
		{
			// note: we are only simulating the EC consumption
			// note: there is no easy way to 'stop' the lab when there isn't enough EC

			// if active
			if (Lib.Proto.GetBool(protoPartModule, "IsActivated"))
			{
				// consume ec
				electricCharge.Consume(lab.powerRequirement * elapsedSeconds, ResourceBroker.ScienceLab);
			}
		}


		static void ProcessLight(ProtoPartModuleSnapshot protoPartModule, ModuleLight light, ResourceInfo electricCharge, double elapsedSeconds)
		{
			if (light.useResources && Lib.Proto.GetBool(protoPartModule, "isOn"))
			{
				electricCharge.Consume(light.resourceAmount * elapsedSeconds, ResourceBroker.Light);
			}
		}

		/*
		static void ProcessScanner(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot m, PartModule scanner, Part part_prefab, VesselData vd, ResourceInfo ec, double elapsed_s)
		{
			// get ec consumption rate
			double power = SCANsat.EcConsumption(scanner);

			// if the scanner doesn't require power to operate, we aren't interested in simulating it
			if (power <= double.Epsilon) return;

			// get scanner state
			bool is_scanning = Lib.Proto.GetBool(m, "scanning");

			// if its scanning
			if (is_scanning)
			{
				// consume ec
				ec.Consume(power * elapsed_s, "scanner");

				// if there isn't ec
				// - comparing against amount in previous simulation step
				if (ec.amount <= double.Epsilon)
				{
					// unregister scanner
					SCANsat.StopScanner(v, m, part_prefab);
					is_scanning = false;

					// remember disabled scanner
					vd.scansat_id.Add(p.flightID);

					// give the user some feedback
					if (vd.cfg_ec) Message.Post(String.BuildString("SCANsat sensor was disabled on <b>", v.vesselName, "</b>"));
				}
			}
			// if it was disabled in background
			else if (vd.scansat_id.Contains(p.flightID))
			{
				// if there is enough ec
				// note: comparing against amount in previous simulation step
				// re-enable at 25% EC
				if (ec.level > 0.25)
				{
					// re-enable the scanner
					SCANsat.ResumeScanner(v, m, part_prefab);
					is_scanning = true;

					// give the user some feedback
					if (vd.cfg_ec) Message.Post(String.BuildString("SCANsat sensor resumed operations on <b>", v.vesselName, "</b>"));
				}
			}

			// forget active scanners
			if (is_scanning) vd.scansat_id.Remove(p.flightID);
		}
		*/

		static void ProcessFissionGenerator(ProtoPartSnapshot protoPart, PartModule fissionGenerator, ResourceInfo electricCharge, double elapsedSeconds)
		{
			// note: ignore heat

			double power = Reflection.ReflectionValue<float>(fissionGenerator, "PowerGeneration");
			ProtoPartModuleSnapshot reactor = protoPart.modules.Find(x => x.moduleName == "FissionReactor");

			double tweakable = reactor == null ? 1.0 : Lib.ConfigValue(reactor.moduleValues, "CurrentPowerPercent", 100.0) * 0.01;

			electricCharge.Produce(power * tweakable * elapsedSeconds, ResourceBroker.FissionReactor);
		}


		static void ProcessRadioisotopeGenerator(Vessel vessel, PartModule radioisotopeGenerator, ResourceInfo electricCharge, double elapsedSeconds)
		{
			// note: doesn't support easy mode

			double power = Reflection.ReflectionValue<float>(radioisotopeGenerator, "BasePower");
			double halfLife = Reflection.ReflectionValue<float>(radioisotopeGenerator, "HalfLife");

			double missionTime = vessel.missionTime / (3600.0 * Time.HoursInDay * Time.DaysInYear);
			double remaining = System.Math.Pow(2.0, (-missionTime) / halfLife);

			electricCharge.Produce(power * remaining * elapsedSeconds, ResourceBroker.RTG);
		}


		static void ProcessCryoTank(Vessel vessel, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoPartModule, PartModule cryoTank, VesselResources resources, ResourceInfo electricCharge, double elapsedSeconds)
		{
			// Note. Currently background simulation of Cryotanks has an irregularity in that boiloff of a fuel type in a tank removes resources from all tanks
			// but at least some simulation is better than none ;)

			// get list of fuels, do nothing if no fuels
			IList fuels = Reflection.ReflectionValue<IList>(cryoTank, "fuels");
			if (fuels == null)
				return;

			// is cooling available, note: comparing against amount in previous simulation step
			bool available = Lib.Proto.GetBool(protoPartModule, "CoolingEnabled") && electricCharge.Amount > double.Epsilon;

			// get cooling cost
			double coolingCost = Reflection.ReflectionValue<float>(cryoTank, "CoolingCost");

			string fuelName = "";
			double amount = 0.0;
			double totalCost = 0.0;
			double boiloffRate = 0.0;

			foreach (object fuel in fuels)
			{
				fuelName = Reflection.ReflectionValue<string>(fuel, "fuelName");
				// if fuel_name is null, don't do anything
				if (fuelName == null)
					continue;

				//get fuel resource
				ResourceInfo fuelResource = resources.GetResource(vessel, fuelName);

				// if there is some fuel
				// note: comparing against amount in previous simulation step
				if (fuelResource.Amount > double.Epsilon)
				{
					// Try to find resource "fuel_name" in PartResources
					ProtoPartResourceSnapshot protoFuel = protoPart.resources.Find(x => x.resourceName == fuelName);

					// If part doesn't have the fuel, don't do anything.
					if (protoFuel == null)
						continue;

					// get amount in the part
					amount = protoFuel.amount;

					// if cooling is enabled and there is enough EC
					if (available)
					{
						// calculate ec consumption
						totalCost += coolingCost * amount * 0.001;
					}
					// if cooling is disabled or there wasn't any EC
					else
					{
						// get boiloff rate per-second
						boiloffRate = Reflection.ReflectionValue<float>(fuel, "boiloffRate") / 360000.0f;

						// let it boil off
						fuelResource.Consume(amount * (1.0 - System.Math.Pow(1.0 - boiloffRate, elapsedSeconds)), ResourceBroker.Boiloff);
					}
				}
			}
			// apply EC consumption
			electricCharge.Consume(totalCost * elapsedSeconds, ResourceBroker.Cryotank);
		}

		// TODO : this is to migrate pre-3.1 saves using WarpFixer to the new SolarPanelFixer. At some point in the future we can remove this code.
		static void MigrateWarpFixer(Vessel vessel, Part part, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoPartModule)
		{
			ModuleDeployableSolarPanel panelModule = part.FindModuleImplementing<ModuleDeployableSolarPanel>();
			ProtoPartModuleSnapshot protoPanelModule = protoPart.modules.Find(x => x.moduleName == "ModuleDeployableSolarPanel");

			if (panelModule == null || protoPanelModule == null)
			{
				Logging.Log("Vessel " + vessel.name + " has solar panels that can't be converted automatically following Kerbalism 3.1 update. Load it to fix the issue.");
				return;
			}

			SolarPanelFixer.PanelState state = SolarPanelFixer.PanelState.Unknown;
			string panelStateString = Lib.Proto.GetString(protoPanelModule, "deployState");

			if (!Enum.IsDefined(typeof(ModuleDeployablePart.DeployState), panelStateString))
				return;

			ModuleDeployablePart.DeployState panelState = (ModuleDeployablePart.DeployState)Enum.Parse(typeof(ModuleDeployablePart.DeployState), panelStateString);

			if (panelState == ModuleDeployablePart.DeployState.BROKEN)
			{
				state = SolarPanelFixer.PanelState.Broken;
			}
			else if (!panelModule.isTracking)
			{
				state = SolarPanelFixer.PanelState.Static;
			}
			else
			{
				switch (panelState)
				{
					case ModuleDeployablePart.DeployState.EXTENDED:
						if (!panelModule.retractable)
							state = SolarPanelFixer.PanelState.ExtendedFixed;
						else
							state = SolarPanelFixer.PanelState.Extended;
						break;

					case ModuleDeployablePart.DeployState.RETRACTED:
						state = SolarPanelFixer.PanelState.Retracted;
						break;

					case ModuleDeployablePart.DeployState.RETRACTING:
						state = SolarPanelFixer.PanelState.Retracting;
						break;

					case ModuleDeployablePart.DeployState.EXTENDING:
						state = SolarPanelFixer.PanelState.Extending;
						break;

					default:
						state = SolarPanelFixer.PanelState.Unknown;
						break;
				}
			}
			protoPartModule.moduleName = "SolarPanelFixer";
			Lib.Proto.Set(protoPartModule, "state", state);
			Lib.Proto.Set(protoPartModule, "persistentFactor", 0.75);
			Lib.Proto.Set(protoPartModule, "launchUT", Planetarium.GetUniversalTime());
			Lib.Proto.Set(protoPartModule, "nominalRate", panelModule.chargeRate);
		}
	}
} // KERBALISM


using System;
using System.Collections.Generic;


namespace KERBALISM
{
	class KerbalismScansat : PartModule
	{
		[KSPField] public string experimentType = string.Empty;
		[KSPField] public double ec_rate = 0.0;

		[KSPField(isPersistant = true)] public int sensorType = 0;
		[KSPField(isPersistant = true)] public string body_name = string.Empty;
		[KSPField(isPersistant = true)] public double body_coverage = 0.0;
		[KSPField(isPersistant = true)] public double warp_buffer = 0.0;


		PartModule scanner = null;
		ExperimentInfo expInfo;
		bool IsScanning { get; set; }

		public override void OnStart(StartState state)
		{
			if (GameLogic.DisableScenario(this)) return;
			if (GameLogic.IsEditor()) return;

			foreach(var module in part.Modules)
			{
				if(module.moduleName == "SCANsat" || module.moduleName == "ModuleSCANresourceScanner")
				{
					scanner = module;
					break;
				}
			}

			if (scanner == null) return;
			sensorType = Reflection.ReflectionValue<int>(scanner, "sensorType");
			expInfo = ScienceDB.GetExperimentInfo(experimentType);
		}

		void FixedUpdate()
		{
			if (scanner == null) return;
			if (!Features.Science) return;

			IsScanning = SCANsat.IsScanning(scanner);
			double new_coverage = SCANsat.Coverage(sensorType, vessel.mainBody);

			if(body_name == vessel.mainBody.name && new_coverage < body_coverage)
			{
				// SCANsat sometimes reports a coverage of 0, which is wrong
				new_coverage = body_coverage;
			}

			if (vessel.mainBody.name != body_name)
			{
				body_name = vessel.mainBody.name;
				body_coverage = new_coverage;
			}
			else
			{
				double coverage_delta = new_coverage - body_coverage;
				body_coverage = new_coverage;
				VesselData vd = vessel.KerbalismData();

				if (IsScanning)
				{
					Situation scanSatSituation = new Situation(vessel.mainBody.flightGlobalsIndex, ScienceSituation.InSpaceHigh);
					SubjectData subject = ScienceDB.GetSubjectData(expInfo, scanSatSituation);
					if (subject == null)
						return;

					double size = expInfo.DataSize * coverage_delta / 100.0; // coverage is 0-100%
					size += warp_buffer;
					size = Drive.StoreFile(vessel, subject, size);
					if (size > double.Epsilon)
					{
						// we filled all drives up to the brim but were unable to store everything
						if (warp_buffer < double.Epsilon)
						{
							// warp buffer is empty, so lets store the rest there
							warp_buffer = size;
							size = 0;
						}
						else
						{
							// warp buffer not empty. that's ok if we didn't get new data
							if (coverage_delta < double.Epsilon)
							{
								size = 0;
							}
							// else we're scanning too fast. stop.
						}

						// cancel scanning and annoy the user
						if (size > double.Epsilon)
						{
							warp_buffer = 0;
							StopScan();
							vd.scansatID.Add(part.flightID);
							Message.Post(String.Color(Local.Scansat_Scannerhalted, String.Kolor.Red, true), Local.Scansat_Scannerhalted_text.Format("<b>" + vessel.vesselName + "</b>"));//"Scanner halted""Scanner halted on <<1>>. No storage left on vessel."
						}
					}
				}
				else if(vd.scansatID.Contains(part.flightID))
				{
					
					if (vd.DrivesFreeSpace / vd.DrivesCapacity > 0.9) // restart when 90% of capacity is available 
					{
						StartScan();
						vd.scansatID.Remove(part.flightID);
						if (vd.configElectricCharge) Message.Post(Local.Scansat_sensorresumed.Format("<b>" + vessel.vesselName + "</b>"));//String.BuildString("SCANsat sensor resumed operations on <<1>>)
					}
				}
			}
		}

		void StopScan()
		{
			if (scanner == null) return;
			SCANsat.StopScan(scanner);
			IsScanning = SCANsat.IsScanning(scanner);
		}

		void StartScan()
		{
			if (scanner == null) return;
			SCANsat.StartScan(scanner);
			IsScanning = SCANsat.IsScanning(scanner);
		}

		internal static void BackgroundUpdate(Vessel vessel, ProtoPartSnapshot p, ProtoPartModuleSnapshot m, KerbalismScansat kerbalismScansat,
		                                    Part part_prefab, VesselData vd, ResourceInfo ec, double elapsed_s)
		{
			List<ProtoPartModuleSnapshot> scanners = Cache.VesselObjectsCache<List<ProtoPartModuleSnapshot>>(vessel, "scansat_" + p.flightID);
			if(scanners == null)
			{
				scanners = Lib.FindModules(p, "SCANsat");
				if (scanners.Count == 0) scanners = Lib.FindModules(p, "ModuleSCANresourceScanner");
				Cache.SetVesselObjectsCache(vessel, "scansat_" + p.flightID, scanners);
			}

			if (scanners.Count == 0) return;
			var scanner = scanners[0];

			bool is_scanning = Lib.Proto.GetBool(scanner, "scanning");
			if(is_scanning && kerbalismScansat.ec_rate > double.Epsilon)
				ec.Consume(kerbalismScansat.ec_rate * elapsed_s, ResourceBroker.Scanner);

			if (!Features.Science)
			{
				if(is_scanning && ec.Amount < double.Epsilon)
				{					
					SCANsat.StopScanner(vessel, scanner, part_prefab);
					is_scanning = false;

					// remember disabled scanner
					vd.scansatID.Add(p.flightID);

					// give the user some feedback
					if (vd.configElectricCharge) Message.Post(Local.Scansat_sensordisabled.Format("<b>"+vessel.vesselName+"</b>"));//String.BuildString("SCANsat sensor was disabled on <<1>>)
				}
				else if (vd.scansatID.Contains(p.flightID))
				{
					// if there is enough ec
					// note: comparing against amount in previous simulation step
					// re-enable at 25% EC
					if (ec.Level > 0.25)
					{
						// re-enable the scanner
						SCANsat.ResumeScanner(vessel, m, part_prefab);
						is_scanning = true;

						// give the user some feedback
						if (vd.configElectricCharge) Message.Post(Local.Scansat_sensorresumed.Format("<b>"+vessel.vesselName+"</b>"));//String.BuildString("SCANsat sensor resumed operations on <<1>>)
					}
				}

				// forget active scanners
				if (is_scanning) vd.scansatID.Remove(p.flightID);

				return;
			} // if(!Feature.Science)

			string body_name = Lib.Proto.GetString(m, "body_name");
			int sensorType = (int)Lib.Proto.GetUInt(m, "sensorType");
			double body_coverage = Lib.Proto.GetDouble(m, "body_coverage");
			double warp_buffer = Lib.Proto.GetDouble(m, "warp_buffer");

			double new_coverage = SCANsat.Coverage(sensorType, vessel.mainBody);

			if (body_name == vessel.mainBody.name && new_coverage < body_coverage)
			{
				// SCANsat sometimes reports a coverage of 0, which is wrong
				new_coverage = body_coverage;
			}

			if (vessel.mainBody.name != body_name)
			{
				body_name = vessel.mainBody.name;
				body_coverage = new_coverage;
			}
			else
			{
				double coverage_delta = new_coverage - body_coverage;
				body_coverage = new_coverage;

				if (is_scanning)
				{
					ExperimentInfo expInfo = ScienceDB.GetExperimentInfo(kerbalismScansat.experimentType);
					SubjectData subject = ScienceDB.GetSubjectData(expInfo, vd.VesselSituations.GetExperimentSituation(expInfo));
					if (subject == null)
						return;

					double size = expInfo.DataSize * coverage_delta / 100.0; // coverage is 0-100%
					size += warp_buffer;

					if (size > double.Epsilon)
					{
						// store what we can
						foreach (var d in Drive.GetDrives(vd))
						{
							var available = d.FileCapacityAvailable();
							var chunk = System.Math.Min(size, available);
							if (!d.Record_file(subject, chunk, true))
								break;
							size -= chunk;

							if (size < double.Epsilon)
								break;
						}
					}

					if (size > double.Epsilon)
					{
						// we filled all drives up to the brim but were unable to store everything
						if (warp_buffer < double.Epsilon)
						{
							// warp buffer is empty, so lets store the rest there
							warp_buffer = size;
							size = 0;
						}
						else
						{
							// warp buffer not empty. that's ok if we didn't get new data
							if (coverage_delta < double.Epsilon)
							{
								size = 0;
							}
							// else we're scanning too fast. stop.
						}
					}

					// we filled all drives up to the brim but were unable to store everything
					// cancel scanning and annoy the user
					if (size > double.Epsilon || ec.Amount < double.Epsilon)
					{
						warp_buffer = 0;
						SCANsat.StopScanner(vessel, scanner, part_prefab);
						vd.scansatID.Add(p.flightID);
						if (vd.configElectricCharge) Message.Post(Local.Scansat_sensordisabled.Format("<b>"+vessel.vesselName+"</b>"));//String.BuildString("SCANsat sensor was disabled on <<1>>)
					}
				}
				else if (vd.scansatID.Contains(p.flightID))
				{
					if (ec.Level >= 0.25 && (vd.DrivesFreeSpace / vd.DrivesCapacity > 0.9))
					{
						SCANsat.ResumeScanner(vessel, scanner, part_prefab);
						vd.scansatID.Remove(p.flightID);
						if (vd.configElectricCharge) Message.Post(Local.Scansat_sensorresumed.Format("<b>"+vessel.vesselName+"</b>"));//String.BuildString("SCANsat sensor resumed operations on <<1>>)
					}
				}
			}

			Lib.Proto.Set(m, "warp_buffer", warp_buffer);
			Lib.Proto.Set(m, "body_coverage", body_coverage);
			Lib.Proto.Set(m, "body_name", body_name);
		}
	}
}

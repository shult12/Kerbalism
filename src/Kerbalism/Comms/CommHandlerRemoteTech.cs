﻿using System;
using System.Collections.Generic;

namespace KERBALISM
{
	class CommHandlerRemoteTech : CommHandler
	{
		const double bitsPerMB = 1024.0 * 1024.0 * 8.0;

		double baseRate;

		class UnloadedTransmitter
		{
			internal PartModule prefab;
			internal ProtoPartModuleSnapshot protoTransmitter;

			internal UnloadedTransmitter(PartModule prefab, ProtoPartModuleSnapshot protoTransmitter)
			{
				this.prefab = prefab;
				this.protoTransmitter = protoTransmitter;
			}
		}

		List<UnloadedTransmitter> unloadedTransmitters;
		List<PartModule> loadedTransmitters;

		protected override bool NetworkIsReady => RemoteTech.Enabled;

		protected override void UpdateNetwork(ConnectionInfo connection)
		{
			// if we're NOT connected
			if (!RemoteTech.Connected(vd.VesselID))
			{
				connection.linked = false;

				// is loss of connection due to a blackout
				if (RemoteTech.GetCommsBlackout(vd.VesselID))
					connection.Status = connection.storm ? LinkStatus.storm : LinkStatus.plasma;
				else
					connection.Status = LinkStatus.no_link;

				connection.strength = 0.0;
				connection.rate = 0.0;
				connection.target_name = string.Empty;
				connection.control_path.Clear();
				return;
			}

			connection.linked = RemoteTech.ConnectedToKSC(vd.VesselID);
			connection.Status = RemoteTech.TargetsKSC(vd.VesselID) ? LinkStatus.direct_link : LinkStatus.indirect_link;
			connection.target_name = connection.Status == LinkStatus.direct_link ? String.Ellipsis("DSN: " + (RemoteTech.NameTargetsKSC(vd.VesselID) ?? ""), 20) :
				String.Ellipsis(RemoteTech.NameFirstHopToKSC(vd.VesselID) ?? "", 20);

			Guid[] controlPath = null;
			if (connection.linked) controlPath = RemoteTech.GetCommsControlPath(vd.VesselID);

			// Get the lowest rate in ControlPath
			if (controlPath == null)
			{
				connection.strength = 0.0;
				connection.rate = 0.0;
				connection.control_path.Clear();
			}
			else
			{
				if (controlPath.Length > 0)
				{
					double dist = RemoteTech.GetCommsDistance(vd.VesselID, controlPath[0]);
					double maxDist = RemoteTech.GetCommsMaxDistance(vd.VesselID, controlPath[0]);
					connection.strength = maxDist > 0.0 ? 1.0 - (dist / System.Math.Max(maxDist, 1.0)) : 0.0;
					connection.strength = System.Math.Pow(connection.strength, DataRateDampingExponentRT);

					connection.rate = baseRate * connection.strength;

					// If using relay, get the lowest rate
					if (connection.Status != LinkStatus.direct_link)
					{
						// Check the connection link on the next vessel in the connection chain
						// to find the lowest data rate, this value can be one tick inaccurate depending on order of
						// processing of vessel connections, but is negligible
						Vessel target = FlightGlobals.FindVessel(controlPath[0]);
						target.TryGetVesselDataTemp(out VesselData vd);
						ConnectionInfo ci = vd.Connection;
						connection.rate = System.Math.Min(ci.rate, connection.rate);
					}
				}

				connection.control_path.Clear();
				Guid i = vd.VesselID;
				foreach (Guid id in controlPath)
				{
					double linkDistance = RemoteTech.GetCommsDistance(i, id);
					double linkMaxDistance = RemoteTech.GetCommsMaxDistance(i, id);
					double signalStrength = 1 - (linkDistance / linkMaxDistance);
					signalStrength = System.Math.Pow(signalStrength, DataRateDampingExponentRT);

					string[] controlPoint = new string[3];

					// satellite name
					controlPoint[0] = String.Ellipsis(RemoteTech.GetSatelliteName(i) + " \\ " + RemoteTech.GetSatelliteName(id), 50);
					// signal strength
					controlPoint[1] = HumanReadable.Percentage(System.Math.Ceiling(signalStrength * 10000) / 10000, "F2");
					// tooltip info
					controlPoint[2] = String.BuildString("Distance: ", HumanReadable.Distance(linkDistance),
						" (Max: ", HumanReadable.Distance(linkMaxDistance), ")");
					
					connection.control_path.Add(controlPoint);
					i = id;
				}
			}

			// set minimal data rate to what is defined in Settings (1 bit/s by default) 
			if (connection.rate > 0.0 && connection.rate * bitsPerMB < Settings.DataRateMinimumBitsPerSecond)
				connection.rate = Settings.DataRateMinimumBitsPerSecond / bitsPerMB;
		}

		static double dampingExponent = 0;
		static double DataRateDampingExponentRT
		{
			get
			{
				if (dampingExponent != 0)
					return dampingExponent;

				if (Settings.DampingExponentOverride != 0)
					return Settings.DampingExponentOverride;

				// Since RemoteTech Mission Control KSC maximum range never exceeds 75Mm we can't use exactly the same logic as CommNet.
				// What we do here is take Duna as a reference and pretend that it is Mars.

				// Lets take a look at some real world examples
				// https://www.researchgate.net/figure/Calculation-of-Received-Power-from-space-probes-based-on-online-DSN-data-given-on-May_tbl1_308019760	

				// [ Satellite ]	[ Power output ]	[ Distance from Earth ]		[ Data rate ]
				//	Voyager 1			20W, 47 dBi			134 au						159 bps
				//	Voyager 2			20W, 47 dBi			111 au						160 bps
				//	New Horizons		12W, 42 dBi			35 au						4.21 kbps
				//	Cassini				20W, 46.6 dBi		9 au						22.12 kbps
				//	Dawn				100W, 39.6 dBi		3.5 au						125 kbps
				//	Rosetta				28W, 42.5 dBi		2.8 au						52.42 kbps
				//	SOHO (omni ant)		10W					4.4 Moon distance			245.76 kbps

				// https://mars.nasa.gov/mro/mission/communications/
				// Also, the Mars Reconnaissance Orbiter sends data to Earth, the data rate is about 500 to 4000 kilobits per second.
				// ie. between 62.5 kB/s - 500 kB/s
				// https://en.wikipedia.org/wiki/Mars_Reconnaissance_Orbiter
				// The spacecraft carries two 100-watt X-band amplifiers (one of which is a backup), one 35-watt Ka-band amplifier
				// so, 135 W to transmit?

				// For our estimation, we assume a base reach similar to the Reflectron KR-14 that has
				// similar specs to Mars Reconnaissance Orbiter
				double testRange = 60000000000.0; // 60Gm

				// signal strength at ~ farthest earth - mars distance, Duna is at 2.53 au with stock ksp solar sytem
				double strengthAt2AU = SignalStrength(testRange, 2.53 * Sim.AU);    // 34.4 Gm w stock solar system

				// For our estimation, we assume a base rate similar to the Reflectron KR-14
				double baseRate = 0.4815;

				// At 2 AU, this is the rate we want to get out of it
				double desiredRateAt2AU = 0.05;

				// dataRate = baseRate * (strengthAt2AU ^ exponent)
				// so...
				// exponent = log_strengthAt2AU(dataRate / baseRate)
				dampingExponent = System.Math.Log(desiredRateAt2AU / baseRate, strengthAt2AU);

				// 2.4 seems good for RemoteTech
				return dampingExponent;
			}
		}

		protected override void UpdateTransmitters(ConnectionInfo connection, bool searchTransmitters)
		{
			Vessel v = vd.Vessel;

			if (RemoteTech.Enabled)
			{
				RemoteTech.SetPoweredDown(v.id, !connection.powered);
				RemoteTech.SetCommsBlackout(v.id, connection.storm);
			}

			baseRate = 0.0;
			connection.ec = 0.0;
			connection.ec_idle = 0.0;
			double highestDataRate = double.Epsilon;

			// Since RemoteTech is all about building comm networks by pointing dishes, we need to tweak some things.
			// Before we multiplied all transmitters together and then calculated a kind of an average for the rate,
			// now, we look for the highest transmitter available that is activated on the vessel currently.
			// this one will also determine the additional transmission energy cost with its packetResourceCost value.
			// The best way would be if RemoteTech provided API to get all the antenna partModules that actually make
			// up the current connection chain, then check for the lowest transmission rate, consume energy on all of them etc...
			if (v.loaded)
			{
				if (loadedTransmitters == null)
				{
					loadedTransmitters = new List<PartModule>();
					GetTransmittersLoaded(v);
				}
				else if (searchTransmitters)
				{
					loadedTransmitters.Clear();
					GetTransmittersLoaded(v);
				}

				if (unloadedTransmitters != null)
					unloadedTransmitters = null;

				foreach (PartModule pm in loadedTransmitters)
				{
					// calculate internal (passive) transmitter ec usage @ 0.5W each
					if (pm.moduleName == "ModuleRTAntennaPassive")
					{
						connection.ec_idle += 0.0005;
					}
					// calculate external transmitters
					else
					{
						// only include data rate and ec cost if transmitter is active
						if (Reflection.ReflectionValue<bool>(pm, "IsRTActive"))
						{
							double dataRate = (Reflection.ReflectionValue<float>(pm, "RTPacketSize") / Reflection.ReflectionValue<float>(pm, "RTPacketInterval"));
							connection.ec_idle += RemoteTech.GetModuleRTAntennaConsumption(pm);
							// Transmit data only with the antenna that has highest transmission data rate, and also consume that
							// antennas energy transmission rate, based on the antenna's efficiency in configs
							if (dataRate > highestDataRate)
							{
								highestDataRate = dataRate;
								baseRate = dataRate;
								float packetResourceCost = Reflection.ReflectionValue<float>(pm, "RTPacketResourceCost");
								connection.ec = dataRate * packetResourceCost;
							}
						}
					}
				}
			}
			else
			{
				if (unloadedTransmitters == null)
				{
					unloadedTransmitters = new List<UnloadedTransmitter>();
					GetTransmittersUnloaded(v);
				}
				else if (searchTransmitters)
				{
					unloadedTransmitters.Clear();
					GetTransmittersUnloaded(v);
				}

				if (loadedTransmitters != null)
					loadedTransmitters = null;

				foreach (UnloadedTransmitter mdt in unloadedTransmitters)
				{
					if (mdt.protoTransmitter.moduleName == "ModuleRTAntennaPassive")
					{
						connection.ec_idle += 0.0005;
					}
					else
					{
						if (!Lib.Proto.GetBool(mdt.protoTransmitter, "IsRTActive", false))
							continue;

						float? packet_size = Reflection.SafeReflectionValue<float>(mdt.prefab, "RTPacketSize");
						float? packet_Interval = Reflection.SafeReflectionValue<float>(mdt.prefab, "RTPacketInterval");

						if (packet_size != null && packet_Interval != null)
						{
							double dataRate = (float)packet_size / (float)packet_Interval;
							connection.ec_idle += RemoteTech.GetModuleRTAntennaConsumption(mdt.prefab);
							if(dataRate > highestDataRate)
							{
								float? packetResourceCost = Reflection.SafeReflectionValue<float>(mdt.prefab, "RTPacketResourceCost");
								if(packetResourceCost != null)
								{
									highestDataRate = dataRate;
									baseRate = dataRate;
									connection.ec = dataRate * (float)packetResourceCost;
								}
							}
						}
					}
				}
			}

			// Apply Antenna Speed value from ksp in game settings
			baseRate *= PreferencesScience.Instance.transmitFactor;

			// With RemoteTech it is very common to build satellites with 3 or more relay antennas becuase of the fact the they
			// can only point in a certain direction, usually only covering a single target with their small cone of vision.
			// The compensation that RemoteTech users see is that many of the later dishes can reach longer than CommNet counterparts
			// at the cost of a much higher energy consumption for even establishing connection without transferring data.
			// I think it would be too much to add the "transmission energy" consumption of all the active antennas on the vessel even though
			// there will only be one of these antennas that actually is transmitting the data. So find the one with the highest data rate
			// and apply the transmissionCost per package for that one.

			// This is what RemoteTech users are used to, high energy costs just to have antenna running even without transferring data
			connection.ec_idle *= Settings.TransmitterPassiveEcFactorRT; // apply passive factor to "internal" antennas always-consumed rate
			connection.ec += connection.ec_idle;
			connection.ec *= Settings.TransmitterActiveEcFactorRT;

			connection.hasActiveAntenna = connection.ec_idle > 0.0;
		}

		internal override double GetTransmissionCost(double transmittedTotal, double elapsed_s)
		{
			// I think a better model to calculate transmission cost with RemoteTech is to factor in the distance of
			// transmission. An antenna wouldn't have to "scream" as loud if the recieving satellite or station were close.

			// The Connection.strength value is already prepared with the correct value to the next satellite in the chain at this point
			// Better connection strength means cheaper transmission energy cost, 5% at minimum
			double strength = vd.Connection.strength < 0.95 ? vd.Connection.strength : 0.95;
			return ((vd.Connection.ec - vd.Connection.ec_idle) * (1 - strength)) * (transmittedTotal / (vd.Connection.rate * elapsed_s));
		}

		void GetTransmittersLoaded(Vessel v)
		{
			foreach (Part p in v.parts)
			{
				foreach (PartModule pm in p.Modules)
				{
					if (pm.moduleName == "ModuleRTAntennaPassive")
					{
						loadedTransmitters.Add(pm);
					}
					else if (pm.moduleName == "ModuleRTAntenna")
					{
						pm.Events["EventTransmit"].active = false;
						pm.resHandler.inputResources.Clear(); // we handle consumption by ourselves
						loadedTransmitters.Add(pm);
					}
				}
			}
		}

		void GetTransmittersUnloaded(Vessel v)
		{
			foreach (ProtoPartSnapshot pps in v.protoVessel.protoPartSnapshots)
			{
				// get part prefab (required for module properties)
				Part part_prefab = pps.partInfo.partPrefab;

				for (int i = 0; i < part_prefab.Modules.Count; i++)
				{
					if (part_prefab.Modules[i].moduleName == "ModuleRTAntenna")
					{
						ProtoPartModuleSnapshot ppms;
						if (i < pps.modules.Count && pps.modules[i].moduleName == "ModuleRTAntenna")
						{
							ppms = pps.modules[i];
						}
						// fallback in case the module indexes are messed up
						else
						{
							ppms = pps.FindModule("ModuleRTAntenna");
							Logging.LogDebug($"Could not find a ModuleRTAntenna at index {i} on part {pps.partName} on vessel {v.protoVessel.vesselName}", Logging.LogLevel.Warning);
						}

						if (ppms != null)
						{
							unloadedTransmitters.Add(new UnloadedTransmitter(part_prefab.Modules[i], ppms));
						}
					}
					else if (part_prefab.Modules[i].moduleName == "ModuleRTAntennaPassive")
					{
						ProtoPartModuleSnapshot ppms;
						if (i < pps.modules.Count && pps.modules[i].moduleName == "ModuleRTAntennaPassive")
						{
							ppms = pps.modules[i];
						}
						// fallback in case the module indexes are messed up
						else
						{
							ppms = pps.FindModule("ModuleRTAntennaPassive");
							Logging.LogDebug($"Could not find a ModuleRTAntennaPassive at index {i} on part {pps.partName} on vessel {v.protoVessel.vesselName}", Logging.LogLevel.Warning);
						}

						if (ppms != null)
						{
							unloadedTransmitters.Add(new UnloadedTransmitter(part_prefab.Modules[i], ppms));
						}
					}
				}
			}
		}

	}
}

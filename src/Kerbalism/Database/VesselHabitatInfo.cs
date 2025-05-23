using System;
using System.Collections.Generic;
using UnityEngine;

namespace KERBALISM
{
	class SunShieldingPartData
	{
		internal double distance = 1.0;
		internal double thickness = 1.0;

		internal SunShieldingPartData(double distance, double thickness)
		{
			this.distance = distance;
			this.thickness = thickness;
		}

		internal SunShieldingPartData(ConfigNode node)
		{
			distance = Lib.ConfigValue<double>(node, "distance", 1.0);
			thickness = Lib.ConfigValue<double>(node, "thickness", 1.0);
		}

		internal void Save(ConfigNode node)
		{
			node.AddValue("distance", distance);
			node.AddValue("thickness", thickness);
		}
	}

	class VesselHabitatInfo
	{
		Dictionary<uint, List<SunShieldingPartData>> habitatShieldings = new Dictionary<uint, List<SunShieldingPartData>>();
		int habitatIndex = -1;
		List<Habitat> habitats;

		internal double MaxPressure => maxPressure; double maxPressure;

		internal VesselHabitatInfo(ConfigNode node)
		{
			if (node == null)
				return;

			maxPressure = Lib.ConfigValue(node, "maxPressure", 1.0);

			if (!node.HasNode("sspi")) return;
			foreach (var n in node.GetNode("sspi").GetNodes())
			{
				uint partId = uint.Parse(n.name);
				List<SunShieldingPartData> sspiList = new List<SunShieldingPartData>();
				foreach (var p in n.GetNodes())
				{
					sspiList.Add(new SunShieldingPartData(p));
				}
				habitatShieldings[partId] = sspiList;
			}
		}

		internal void Save(ConfigNode node)
		{
			node.AddValue("maxPressure", maxPressure);

			var sspiNode = node.AddNode("sspi");
			foreach (var entry in habitatShieldings)
			{
				var n = sspiNode.AddNode(entry.Key.ToString());
				for (var i = 0; i < entry.Value.Count; i++)
				{
					entry.Value[i].Save(n.AddNode(i.ToString()));
				}
			}
		}

		void RaytraceToSun(Part habitat)
		{
			if (!Features.Radiation) return;

			var habitatPosition = habitat.transform.position;
			var vd = habitat.vessel.KerbalismData();
			List<SunShieldingPartData> sunShieldingParts = new List<SunShieldingPartData>();

			Ray r = new Ray(habitatPosition, vd.EnvironmentMainSun.Direction);
			var hits = Physics.RaycastAll(r, 200);

			foreach (var hit in hits)
			{
				if (hit.collider != null && hit.collider.gameObject != null)
				{
					Part blockingPart = Part.GetComponentUpwards<Part>(hit.collider.gameObject);
					if (blockingPart == null || blockingPart == habitat) continue;
					var mass = blockingPart.mass + blockingPart.GetResourceMass();
					mass *= 1000; // KSP masses are in tons

					// divide part mass by the mass of aluminium (2699 kg/m³), cubic root of that
					// gives a very rough approximation of the thickness, assuming it's a cube.
					// So a 40.000 kg fuel tank would be equivalent to 2.45m aluminium.

					var thickness = System.Math.Pow(mass / 2699.0, 1.0 / 3.0);

					sunShieldingParts.Add(new SunShieldingPartData(hit.distance, thickness));
				}
			}

			// sort by distance, in reverse
			sunShieldingParts.Sort((a, b) => b.distance.CompareTo(a.distance));

			habitatShieldings[habitat.flightID] = sunShieldingParts;
		}

		internal double AverageHabitatRadiation(double radiation)
		{
			if (habitatShieldings.Count < 1) return radiation;

			var result = 0.0;

			foreach (var shieldingPartsList in habitatShieldings.Values)
			{
				var remainingRadiation = radiation;

				foreach (var shieldingInfo in shieldingPartsList)
				{
					// for a 500 keV gamma ray, halfing thickness for aluminium is 3.05cm. But...
					// Solar energetic particles (SEP) are high-energy particles coming from the Sun.
					// They consist of protons, electrons and HZE ions with energy ranging from a few tens of keV
					// to many GeV (the fastest particles can approach the speed of light, as in a
					// "ground-level event"). This is why they are such a big problem for interplanetary space travel.

					// Beer-Lambert law: Remaining radiation = radiation * e^-ux.  Not exact for SEP, but close enough to loosely fit observed curves.
					// linear attenuation coefficent u.  Asssuming an average CME event energy Al shielding 10 ~= 30 g/cm^2.
					// Averaged from NASA plots of large CME events vs Al shielding projections.
					var linearAttenuation = 10; 

					// However, what you lose in particle radiation you gain in gamma radiation (Bremsstrahlung)

					var incomingRadiation = remainingRadiation;
					remainingRadiation *= System.Math.Exp(shieldingInfo.thickness * linearAttenuation * -1);
					var bremsstrahlung = incomingRadiation - remainingRadiation;

					result += Radiation.DistanceRadiation(bremsstrahlung, System.Math.Max(1,shieldingInfo.distance)) / 10; //Gamma radiation has 1/10 the quality factor of SEP
				}

				result += remainingRadiation;
			}

			return result / habitatShieldings.Count;
		}

		internal void Update(Vessel v)
		{
			if (v == null || !v.loaded) return;

			if (habitats == null)
			{
				habitats = Lib.FindModules<Habitat>(v);
				maxPressure = 1.0;
				foreach (var habitat in habitats)
					maxPressure = System.Math.Min(maxPressure, habitat.max_pressure);
			}

			if (!Features.Radiation || habitats.Count == 0)
				return;

			// always do EVAs
			if (v.isEVA)
			{
				RaytraceToSun(v.rootPart);
				return;
			}
				
			if (habitatIndex < 0)
			{
				// first run, do them all
				foreach (var habitat in habitats)
					RaytraceToSun(habitat.part);
				habitatIndex = 0;
			}
			else
			{
				// only do one habitat at a time to preserve some performance
				// and check that part still exists
				if (habitats[habitatIndex].part == null)
					habitats.RemoveAt(habitatIndex);
				else
					RaytraceToSun(habitats[habitatIndex].part);

				habitatIndex = (habitatIndex + 1) % habitats.Count;
			}
		}
	}
}

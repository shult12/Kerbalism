﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace KERBALISM
{
	public class SunShieldingPartInfo
	{
		public double distance;
		public double thickness;

		public SunShieldingPartInfo(double distance, double thickness)
		{
			this.distance = distance;
			this.thickness = thickness;
		}

		public SunShieldingPartInfo(ConfigNode node)
		{
			distance = Lib.ConfigValue<double>(node, "distance", 1.0);
			thickness = Lib.ConfigValue<double>(node, "thickness", 1.0);
		}

		public void Save(ConfigNode node)
		{
			node.SetValue("distance", distance);
			node.SetValue("thickness", thickness);
		}
	}

	public class SunShieldingInfo
	{
		public Dictionary<uint, List<SunShieldingPartInfo>> habitatShieldings = new Dictionary<uint, List<SunShieldingPartInfo>>();

		public SunShieldingInfo()
		{
			// default constructor
		}

		public SunShieldingInfo(ConfigNode node)
		{
			/*
			if (!node.HasNode("sspi")) return;


			foreach(var n in node.GetNode("sspi").GetNodes())
			{

				uint partId = uint.Parse(n.name);
				n.

			}
			*/
		}

		internal void Save(ConfigNode node)
		{
			/*
			var parts_node = node.AddNode("sspi");
			foreach(var entry in habitatShieldings)
			{
				var n = parts_node.AddNode(entry.Key.ToString());
				foreach(var i in entry.Value)
				{
					n.GetValues()
					i.Save(n.AddNode()
				}
				entry.Value.Save(parts_node.AddNode(entry.Key.ToString());
			}
			*/
		}

		public void Add(Part habitat)
		{
			var habitatPosition = habitat.transform.position;
			var vd = habitat.vessel.KerbalismData();
			List<SunShieldingPartInfo> sunShieldingParts = new List<SunShieldingPartInfo>();

			Ray r = new Ray(habitatPosition, vd.EnvMainSun.Direction);
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

					var thickness = Math.Pow(mass / 2699.0, 1.0 / 3.0);

					sunShieldingParts.Add(new SunShieldingPartInfo(hit.distance, thickness));
				}
			}

			// sort by distance, in reverse
			sunShieldingParts.Sort((a, b) => b.distance.CompareTo(a.distance));

			habitatShieldings[habitat.flightID] = sunShieldingParts;
		}


		public double AverageHabitatRadiation(double radiation)
		{
			if (habitatShieldings.Count < 1) return radiation;

			var result = 0.0;

			foreach(var shieldingInfos in habitatShieldings.Values)
			{
				var remainingRadiation = radiation;

				foreach(var shieldingInfo in shieldingInfos)
				{
					// for a 500 keV gamma ray, halfing thickness for aluminium is 3.05cm. But...
					// Solar energetic particles (SEP) are high-energy particles coming from the Sun.
					// They consist of protons, electrons and HZE ions with energy ranging from a few tens of keV
					// to many GeV (the fastest particles can approach the speed of light, as in a
					// "ground-level event"). This is why they are such a big problem for interplanetary space travel.

					// We assume a 1m halfing thickness for that kind of ionized radiation.

					// halfing factor h = part thickness / halfing thickness
					// remaining radiation = radiation / (2^h)
					// However, what you loose in particle radiation you gain in gamma radiation (Bremsstrahlung)

					var bremsstrahlung = remainingRadiation / Math.Pow(2, shieldingInfo.thickness);
					remainingRadiation -= bremsstrahlung;

					result += Radiation.DistanceFactor(bremsstrahlung, shieldingInfo.distance);
				}

				result += remainingRadiation;
			}

			return result / habitatShieldings.Count;
		}

		internal void Update(Vessel v)
		{
			if (!v.loaded) return;

			var vd = v.KerbalismData();
			habitatShieldings.Clear();

			var habitats = Lib.FindModules<Habitat>(v);

			foreach (var habitat in habitats)
				Add(habitat.part);

			if (v.isEVA)
				Add(v.rootPart);
		}
	}
}
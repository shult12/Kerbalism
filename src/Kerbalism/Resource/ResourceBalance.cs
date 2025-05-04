using System;

namespace KERBALISM
{
	// equalize/vent a vessel
	static class ResourceBalance
	{
		// This Method has a lot of "For\Foreach" because it was design for multi resources
		// Method don't count disabled habitats
		internal static void Equalizer(Vessel v)
		{
			// get resource level in habitats
			double[] res_level = new double[resourceName.Length];                   // Don't count Manned or Depressiong habitats

			// Total resource in parts not disabled
			double[] totalAmount = new double[resourceName.Length];
			double[] maxAmount = new double[resourceName.Length];

			// Total resource in Enabled parts (No crew)
			double[] totalE = new double[resourceName.Length];
			double[] maxE = new double[resourceName.Length];

			// Total resource in Manned parts (Priority!)
			double[] totalP = new double[resourceName.Length];
			double[] maxP = new double[resourceName.Length];

			// Total resource in Depressurizing
			double[] totalD = new double[resourceName.Length];
			double[] maxD = new double[resourceName.Length];

			// amount to equalize speed
			double[] amount = new double[resourceName.Length];

			// Can be positive or negative, controlling the resource flow
			double flowController;

			bool[] mannedisPriority = new bool[resourceName.Length];                // The resource is priority
			bool equalize = false;                                                  // Has any resource that needs to be equalized

			// intial value
			for (int i = 0; i < resourceName.Length; i++)
			{
				totalAmount[i] = new ResourceInfo(v, resourceName[i]).Rate;        // Get generate rate for each resource
				maxAmount[i] = 0;

				totalE[i] = 0;
				maxE[i] = 0;

				totalP[i] = 0;
				maxP[i] = 0;

				totalD[i] = 0;
				maxD[i] = 0;

				mannedisPriority[i] = false;
			}

			foreach (Habitat partHabitat in v.FindPartModulesImplementing<Habitat>())
			{
				// Skip disabled habitats
				if (partHabitat.state != Habitat.State.disabled)
				{
					// Has flag to be Equalized?
					equalize |= partHabitat.needEqualize;

					PartResource[] resources = new PartResource[resourceName.Length];
					for (int i = 0; i < resourceName.Length; i++)
					{
						if (partHabitat.part.Resources.Contains(resourceName[i]))
						{
							PartResource t = partHabitat.part.Resources[resourceName[i]];

							// Manned Amounts
							if (Lib.IsCrewed(partHabitat.part))
							{
								totalP[i] += t.amount;
								maxP[i] += t.maxAmount;
							}
							// Amount for Depressurizing
							else if (partHabitat.state == Habitat.State.depressurizing)
							{
								totalD[i] += t.amount;
								maxD[i] += t.maxAmount;
							}
							else
							{
								totalE[i] += t.amount;
								maxE[i] += t.maxAmount;
							}
							totalAmount[i] += t.amount;
							maxAmount[i] += t.maxAmount;
						}
					}
				}
			}

			if (!equalize) return;

			for (int i = 0; i < resourceName.Length; i++)
			{
				// resource level for Enabled habitats no Manned
				res_level[i] = totalE[i] / (maxAmount[i] - maxP[i]);

				// Manned is priority?
				// If resource amount is less then maxAmount in manned habitat and it's flagged to equalize, define as priority
				// Using Atmosphere, N2, O2 as Priority trigger (we don't want to use CO2 as a trigger)
				if (resourceName[i] != "WasteAtmosphere" && equalize)
				{
					mannedisPriority[i] = maxP[i] - totalP[i] > 0;
				}

				// determine generic equalization speed	per resource
				if (mannedisPriority[i])
					amount[i] = maxAmount[i] * equalize_speed * Kerbalism.elapsed_s;
				else
					amount[i] = (maxE[i] + maxD[i]) * equalize_speed * Kerbalism.elapsed_s;
			}

			if (equalize)
			{
				foreach (Habitat partHabitat in v.FindPartModulesImplementing<Habitat>())
				{
					bool stillNeed = false;
					if (partHabitat.state != Habitat.State.disabled)
					{
						for (int i = 0; i < resourceName.Length; i++)
						{
							if (partHabitat.part.Resources.Contains(resourceName[i]))
							{
								PartResource t = partHabitat.part.Resources[resourceName[i]];
								flowController = 0;

								// Conditions in order
								// If perctToMax = 0 (means Habitat will have 0% of amount:
								//	1 case: modules still needs to be equalized
								//	2 case: has depressurizing habitat
								//	3 case: dropping everything into the priority habitats

								if ((System.Math.Abs(res_level[i] - (t.amount / t.maxAmount)) > precision && !Lib.IsCrewed(partHabitat.part))
									|| ((partHabitat.state == Habitat.State.depressurizing
									|| mannedisPriority[i]) && t.amount > double.Epsilon))
								{
									double perctToAll;              // Percent of resource for this habitat related
									double perctRest;               // Percent to fill priority

									perctToAll = t.amount / maxAmount[i];

									double perctToType;
									double perctToMaxType;

									// Percts per Types
									if (Lib.IsCrewed(partHabitat.part))
									{
										perctToType = t.amount / totalP[i];
										perctToMaxType = t.maxAmount / maxP[i];
									}
									else if (partHabitat.state == Habitat.State.depressurizing)
									{
										perctToType = t.amount / totalD[i];
										perctToMaxType = t.maxAmount / maxD[i];
									}
									else
									{
										perctToType = t.amount / totalE[i];
										perctToMaxType = t.maxAmount / maxE[i];
									}

									// Perct from the left resource
									if (totalAmount[i] - maxP[i] <= 0 || partHabitat.state == Habitat.State.depressurizing)
									{
										perctRest = 0;
									}
									else
									{
										perctRest = (((totalAmount[i] - maxP[i]) * perctToMaxType) - t.amount) / totalE[i];
									}

									// perctToMax < perctToAll ? habitat will send resource : otherwise will receive, flowController == 0 means no flow
									if ((partHabitat.state == Habitat.State.depressurizing || totalAmount[i] - maxP[i] <= 0) && !Lib.IsCrewed(partHabitat.part))
									{
										flowController = 0 - perctToType;
									}
									else if (mannedisPriority[i] && !Lib.IsCrewed(partHabitat.part))
									{
										flowController = System.Math.Min(perctToMaxType - perctToAll, (t.maxAmount - t.amount) / totalAmount[i]);
									}
									else
									{
										flowController = perctRest;
									}

									// clamp amount to what's available in the hab and what can fit in the part
									double amountAffected;
									if (partHabitat.state == Habitat.State.depressurizing)
									{
										amountAffected = flowController * totalD[i];
									}
									else if (!mannedisPriority[i] || !Lib.IsCrewed(partHabitat.part))
									{
										amountAffected = flowController * totalE[i];
									}
									else
									{
										amountAffected = flowController * totalP[i];
									}

									amountAffected *= equalize_speed;

									amountAffected = System.Math.Sign(amountAffected) >= 0 ? System.Math.Max(System.Math.Sign(amountAffected) * precision, amountAffected) : System.Math.Min(System.Math.Sign(amountAffected) * precision, amountAffected);

									double va = amountAffected < 0.0
										? System.Math.Abs(amountAffected) > t.amount                // If negative, habitat can't send more than it has
										? t.amount * (-1)
										: amountAffected
										: System.Math.Min(amountAffected, t.maxAmount - t.amount);  // if positive, habitat can't receive more than max

									va = Double.IsNaN(va) ? 0.0 : va;

									// consume relative percent of this part
									t.amount += va;

									if (va < double.Epsilon) stillNeed = false;
									else stillNeed = true;
								}
							}
						}
					}

					partHabitat.needEqualize = stillNeed;
				}
			}
		}

		// constants
		const double equalize_speed = 0.01;  // equalization/venting mutiple speed per-second, in proportion to amount

		// Resources to equalize
		internal static string[] resourceName = new string[2] { "Atmosphere", "WasteAtmosphere" };

		// Resources to equalize
		internal static double precision = 0.00001;
	}
}

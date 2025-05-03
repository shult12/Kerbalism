using System;
using UnityEngine;

namespace KERBALISM
{
	sealed class Transformator
	{
		readonly Part part;
		Transform transform;
		readonly string name;

		Quaternion baseAngles;

		float rotationRateGoal;
		float CurrentSpinRate;

		readonly float SpinRate;
		readonly float spinAccel;
		readonly bool rotate_iva;

		internal Transformator(Part p, string transf_name, float SpinRate, float spinAccel, bool iva = true)
		{
			transform = null;
			name = string.Empty;
			part = p;
			rotate_iva = iva;

			if (transf_name.Length > 0)
			{
				//Logging.Log("Looking for : {0}", transf_name);
				transform = p.FindModelTransform(transf_name);
				if (transform != null)
				{
					name = transf_name;
					//Logging.Log("Transform {0} has been found", name);

					this.SpinRate = SpinRate;
					this.spinAccel = spinAccel;
					baseAngles = transform.localRotation;
				}
			}
		}

		internal void Play()
		{
			//Logging.Log("Playing Transformation {0}", name);
			if (transform != null) rotationRateGoal = 1.0f;
		}

		internal void Stop()
		{
			//Logging.Log("Stopping Transformation {0}", name);
			if (transform != null) rotationRateGoal = 0.0f;
		}

		internal void DoSpin()
		{
			CurrentSpinRate = Mathf.MoveTowards(CurrentSpinRate, rotationRateGoal * SpinRate, TimeWarp.deltaTime * spinAccel);
			float spin = Mathf.Clamp(TimeWarp.deltaTime * CurrentSpinRate, -10.0f, 10.0f);
			//Logging.Log("Transform {0} spin rate {1}", name, CurrentSpinRate);
			// Part rotation
			if (transform != null) transform.Rotate(Vector3.forward * spin);

			if(rotate_iva && part.internalModel != null)
			{
				// IVA rotation
				if (part.internalModel != null && part.internalModel.transform != null)
					part.internalModel.transform.Rotate(Vector3.forward * (spin * -1));
			}
		}

		internal bool IsRotating()
		{
			return Math.Abs(CurrentSpinRate) > Math.Abs(float.Epsilon * SpinRate);
		}

		internal bool IsStopping()
		{
			return Math.Abs(rotationRateGoal) <= float.Epsilon;
		}
	}
}

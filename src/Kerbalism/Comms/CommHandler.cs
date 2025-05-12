using System;

namespace KERBALISM
{
	class CommHandler
	{
		static bool CommNetStormPatchApplied = false;

		protected VesselData vd;

		bool transmittersDirty;

		/// <summary>
		/// false while the network isn't initialized or when the transmitter list is not up-to-date
		/// </summary>
		internal bool IsReady => NetworkIsReady && !transmittersDirty;

		/// <summary>
		/// pseudo ctor for getting the right handler type
		/// </summary>
		internal static CommHandler GetHandler(VesselData vd, bool isGroundController)
		{
			CommHandler handler;

			// Note : API CommHandlers may not be registered yet when this is called,
			// but this shouldn't be an issue, as the derived types UpdateTransmitters / UpdateNetwork
			// won't be called anymore once the API handler is registered.
			// This said, this isn't ideal, and it would be cleaner to have a "commHandledByAPI"
			// bool that mods should set once and for all before any vessel exist.

			if (!CommNetStormPatchApplied)
			{
				CommNetStormPatchApplied = true;

				if (API.Comm.handlers.Count == 0 && !RemoteTech.Installed)
				{
					CommHandlerCommNetBase.ApplyHarmonyPatches();
				}
			}

			if (API.Comm.handlers.Count > 0)
			{
				handler = new CommHandler();
				Logging.LogDebug("Created new API CommHandler", Logging.LogLevel.Message);
			}
			else if (RemoteTech.Installed)
			{
				handler = new CommHandlerRemoteTech();
				Logging.LogDebug("Created new CommHandlerRemoteTech", Logging.LogLevel.Message);
			}
			else if (isGroundController)
			{
				handler = new CommHandlerCommNetSerenity();
				Logging.LogDebug("Created new CommHandlerCommNetSerenity", Logging.LogLevel.Message);
			}
			else
			{
				handler = new CommHandlerCommNetVessel();
				Logging.LogDebug("Created new CommHandlerCommNetVessel", Logging.LogLevel.Message);
			}
				
			handler.vd = vd;
			handler.transmittersDirty = true;

			return handler;
		}

		/// <summary> Update the provided Connection </summary>
		internal void UpdateConnection(ConnectionInfo connection)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.CommHandler.UpdateConnection");

			UpdateInputs(connection);

			// Can this ever be anything other than 0? not atm, unless I'm missing something...
			if (API.Comm.handlers.Count == 0)
			{
				if (NetworkIsReady)
				{
					if (transmittersDirty)
					{
						UpdateTransmitters(connection, true);
						transmittersDirty = false;
					}
					else
					{
						UpdateTransmitters(connection, false);
					}

					UpdateNetwork(connection);
				}
			}
			else
			{
				transmittersDirty = false;
				try
				{
					API.Comm.handlers[0].Invoke(null, new object[] { connection, vd.Vessel });
				}
				catch (Exception e)
				{
					Logging.Log("CommInfo handler threw exception " + e.Message + "\n" + e.ToString(), Logging.LogLevel.Error);
				}
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		/// <summary>
		/// Clear and re-find all transmitters partmodules on the vessel.
		/// Must be called when parts have been removed / added on the vessel.
		/// </summary>
		internal void ResetPartTransmitters() => transmittersDirty = true;

		/// <summary>
		/// Get the cost for transmitting data with this CommHandler
		/// </summary>
		/// <param name="transmittedTotal">Amount of the total capacity of data that can be sent</param>
		/// <param name="elapsed_s"></param>
		/// <returns></returns>
		internal virtual double GetTransmissionCost(double transmittedTotal, double elapsed_s)
		{
			return (vd.Connection.ec - vd.Connection.ec_idle) * (transmittedTotal / (vd.Connection.rate * elapsed_s));
		}

		/// <summary>
		/// update the fields that can be used as an input by API handlers
		/// </summary>
		protected virtual void UpdateInputs(ConnectionInfo connection)
		{
			connection.transmitting = vd.filesTransmitted.Count > 0;
			connection.storm = vd.EnvStorm;
			connection.powered = vd.Powered;
		}

		protected virtual bool NetworkIsReady => true;

		protected virtual void UpdateNetwork(ConnectionInfo connection) { }

		protected virtual void UpdateTransmitters(ConnectionInfo connection, bool searchTransmitters) { }

		protected static double SignalStrength(double maxRange, double distance)
		{
			if (distance > maxRange)
				return 0.0;

			double relativeDistance = 1.0 - (distance / maxRange);
			double strength = (3.0 - (2.0 * relativeDistance)) * (relativeDistance * relativeDistance);

			if (strength < 0)
				return 0.0;

			return strength;
		}
	}
}

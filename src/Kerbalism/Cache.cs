using System;
using System.Collections.Generic;

namespace KERBALISM
{
	static class Cache
	{
		// caches
		static Dictionary<Guid, Dictionary<string, object>> vesselObjects;


		internal static void Init() => vesselObjects = new Dictionary<Guid, Dictionary<string, object>>();

        internal static void Clear() => vesselObjects.Clear();

		/// <summary>
		/// Called whenever a vessel changes and/or should be updated for various reasons.
		/// Purge the anonymous cache and science transmission cache
		/// </summary>
        internal static void PurgeVesselCaches(Vessel vessel)
		{
			Guid id = Lib.VesselID(vessel);
			vesselObjects.Remove(id);
		}

		/// <summary>
		/// Called whenever a vessel changes and/or should be updated for various reasons.
		/// Purge the anonymous cache and science transmission cache
		/// </summary>
		internal static void PurgeVesselCaches(ProtoVessel protoVessel)
		{
			Guid id = Lib.VesselID(protoVessel);
			vesselObjects.Remove(id);
		}

		/// <summary>
		/// Called when the game state has changed (savegame loads), must reset all non-persisted data that won't be loaded from DB
		/// Purge all anonymous caches, science transmission caches, experiments cached data, and log messages
		/// </summary>
		static void PurgeAllCaches()
		{
			vesselObjects.Clear();
			Message.all_logs.Clear();
		}

        internal static T VesselObjectsCache<T>(Vessel vessel, string key) => VesselObjectsCache<T>(Lib.VesselID(vessel), key);

        internal static T VesselObjectsCache<T>(ProtoVessel vessel, string key) => VesselObjectsCache<T>(Lib.VesselID(vessel), key);

		static T VesselObjectsCache<T>(Guid id, string key)
		{
			if (!vesselObjects.ContainsKey(id))
				return default;

			Dictionary<string, object> dictionary = vesselObjects[id];
			if (dictionary == null)
				return default;

			if (!dictionary.ContainsKey(key))
				return default;

			return (T)dictionary[key];
		}

        internal static void SetVesselObjectsCache<TValue>(Vessel vessel, string key, TValue value) => SetVesselObjectsCache(Lib.VesselID(vessel), key, value);

        internal static void SetVesselObjectsCache<TValue>(ProtoVessel protoVessel, string key, TValue value) => SetVesselObjectsCache(Lib.VesselID(protoVessel), key, value);

        static void SetVesselObjectsCache<TValue>(Guid id, string key, TValue value)
		{
			if (!vesselObjects.ContainsKey(id))
				vesselObjects.Add(id, new Dictionary<string, object>());

			Dictionary<string, object> dictionary = vesselObjects[id];
			dictionary.Remove(key);
			dictionary.Add(key, value);
		}

        internal static bool HasVesselObjectsCache(Vessel vessel, string key) => HasVesselObjectsCache(Lib.VesselID(vessel), key);

        internal static bool HasVesselObjectsCache(ProtoVessel protoVessel, string key) => HasVesselObjectsCache(Lib.VesselID(protoVessel), key);

		static bool HasVesselObjectsCache(Guid id, string key)
		{
			if (!vesselObjects.ContainsKey(id))
				return false;

			Dictionary<string, object> dictionary = vesselObjects[id];
			return dictionary.ContainsKey(key);
		}

        internal static void RemoveVesselObjectsCache(Vessel vessel, string key) => RemoveVesselObjectsCache(Lib.VesselID(vessel), key);

        internal static void RemoveVesselObjectsCache(ProtoVessel protoVessel, string key) => RemoveVesselObjectsCache(Lib.VesselID(protoVessel), key);

		static void RemoveVesselObjectsCache(Guid id, string key)
		{
			if (!vesselObjects.ContainsKey(id))
				return;

			Dictionary<string, object> dictionary = vesselObjects[id];
			dictionary.Remove(key);
		}
	}
} // KERBALISM

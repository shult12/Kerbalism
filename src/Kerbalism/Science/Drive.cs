using System;
using System.Collections.Generic;

namespace KERBALISM
{

	sealed class Drive
	{
		internal Drive(string name, double dataCapacity, int sampleCapacity, bool is_private = false)
		{
			this.files = new Dictionary<SubjectData, File>();
			this.samples = new Dictionary<SubjectData, Sample>();
			this.fileSendFlags = new Dictionary<string, bool>();
			this.dataCapacity = dataCapacity;
			this.sampleCapacity = sampleCapacity;
			this.name = name;
			this.is_private = is_private;
		}

		internal Drive(ConfigNode node)
		{
			// parse science  files
			files = new Dictionary<SubjectData, File>();
			if (node.HasNode("files"))
			{
				foreach (var file_node in node.GetNode("files").GetNodes())
				{
					string subject_id = DB.From_safe_key(file_node.name);
					File file = File.Load(subject_id, file_node);
					if (file != null)
					{
						if(files.ContainsKey(file.subjectData))
						{
							Logging.Log("discarding duplicate subject " + file.subjectData, Logging.LogLevel.Warning);
						}
						else
						{
							files.Add(file.subjectData, file);
							file.subjectData.AddDataCollectedInFlight(file.size);
						}
					}
					else
					{
						file = File.LoadOldFormat(subject_id, file_node);
						if (file != null)
						{
							Logging.Log("Drive file load : converted '" + subject_id + "' to new format");
							if(files.ContainsKey(file.subjectData))
							{
								Logging.Log("discarding duplicate converted subject " + file.subjectData, Logging.LogLevel.Warning);
							}
							else
							{
								files.Add(file.subjectData, file);
								file.subjectData.AddDataCollectedInFlight(file.size);
							}
						}
					}
				}
			}

			// parse science samples
			samples = new Dictionary<SubjectData, Sample>();
			if (node.HasNode("samples"))
			{
				foreach (var sample_node in node.GetNode("samples").GetNodes())
				{
					string subject_id = DB.From_safe_key(sample_node.name);
					Sample sample = Sample.Load(subject_id, sample_node);
					if (sample != null)
					{
						samples.Add(sample.subjectData, sample);
						sample.subjectData.AddDataCollectedInFlight(sample.size);
					}
					else
					{
						sample = Sample.LoadOldFormat(subject_id, sample_node);
						if (sample != null)
						{
							Logging.Log("Drive sample load : converted '" + subject_id + "' to new format");
							samples.Add(sample.subjectData, sample);
							sample.subjectData.AddDataCollectedInFlight(sample.size);
						}
					}

				}
			}

			name = Lib.ConfigValue(node, "name", "DRIVE");
			is_private = Lib.ConfigValue(node, "is_private", false);

			// parse capacities. be generous with default values for backwards
			// compatibility (drives had unlimited storage before this)
			dataCapacity = Lib.ConfigValue(node, "dataCapacity", 100000.0);
			sampleCapacity = Lib.ConfigValue(node, "sampleCapacity", 1000);

			fileSendFlags = new Dictionary<string, bool>();
			string fileNames = Lib.ConfigValue(node, "sendFileNames", string.Empty);
			foreach (string fileName in Lib.Tokenize(fileNames, ','))
			{
				Send(fileName, true);
			}
		}

		internal void Save(ConfigNode node)
		{
			// save science files
			var files_node = node.AddNode("files");
			foreach (File file in files.Values)
			{
				file.Save(files_node.AddNode(DB.To_safe_key(file.subjectData.Id)));
			}

			// save science samples
			var samples_node = node.AddNode("samples");
			foreach (Sample	sample in samples.Values)
			{
				sample.Save(samples_node.AddNode(DB.To_safe_key(sample.subjectData.Id)));
			}

			node.AddValue("name", name);
			node.AddValue("is_private", is_private);
			node.AddValue("dataCapacity", dataCapacity);
			node.AddValue("sampleCapacity", sampleCapacity);

			string fileNames = string.Empty;
			foreach (string subjectId in fileSendFlags.Keys)
			{
				if (fileNames.Length > 0) fileNames += ",";
				fileNames += subjectId;
			}
			node.AddValue("sendFileNames", fileNames);
		}



		internal static double StoreFile(Vessel vessel, SubjectData subjectData, double size, bool include_private = false)
		{
			if (size < double.Epsilon)
				return 0;

			// store what we can

			var drives = GetDrives(vessel, include_private);
			drives.Insert(0, vessel.KerbalismData().TransmitBufferDrive);

			foreach (var d in drives)
			{
				var available = d.FileCapacityAvailable();
				var chunk = System.Math.Min(size, available);
				if (!d.Record_file(subjectData, chunk, true))
					break;
				size -= chunk;

				if (size < double.Epsilon)
					break;
			}

			return size;
		}

		// add science data, creating new file or incrementing existing one
		internal bool Record_file(SubjectData subjectData, double amount, bool allowImmediateTransmission = true, bool useStockCrediting = false)
		{
			if (dataCapacity >= 0 && FilesSize() + amount > dataCapacity)
				return false;

			// create new data or get existing one
			File file;
			if (!files.TryGetValue(subjectData, out file))
			{
				file = new File(subjectData, 0.0, useStockCrediting);
				files.Add(subjectData, file);

				if (!allowImmediateTransmission) Send(subjectData.Id, false);
			}

			// increase amount of data stored in the file
			file.size += amount;

			// keep track of data collected
			subjectData.AddDataCollectedInFlight(amount);

			return true;
		}

		internal void Send(string subjectId, bool send)
		{
			if (!fileSendFlags.ContainsKey(subjectId)) fileSendFlags.Add(subjectId, send);
			else fileSendFlags[subjectId] = send;
		}

		internal bool GetFileSend(string subjectId)
		{
			if (!fileSendFlags.ContainsKey(subjectId)) return PreferencesScience.Instance.transmitScience;
			return fileSendFlags[subjectId];
		}

		// add science sample, creating new sample or incrementing existing one
		internal bool Record_sample(SubjectData subjectData, double amount, double mass, bool useStockCrediting = false)
		{
			int currentSampleSlots = SamplesSize();
			if (sampleCapacity >= 0)
			{
				if (!samples.ContainsKey(subjectData) && currentSampleSlots >= sampleCapacity)
				{
					// can't take a new sample if we're already at capacity
					return false;
				}
			}

			Sample sample;
			if (samples.ContainsKey(subjectData) && sampleCapacity >= 0)
			{
				// test if adding the amount to the sample would exceed our capacity
				sample = samples[subjectData];

				int existingSampleSlots = HumanReadable.SampleSizeToSlots(sample.size);
				int newSampleSlots = HumanReadable.SampleSizeToSlots(sample.size + amount);
				if (currentSampleSlots - existingSampleSlots + newSampleSlots > sampleCapacity)
					return false;
			}

			// create new data or get existing one
			if (!samples.TryGetValue(subjectData, out sample))
			{
				sample = new Sample(subjectData, 0.0, useStockCrediting);
				sample.analyze = PreferencesScience.Instance.analyzeSamples;
				samples.Add(subjectData, sample);
			}

			// increase amount of data stored in the sample
			sample.size += amount;
			sample.mass += mass;

			// keep track of data collected
			subjectData.AddDataCollectedInFlight(amount);

			return true;
		}

		// remove science data, deleting the file when it is empty
		internal void Delete_file(SubjectData subjectData, double amount = 0.0)
		{
			// get data
			File file;
			if (files.TryGetValue(subjectData, out file))
			{
				// decrease amount of data stored in the file
				if (amount == 0.0)
					amount = file.size;
				else
					amount = System.Math.Min(amount, file.size);

				file.size -= amount;

				// keep track of data collected
				subjectData.RemoveDataCollectedInFlight(amount);

				// remove file if empty
				if (file.size <= 0.0) files.Remove(subjectData);
			}
		}

		// remove science sample, deleting the sample when it is empty
		internal double Delete_sample(SubjectData subjectData, double amount = 0.0)
		{
			// get data
			Sample sample;
			if (samples.TryGetValue(subjectData, out sample))
			{
				// decrease amount of data stored in the sample
				if (amount == 0.0)
					amount = sample.size;
				else
					amount = System.Math.Min(amount, sample.size);

				double massDelta = sample.mass * amount / sample.size;
				sample.size -= amount;
				sample.mass -= massDelta;

				// keep track of data collected
				subjectData.RemoveDataCollectedInFlight(amount);

				// remove sample if empty
				if (sample.size <= 0.0) samples.Remove(subjectData);

				return massDelta;
			}
			return 0.0;
		}

		// set analyze flag for a sample
		void Analyze(SubjectData subjectData, bool b)
		{
			Sample sample;
			if (samples.TryGetValue(subjectData, out sample))
			{
				sample.analyze = b;
			}
		}

		// move all data to another drive
		internal bool Move(Drive destination, bool moveSamples)
		{
			bool result = true;

			// copy files
			List<SubjectData> filesList = new List<SubjectData>();
			foreach (File file in files.Values)
			{
				double size = System.Math.Min(file.size, destination.FileCapacityAvailable());
				if (destination.Record_file(file.subjectData, size, true, file.useStockCrediting))
				{
					file.size -= size;
					file.subjectData.RemoveDataCollectedInFlight(size);
					if (file.size < double.Epsilon)
					{
						filesList.Add(file.subjectData);
					}
					else
					{
						result = false;
						break;
					}
				}
				else
				{
					result = false;
					break;
				}
			}
			foreach (SubjectData id in filesList) files.Remove(id);

			if (!moveSamples) return result;

			// move samples
			List<SubjectData> samplesList = new List<SubjectData>();
			foreach (Sample sample in samples.Values)
			{
				double size = System.Math.Min(sample.size, destination.SampleCapacityAvailable(sample.subjectData));
				if (size < double.Epsilon)
				{
					result = false;
					break;
				}

				double mass = sample.mass * (sample.size / size);
				if (destination.Record_sample(sample.subjectData, size, mass, sample.useStockCrediting))
				{
					sample.size -= size;
					sample.subjectData.RemoveDataCollectedInFlight(size);
					sample.mass -= mass;

					if (sample.size < double.Epsilon)
					{
						samplesList.Add(sample.subjectData);
					}
					else
					{
						result = false;
						break;
					}
				}
				else
				{
					result = false;
					break;
				}
			}
			foreach (var id in samplesList) samples.Remove(id);

			return result; // true if everything was moved, false otherwise
		}

		internal double FileCapacityAvailable()
		{
			if (dataCapacity < 0) return double.MaxValue;
			return System.Math.Max(dataCapacity - FilesSize(), 0.0); // clamp to 0 due to fp precision in FilesSize()
		}

		internal double FilesSize()
		{
			double amount = 0.0;
			foreach (var p in files)
			{
				amount += p.Value.size;
			}
			return amount;
		}

		internal double SampleCapacityAvailable(SubjectData subject = null)
		{
			if (sampleCapacity < 0) return double.MaxValue;

			double result = HumanReadable.SlotsToSampleSize(sampleCapacity - SamplesSize());
			if (subject != null && samples.ContainsKey(subject))
			{
				int slotsForMyFile = HumanReadable.SampleSizeToSlots(samples[subject].size);
				double amountLostToSlotting = HumanReadable.SlotsToSampleSize(slotsForMyFile) - samples[subject].size;
				result += amountLostToSlotting;
			}
			return result;
		}

		internal int SamplesSize()
		{
			int amount = 0;
			foreach (var p in samples)
			{
				amount += HumanReadable.SampleSizeToSlots(p.Value.size);
			}
			return amount;
		}

		// return size of data stored in Mb (including samples)
		internal string Size()
		{
			var f = FilesSize();
			var s = SamplesSize();
			var result = f > double.Epsilon ? HumanReadable.DataSize(f) : "";
			if (result.Length > 0) result += " ";
			if (s > 0) result += HumanReadable.SampleSize(s);
			return result;
		}

		internal bool Empty()
		{
			return files.Count + samples.Count == 0;
		}

		// transfer data from a vessel to a drive
		internal static bool Transfer(Vessel src, Drive dst, bool samples)
		{
			double dataAmount = 0.0;
			int sampleSlots = 0;
			foreach (var drive in GetDrives(src, true))
			{
				dataAmount += drive.FilesSize();
				sampleSlots += drive.SamplesSize();
			}

			if (dataAmount < double.Epsilon && (sampleSlots == 0 || !samples))
				return true;

			// get drives
			var allSrc = GetDrives(src, true);

			bool allMoved = true;
			foreach (var a in allSrc)
			{
				if (a.Move(dst, samples))
				{
					allMoved = true;
					break;
				}
			}

			return allMoved;
		}

		// transfer data from a drive to a vessel
		internal static bool Transfer(Drive drive, Vessel dst, bool samples)
		{
			double dataAmount = drive.FilesSize();
			int sampleSlots = drive.SamplesSize();

			if (dataAmount < double.Epsilon && (sampleSlots == 0 || !samples))
				return true;

			// get drives
			var allDst = GetDrives(dst);

			bool allMoved = true;
			foreach (var b in allDst)
			{
				if (drive.Move(b, samples))
				{
					allMoved = true;
					break;
				}
			}

			return allMoved;
		}

		// transfer data between two vessels
		internal static void Transfer(Vessel src, Vessel dst, bool samples)
		{
			double dataAmount = 0.0;
			int sampleSlots = 0;
			foreach (var drive in GetDrives(src, true))
			{
				dataAmount += drive.FilesSize();
				sampleSlots += drive.SamplesSize();
			}

			if (dataAmount < double.Epsilon && (sampleSlots == 0 || !samples))
				return;

			var allSrc = GetDrives(src, true);
			bool allMoved = false;
			foreach (var a in allSrc)
			{
				if (Transfer(a, dst, samples))
				{
					allMoved = true;
					break;
				}
			}

			// inform the user
			if (allMoved)
				Message.Post
				(
					HumanReadable.DataSize(dataAmount) + " " + Local.Science_ofdatatransfer,
				 	Lib.BuildString(Local.Generic_FROM, " <b>", src.vesselName, "</b> ", Local.Generic_TO, " <b>", dst.vesselName, "</b>")
				);
			else
				Message.Post
				(
					Lib.Color(Lib.BuildString("WARNING: not evering copied"), Lib.Kolor.Red, true),
					Lib.BuildString(Local.Generic_FROM, " <b>", src.vesselName, "</b> ", Local.Generic_TO, " <b>", dst.vesselName, "</b>")
				);
		}

		/// <summary> delete all files/samples in the drive</summary>
		internal void DeleteDriveData()
		{
			foreach (File file in files.Values)
				file.subjectData.RemoveDataCollectedInFlight(file.size);

			foreach (Sample sample in samples.Values)
				sample.subjectData.RemoveDataCollectedInFlight(sample.size);

			files.Clear();
			samples.Clear();
		}

		/// <summary> delete all files/samples in the vessel drives</summary>
		internal static void DeleteDrivesData(Vessel vessel)
		{
			foreach (PartData partData in vessel.KerbalismData().PartDatas)
			{
				if (partData.Drive != null)
				{
					partData.Drive.DeleteDriveData();
				}
			}
		}

		internal static List<Drive> GetDrives (VesselData vd, bool includePrivate = false)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.Drive.GetDrives");
			List<Drive> drives = new List<Drive>();

			foreach (PartData partData in vd.PartDatas)
			{
				if (partData.Drive != null && (includePrivate || !partData.Drive.is_private))
				{
					drives.Add(partData.Drive);
				}
			}

			UnityEngine.Profiling.Profiler.EndSample();
			return drives;
		}

		internal static List<Drive> GetDrives(Vessel v, bool includePrivate = false)
		{
			return GetDrives(v.KerbalismData(), includePrivate);
		}

		internal static List<Drive> GetDrives(ProtoVessel pv, bool includePrivate = false)
		{
			return GetDrives(pv.KerbalismData(), includePrivate);
		}

		internal static void GetCapacity(VesselData vesseldata, out double free_capacity, out double total_capacity)
		{
			free_capacity = 0;
			total_capacity = 0;
			if (Features.Science)
			{
				foreach (var drive in GetDrives(vesseldata))
				{
					if (drive.dataCapacity < 0 || free_capacity < 0)
					{
						free_capacity = -1;
					}
					else
					{
						free_capacity += drive.FileCapacityAvailable();
						total_capacity += drive.dataCapacity;
					}
				}

				if (free_capacity < 0)
				{
					free_capacity = double.MaxValue;
					total_capacity = double.MaxValue;
				}
			}
		}

		/// <summary> Get a drive for storing files. Will return null if there are no drives on the vessel </summary>
		internal static Drive FileDrive(VesselData vesselData, double size = 0.0)
		{
			Drive result = null;
			foreach (var drive in GetDrives(vesselData))
			{
				if (result == null)
				{
					result = drive;
					if (size > 0.0 && result.FileCapacityAvailable() >= size)
						return result;
					continue;
				}

				if (size > 0.0 && drive.FileCapacityAvailable() >= size)
				{
					return drive;
				}

				// if we're not looking for a minimum capacity, look for the biggest drive
				if (drive.dataCapacity > result.dataCapacity)
				{
					result = drive;
				}
			}
			return result;
		}

		/// <summary> Get a drive for storing samples. Will return null if there are no drives on the vessel </summary>
		internal static Drive SampleDrive(VesselData vesselData, double size = 0, SubjectData subject = null)
		{
			Drive result = null;
			foreach (var drive in GetDrives(vesselData))
			{
				if (result == null)
				{
					result = drive;
					continue;
				}

				double available = drive.SampleCapacityAvailable(subject);
				if (size > double.Epsilon && available < size)
					continue;
				if (available > result.SampleCapacityAvailable(subject))
					result = drive;
			}
			return result;
		}

		internal Dictionary<SubjectData, File> files;      // science files
		internal Dictionary<SubjectData, Sample> samples;  // science samples
		Dictionary<string, bool> fileSendFlags; // file send flags
		internal double dataCapacity;
		internal int sampleCapacity;
		string name = String.Empty;
		internal bool is_private = false;
	}


} // KERBALISM


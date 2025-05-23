namespace KERBALISM
{
	sealed class File
	{
		/// <summary>data size in Mb</summary>
		internal double size;

		/// <summary>randomized result text</summary>
		internal string resultText;

		/// <summary> will be true if the file was created by the hijacker. Force the stock crediting formula to be applied on recovery</summary>
		internal bool useStockCrediting;

		//public double scienceValueRatio;
		//public double ScienceMaxValue => System.Math.Max((subjectData.ScienceMaxValue * scienceValueRatio) - subjectData.ScienceRetrievedInKSC, 0.0);
		//public double SciencePerMB => subjectData.SciencePerMB * scienceValueRatio;

		internal SubjectData subjectData;

		internal double transmitRate = 0.0;

		internal File(SubjectData subjectData, double size = 0.0, bool useStockCrediting = false, string resultText = "")
		{
			this.subjectData = subjectData;
			this.size = size;
			if (double.IsNaN(size))
			{
				Logging.LogStack($"File has a NaN size on creation : {subjectData.DebugStateInfo}", Logging.LogLevel.Error);
				this.size = 0.0;
			}

			this.useStockCrediting = useStockCrediting;
			if (string.IsNullOrEmpty(resultText))
				this.resultText = ResearchAndDevelopment.GetResults(subjectData.StockSubjectId);
			else
				this.resultText = resultText;
		}

		internal static File Load(string integerSubjectId, ConfigNode node)
		{
			SubjectData subjectData;
			string stockSubjectId = Lib.ConfigValue(node, "stockSubjectId", string.Empty);
			// the stock subject id is stored only if this is an asteroid sample, or a non-standard subject id
			if (stockSubjectId != string.Empty)
				subjectData = ScienceDB.GetSubjectDataFromStockId(stockSubjectId);
			else
				subjectData = ScienceDB.GetSubjectData(integerSubjectId);

			if (subjectData == null)
				return null;

			double size = Lib.ConfigValue(node, "size", 0.0);
			if (double.IsNaN(size))
			{
				Logging.LogStack($"File has a NaN size on load : {subjectData.DebugStateInfo}", Logging.LogLevel.Error);
				return null;
			}

			string resultText = Lib.ConfigValue(node, "resultText", "");
			bool useStockCrediting = Lib.ConfigValue(node, "useStockCrediting", false);

			return new File(subjectData, size, useStockCrediting, resultText);
		}

		// this is a fallback loading method for pre 3.1 / pre build 7212 files saved used the stock subject id
		internal static File LoadOldFormat(string stockSubjectId, ConfigNode node)
		{
			SubjectData subjectData = ScienceDB.GetSubjectDataFromStockId(stockSubjectId);

			if (subjectData == null)
				return null;

			double size = Lib.ConfigValue(node, "size", 0.0);
			if (double.IsNaN(size))
			{
				Logging.LogStack($"File has a NaN size on load : {subjectData.DebugStateInfo}", Logging.LogLevel.Error);
				return null;
			}

			string resultText = Lib.ConfigValue(node, "resultText", "");
			bool useStockCrediting = Lib.ConfigValue(node, "useStockCrediting", false);

			return new File(subjectData, size, useStockCrediting, resultText);
		}

		internal void Save(ConfigNode node)
		{
			node.AddValue("size", size);
			node.AddValue("resultText", resultText);
			node.AddValue("useStockCrediting", useStockCrediting);

			if (subjectData is UnknownSubjectData)
				node.AddValue("stockSubjectId", subjectData.StockSubjectId);
		}

		internal ScienceData ConvertToStockData()
		{
			return new ScienceData((float)size, 1.0f, 1.0f, subjectData.StockSubjectId, subjectData.FullTitle);
		}
	}


} // KERBALISM

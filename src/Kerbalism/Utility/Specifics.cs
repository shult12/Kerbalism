using System.Collections.Generic;
using System.Text;


namespace KERBALISM
{


	interface ISpecifics
	{
		Specifics Specs();
	}

	sealed class Specifics
	{
		internal Specifics()
		{
			entries = new List<Entry>();
		}

		internal void Add(string label, string value = "")
		{
			Entry e = new Entry
			{
				label = label,
				value = value
			};
			entries.Add(e);
		}

		internal string Info(string desc = "")
		{
			StringBuilder sb = new StringBuilder();
			if (desc.Length > 0)
			{
				sb.Append("<i>");
				sb.Append(desc);
				sb.Append("</i>\n\n");
			}
			bool firstEntry = true;
			foreach (Entry e in entries)
			{
				if (!firstEntry)
					sb.Append("\n");
				else
					firstEntry = false;

				sb.Append(e.label);
				if (e.value.Length > 0)
				{
					sb.Append(": <b>");
					sb.Append(e.value);
					sb.Append("</b>");
				}
			}
			return sb.ToString();
		}

		internal class Entry
		{
			internal string label = string.Empty;
			internal string value = string.Empty;
		}

		internal List<Entry> entries;
	}


} // KERBALISM

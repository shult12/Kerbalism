using System;
using System.Text;


namespace KERBALISM
{

	// ----------------------------------------------
	// - YES ITS UGLY BUT DON'T TOUCH OR IT GO BOOM -
	// ----------------------------------------------

	class ReadArchive
	{
		internal ReadArchive(string data)
		{
			this.data = data;
		}

		internal void Load(out int integer)
		{
			integer = data[index] - 32;
			++index;
		}

		internal void Load(out string text)
		{
			int len;
			Load(out len);
			text = data.Substring(index, len);
			index += len;
		}

		internal void Load(out double value)
		{
			string s;
			Load(out s);
			value = Parse.ToDouble(s);
		}

		string data;
		int index;
	}


	class WriteArchive
	{
		internal void Save(int integer)
		{
			integer = Lib.Clamp(integer + 32, 32, 255);
			sb.Append((char)integer);
		}

		internal void Save(string text)
		{
			Save(text.Length);
			sb.Append(text.Substring(0, Math.Min(255 - 32, text.Length)));
		}

		internal void Save(double value)
		{
			Save(value.ToString());
		}

		internal string Serialize()
		{
			return sb.ToString();
		}

		StringBuilder sb = new StringBuilder();
	}


} // KERBALISM


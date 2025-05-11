using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KERBALISM
{
	static class String
	{
		#region STRING
		/// <summary> return string limited to len, with ... at the end</summary>
		internal static string Ellipsis(string s, uint len)
		{
			len = System.Math.Max(len, 3u);
			return s.Length <= len ? s : BuildString(s.Substring(0, (int)len - 3), "...");
		}

		/// <summary> return string limited to len, with ... in the middle</summary>
		internal static string EllipsisMiddle(string s, int len)
		{
			if (s.Length > len)
			{
				len = (len - 3) / 2;
				return BuildString(s.Substring(0, len), "...", s.Substring(s.Length - len));
			}
			return s;
		}

		///<summary>tokenize a string</summary>
		internal static List<string> Tokenize(string txt, char separator)
		{
			List<string> ret = new List<string>();
			string[] strings = txt.Split(separator);
			foreach (string s in strings)
			{
				string trimmed = s.Trim();
				if (trimmed.Length > 0) ret.Add(trimmed);
			}
			return ret;
		}

		///<summary>
		/// return message with the macro expanded
		///- variant: tokenize the string by '|' and select one
		///</summary>
		internal static string ExpandMsg(string txt, Vessel v = null, ProtoCrewMember c = null, uint variant = 0)
		{
			// get variant
			var variants = txt.Split('|');
			if (variants.Length > variant) txt = variants[variant];

			// macro expansion
			string v_name = v != null ? (v.isEVA ? "EVA" : v.vesselName) : "";
			string c_name = c != null ? c.name : "";
			return txt
			  .Replace("@", "\n")
			  .Replace("$VESSEL", BuildString("<b>", v_name, "</b>"))
			  .Replace("$KERBAL", "<b>" + c_name + "</b>")
			  .Replace("$ON_VESSEL", v != null && v.isActiveVessel ? "" : BuildString("On <b>", v_name, "</b>, "))
			  .Replace("$HIS_HER", c != null && c.gender == ProtoCrewMember.Gender.Male ? Local.Kerbal_his : Local.Kerbal_her);//"his""her"
		}

		///<summary>make the first letter uppercase</summary>
		internal static string UppercaseFirst(string s)
		{
			return s.Length > 0 ? char.ToUpper(s[0]) + s.Substring(1) : string.Empty;
		}

		///<summary>standardized kerbalism string colors</summary>
		internal enum Kolor
		{
			None,
			Green,
			Yellow,
			Orange,
			Red,
			PosRate,
			NegRate,
			Science,
			Cyan,
			LightGrey,
			DarkGrey
		}

		///<summary>return a colored "[V]" or "[X]" depending on the condition. Only work if placed at the begining of a line. To align other lines, use the "<pos=5em>" tag</summary>
		internal static string Checkbox(bool condition)
		{
			return condition
				? " <color=#88FF00><mark=#88FF0033><mspace=1em><b><i>V </i></b></mspace></mark></color><pos=5em>"
				: " <color=#FF8000><mark=#FF800033><mspace=1em><b><i>X </i></b></mspace></mark></color><pos=5em>";
		}

		///<summary>return the hex representation for kerbalism Kolors</summary>
		static string KolorToHex(Kolor color)
		{
			switch (color)
			{
				case Kolor.None: return "#FFFFFF"; // use this in the Color() methods if no color tag is to be applied
				case Kolor.Green: return "#88FF00"; // green whith slightly less red than the ksp ui default (CCFF00), for better contrast with yellow
				case Kolor.Yellow: return "#FFD200"; // ksp ui yellow
				case Kolor.Orange: return "#FF8000"; // ksp ui orange
				case Kolor.Red: return "#FF3333"; // custom red
				case Kolor.PosRate: return "#88FF00"; // green
				case Kolor.NegRate: return "#FF8000"; // orange
				case Kolor.Science: return "#6DCFF6"; // ksp science color
				case Kolor.Cyan: return "#00FFFF"; // cyan
				case Kolor.LightGrey: return "#CCCCCC"; // light grey
				case Kolor.DarkGrey: return "#999999"; // dark grey	
				default: return "#FEFEFE";
			}
		}

		///<summary>return the unity Colot  for kerbalism Kolors</summary>
		internal static Color KolorToColor(Kolor color)
		{
			switch (color)
			{
				case Kolor.None: return new Color(1.000f, 1.000f, 1.000f);
				case Kolor.Green: return new Color(0.533f, 1.000f, 0.000f);
				case Kolor.Yellow: return new Color(1.000f, 0.824f, 0.000f);
				case Kolor.Orange: return new Color(1.000f, 0.502f, 0.000f);
				case Kolor.Red: return new Color(1.000f, 0.200f, 0.200f);
				case Kolor.PosRate: return new Color(0.533f, 1.000f, 0.000f);
				case Kolor.NegRate: return new Color(1.000f, 0.502f, 0.000f);
				case Kolor.Science: return new Color(0.427f, 0.812f, 0.965f);
				case Kolor.Cyan: return new Color(0.000f, 1.000f, 1.000f);
				case Kolor.LightGrey: return new Color(0.800f, 0.800f, 0.800f);
				case Kolor.DarkGrey: return new Color(0.600f, 0.600f, 0.600f);
				default: return new Color(1.000f, 1.000f, 1.000f);
			}
		}

		///<summary>return string with the specified color and bold if stated</summary>
		internal static string Color(string s, Kolor color, bool bold = false)
		{
			return !bold ? BuildString("<color=", KolorToHex(color), ">", s, "</color>") : BuildString("<color=", KolorToHex(color), "><b>", s, "</b></color>");
		}

		///<summary>return string with different colors depending on the specified condition. "KColor.Default" will not apply any coloring</summary>
		internal static string Color(bool condition, string s, Kolor colorIfTrue, Kolor colorIfFalse = Kolor.None, bool bold = false)
		{
			return condition ? Color(s, colorIfTrue, bold) : colorIfFalse == Kolor.None ? bold ? Bold(s) : s : Color(s, colorIfFalse, bold);
		}

		///<summary>return different colored strings depending on the specified condition. "KColor.Default" will not apply any coloring</summary>
		internal static string Color(bool condition, string sIfTrue, Kolor colorIfTrue, string sIfFalse, Kolor colorIfFalse = Kolor.None, bool bold = false)
		{
			return condition ? Color(sIfTrue, colorIfTrue, bold) : colorIfFalse == Kolor.None ? bold ? Bold(sIfFalse) : sIfFalse : Color(sIfFalse, colorIfFalse, bold);
		}

		///<summary>return string in bold</summary>
		internal static string Bold(string s)
		{
			return BuildString("<b>", s, "</b>");
		}

		///<summary>return string in italic</summary>
		internal static string Italic(string s)
		{
			return BuildString("<i>", s, "</i>");
		}

		///<summary>add spaces on caps</summary>
		internal static string SpacesOnCaps(string s)
		{
			return System.Text.RegularExpressions.Regex.Replace(s, "[A-Z]", " $0").TrimStart();
		}

		///<summary>convert to smart_case</summary>
		internal static string SmartCase(string s)
		{
			return SpacesOnCaps(s).ToLower().Replace(' ', '_');
		}

		///<summary>converts_from_this to this</summary>
		internal static string SpacesOnUnderscore(string s)
		{
			return s.Replace('_', ' ');
		}

		///<summary>select a string at random</summary>
		internal static string TextVariant(params string[] list)
		{
			return list.Length == 0 ? string.Empty : list[Random.RandomInt(list.Length)];
		}

		/// <summary> insert lines break to have a max line length of 'maxCharPerLine' characters </summary>
		internal static string WordWrapAtLength(string longText, int maxCharPerLine)
		{

			longText = longText.Replace("\n", "");
			int currentPosition = 0;
			int textLength = longText.Length;
			while (true)
			{
				// if the remaining text is shorter that maxCharPerLine, return.
				if (currentPosition + maxCharPerLine >= textLength)
					break;

				// get position of first space before maxCharPerLine
				int nextSpacePosition = longText.LastIndexOf(' ', currentPosition + maxCharPerLine);

				// we found a space in the next line, replace it with a new line
				if (nextSpacePosition > currentPosition)
				{
					char[] longTextArray = longText.ToCharArray();
					longTextArray[nextSpacePosition] = '\n';
					longText = new string(longTextArray);
					currentPosition = nextSpacePosition;

				}
				// else break the word
				else
				{
					nextSpacePosition = currentPosition + maxCharPerLine;
					longText = longText.Insert(nextSpacePosition, "-\n");
					textLength += 2;
					currentPosition = nextSpacePosition + 2;
				}
			}
			return longText;

		}
		#endregion

		#region BUILD STRING
		// compose a set of strings together, without creating temporary objects
		// note: the objective here is to minimize number of temporary variables for GC
		// note: okay to call recursively, as long as all individual concatenation is atomic
		static readonly StringBuilder sb = new StringBuilder(256);
		internal static string BuildString(string a, string b)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			return sb.ToString();
		}
		internal static string BuildString(string a, string b, string c)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			return sb.ToString();
		}
		internal static string BuildString(string a, string b, string c, string d)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			return sb.ToString();
		}
		internal static string BuildString(string a, string b, string c, string d, string e)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			sb.Append(e);
			return sb.ToString();
		}
		internal static string BuildString(string a, string b, string c, string d, string e, string f)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			sb.Append(e);
			sb.Append(f);
			return sb.ToString();
		}
		internal static string BuildString(string a, string b, string c, string d, string e, string f, string g)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			sb.Append(e);
			sb.Append(f);
			sb.Append(g);
			return sb.ToString();
		}
		internal static string BuildString(string a, string b, string c, string d, string e, string f, string g, string h)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			sb.Append(e);
			sb.Append(f);
			sb.Append(g);
			sb.Append(h);
			return sb.ToString();
		}
		internal static string BuildString(params string[] args)
		{
			sb.Length = 0;
			foreach (string s in args) sb.Append(s);
			return sb.ToString();
		}
		#endregion
	}
}

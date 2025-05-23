using System.Collections.Generic;
using UnityEngine;


namespace KERBALISM
{


	enum Severity
	{
		relax,    // something went back to nominal
		warning,  // the user should start being worried about something
		danger,   // the user should start panicking about something
		fatality, // somebody died
		breakdown // somebody is breaking down
	}


	sealed class Message
	{
		// represent an entry in the message list
		sealed class Entry
		{
			internal string msg;
			internal float first_seen;
		}

		internal sealed class MessageObject
		{
			internal string title;
			internal string msg;
		}

		internal static List<MessageObject> all_logs;


		// ctor
		internal Message()
		{
			// enable global access
			instance = this;

			// setup style
			style = Styles.message;

			if (all_logs == null)
			{
				all_logs = new List<MessageObject>();
			}
		}


		// called every frame
		internal void On_gui()
		{
			// if queue is empty, do nothing
			if (entries.Count == 0) return;

			// get current time
			float time = UnityEngine.Time.realtimeSinceStartup;

			// get first entry in the queue
			Entry e = entries.Peek();

			// if never visualized, remember first time shown
			if (e.first_seen <= float.Epsilon) e.first_seen = time;

			// if visualized for too long, remove from the queue and skip this update
			if (e.first_seen + PreferencesMessages.Instance.messageLength < time) { entries.Dequeue(); return; }

			// calculate content size
			GUIContent content = new GUIContent(e.msg);
			Vector2 size = style.CalcSize(content);
			size = style.CalcScreenSize(size);
			size.x += style.padding.left + style.padding.right;
			size.y += style.padding.bottom + style.padding.top;

			// calculate position
			Rect rect = new Rect((Screen.width - size.x) * 0.5f, (Screen.height - size.y - offset), size.x, size.y);

			// render the message
			var prev_style = GUI.skin.label;
			GUI.skin.label = style;
			GUI.Label(rect, e.msg);
			GUI.skin.label = prev_style;
		}


		// add a plain message
		internal static void Post(string msg)
		{
			// ignore the message if muted
			if (instance.muted) return;

			// if the user want to use the stock message system, just post it there
			if (PreferencesMessages.Instance.stockMessages)
			{
				ScreenMessages.PostScreenMessage(msg, PreferencesMessages.Instance.messageLength, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			// avoid adding the same message if already present in the queue
			foreach (Entry e in instance.entries) { if (e.msg == msg) return; }

			// compile entry
			Entry entry = new Entry
			{
				msg = msg,
				first_seen = 0
			};

			// add entry
			instance.entries.Enqueue(entry);
		}


		// add a message
		internal static void Post(string text, string subtext)
		{
			// ignore the message if muted
			if (instance.muted) return;

			if (subtext.Length == 0) Post(text);
			else Post(String.BuildString(text, "\n<i>", subtext, "</i>"));
			all_logs.Add(new MessageObject
			{
				msg = String.BuildString(text, "\n<i>", subtext, "</i>"),
			});
			TruncateLogs();
		}


		// add a message
		internal static void Post(Severity severity, string text, string subtext = "")
		{
			// ignore the message if muted
			if (instance.muted) return;

			string title = "";
			switch (severity)
			{
				case Severity.relax: title = String.BuildString(String.Color(Local.Message_RELAX, String.Kolor.Green, true), "\n"); break;//"RELAX"
				case Severity.warning: title = String.BuildString(String.Color(Local.Message_WARNING, String.Kolor.Yellow, true), "\n"); Time.StopWarp(); break; //"WARNING"
				case Severity.danger: title = String.BuildString(String.Color(Local.Message_DANGER, String.Kolor.Red, true), "\n"); Time.StopWarp(); break; //"DANGER"
				case Severity.fatality: title = String.BuildString(String.Color(Local.Message_FATALITY, String.Kolor.Red, true), "\n"); Time.StopWarp(); break; //"FATALITY"
				case Severity.breakdown: title = String.BuildString(String.Color(Local.Message_BREAKDOWN, String.Kolor.Orange, true), "\n"); Time.StopWarp(); break; //"BREAKDOWN"
			}
			if (subtext.Length == 0) Post(String.BuildString(title, text));
			else Post(String.BuildString(title, text, "\n<i>", subtext, "</i>"));
			all_logs.Add(new MessageObject
			{
				title = title,
				msg = String.BuildString(text, "\n<i>", subtext, "</i>"),
			});
			TruncateLogs();
		}

		// This is a bad workaround for the poor performance we have in the log window,
		// which instantiates all GUI elements for every log message for every frame.
		// Especially when viewing longer logs this can lead to a serious performance
		// impact, so we keep the log length short.
		// A good solution would have to re-implement the log using the new UI classes,
		// and while doing that also fix the broken layouting we get with long messages.
		static void TruncateLogs()
		{
			while(all_logs.Count > 25)
			{
				// remove oldest entries at the front, keep the newest entries added at the end
				all_logs.RemoveAt(0);
			}
		}

		/// <summary> Clear all log lists. Called when a new game is loaded </summary>
		internal static void Clear()
		{
			all_logs.Clear();
			instance.entries.Clear();
		}

		// disable rendering of messages
		internal static void Mute()
		{
			instance.muted = true;
		}


		// re-enable rendering of messages
		internal static void Unmute()
		{
			instance.muted = false;
		}


		// return true if user channel is muted
		internal static bool IsMuted()
		{
			return instance.muted;
		}


		readonly float offset = Styles.ScaleFloat(266.0f);

		// store entries
		Queue<Entry> entries = new Queue<Entry>();

		// disable message rendering
		bool muted;

		// styles
		GUIStyle style;

		// permit global access
		static Message instance;
	}


} // KERBALISM

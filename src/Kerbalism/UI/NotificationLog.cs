using System;

namespace KERBALISM
{
	static class NotificationLog
	{
		internal static void Logman(this Panel p, Vessel v)
		{
			p.Title(String.BuildString(String.Ellipsis(v.vesselName, Styles.ScaleStringLength(40)), " ", String.Color(Local.LogMan_ALLLOGS, String.Kolor.LightGrey)));//"ALL LOGS"
			p.Width(Styles.ScaleWidthFloat(465.0f));
			p.paneltype = Panel.PanelType.log;

			p.AddSection(Local.LogMan_LOGS);//"LOGS"
			if (Message.all_logs == null || Message.all_logs.Count == 0)
			{
				p.AddContent("<i>"+Local.LogMan_nologs +"</i>", string.Empty);//no logs
			}
			else
			{
				p.AddContent(string.Empty, string.Empty); //keeps it from bumping into the top
				for (int i = Message.all_logs.Count - 1; i >= 0; --i) //count backwards so most recent is first
				{
					Message.MessageObject log = Message.all_logs[i];
					if (log.title != null)
					{
						p.AddContent(log.title.Replace("\n", "   "), log.msg.Replace("\n", ". "));
					}
					else
					{
						p.AddContent(String.Color(Local.LogMan_ALERT, String.Kolor.Yellow), log.msg.Replace("\n", ". "));//"ALERT   "
					}
					if (Message.all_logs.Count > 1)
					{
						p.AddContent(string.Empty, string.Empty); //this avoids things flowing into each other.
					}
				}
			}
		}
	}
}

using UnityEngine;

namespace KERBALISM
{


	static class TimedOut
	{
		internal static bool Timeout(this Panel p, VesselData vd)
		{
			if (!vd.Connection.linked && vd.CrewCount == 0 && !vd.Vessel.isEVA)
			{ 
				p.AddHeader(msg[((int)UnityEngine.Time.realtimeSinceStartup) % msg.Length]);
				return true;
			}
			return false;
		}

		static string[] msg =
		{
	"<i>"+Local.TimeoutMsg1+"</i>",//Connection in progress
	"<i>"+Local.TimeoutMsg1+".</i>",//Connection in progress.
	"<i>"+Local.TimeoutMsg1+"..</i>",//Connection in progress..
	"<i>"+Local.TimeoutMsg1+"...</i>",//Connection in progress...
	"<i>"+Local.TimeoutMsg1+"....</i>",//Connection in progress....
	"<i>"+Local.TimeoutMsg1+".....</i>",//Connection in progress.....
	"<b><color=#ff3333><i>"+Local.TimeoutMsg2+"</i></color></b>",//Connection timed-out
	"<b><color=#ff3333><i>"+Local.TimeoutMsg2+"</i></color></b>",//Connection timed-out
	"<b><color=#ff3333><i>"+Local.TimeoutMsg2+"</i></color></b>",//Connection timed-out
	"<b><color=#ff3333><i>"+Local.TimeoutMsg2+"</i></color></b>",//Connection timed-out
	"<b><color=#ff3333><i>"+Local.TimeoutMsg2+"</i></color></b>",//Connection timed-out
	"<i>" + Local.TimeoutMsg3.Format("3s") + "</i>",//New tentative in <<1>>
	"<i>" + Local.TimeoutMsg3.Format("2s") + "</i>",//New tentative in <<1>>
	"<i>" + Local.TimeoutMsg3.Format("1s") + "</i>",//New tentative in <<1>>
  };
	}


} // KERBALISM


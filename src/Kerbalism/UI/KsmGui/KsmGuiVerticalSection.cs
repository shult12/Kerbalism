using UnityEngine;
using UnityEngine.UI;

namespace KERBALISM.KsmGui
{
	class KsmGuiVerticalSection : KsmGuiVerticalLayout
	{
		internal KsmGuiVerticalSection(KsmGuiBase parent) : base(parent, 0, 5,5,5,5, TextAnchor.UpperLeft)
		{
			Image background = TopObject.AddComponent<Image>();
			background.color = KsmGuiStyle.boxColor;
		}

	}
}

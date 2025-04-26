using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KERBALISM.KsmGui
{
	class KsmGuiTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		string tooltipText;
		bool IsTooltipOverThis = false;

		public void OnPointerEnter(PointerEventData eventData)
		{
			KsmGuiTooltipController.Instance.ShowTooltip(tooltipText);
			IsTooltipOverThis = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			KsmGuiTooltipController.Instance.HideTooltip();
			IsTooltipOverThis = false;
		}

		internal void SetTooltipText(string text)
		{
			tooltipText = text;
			if (IsTooltipOverThis)
				KsmGuiTooltipController.Instance.SetTooltipText(text);
		}
	}
}

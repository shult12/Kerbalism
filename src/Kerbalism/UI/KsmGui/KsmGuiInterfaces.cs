using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace KERBALISM.KsmGui
{
	interface IKsmGuiText
	{
		string Text { get; set; }
	}

	interface IKsmGuiInteractable
	{
		bool Interactable { get; set; }
	}

	interface IKsmGuiButton
	{
		void SetButtonOnClick(UnityAction action);
	}


	interface IKsmGuiIcon
	{
		void SetIconTexture(Texture2D texture, int width = 16, int height = 16);

		void SetIconColor(Color color);

		void SetIconColor(Lib.Kolor kColor);
	}

	interface IKsmGuiToggle
	{
		void SetToggleOnChange(UnityAction<bool> action);
	}
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KERBALISM.KsmGui
{
	static class KsmGuiStyle
	{
		internal static readonly float defaultWindowOpacity = 0.8f;

		internal static readonly Color textColor = Color.white;
		internal static readonly TMP_FontAsset textFont = UISkinManager.TMPFont; // KSP default font : Noto-sans
		internal static readonly float textSize = 12f;

		internal static readonly Color tooltipBackgroundColor = Color.black;
		internal static readonly Color tooltipBorderColor = Color.white; // new Color(1f, 0.82f, 0f); // yellow #FFD200

		internal static readonly Color boxColor = new Color(0f, 0f, 0f, 0.2f);
		internal static readonly Color selectedBoxColor = new Color(0f, 0f, 0f, 0.5f);

		static readonly Color headerColor = Color.black;

		internal static readonly float tooltipMaxWidth = 300f;

		internal static readonly ColorBlock iconTransitionColorBlock = new ColorBlock()
		{
			normalColor = Color.white,
			highlightedColor = new Color(0.8f, 0.8f, 0.8f, 0.8f),
			pressedColor = Color.white,
			disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f),
			colorMultiplier = 1f,
			fadeDuration = 0.1f
		};

		internal static readonly SpriteState buttonSpriteSwap = new SpriteState()
		{
			highlightedSprite = Textures.KsmGuiSpriteBtnHighlight,
			pressedSprite = Textures.KsmGuiSpriteBtnHighlight,
			disabledSprite = Textures.KsmGuiSpriteBtnDisabled
		};

	}
}

using TMPro;

namespace KERBALISM.KsmGui
{
	class KsmGuiTextBox : KsmGuiVerticalSection, IKsmGuiText
	{
		internal KsmGuiText TextObject { get; private set; }

		internal KsmGuiTextBox(KsmGuiBase parent, string text, string tooltipText = null, TextAlignmentOptions alignement = TextAlignmentOptions.TopLeft) : base(parent)
		{
			SetLayoutElement(true, true);
			TextObject = new KsmGuiText(this, text, null, alignement);

			if (tooltipText != null) SetTooltipText(text);
		}

		public string Text
		{
			get => TextObject.Text;
			set => TextObject.Text = value;
		}
	}
}

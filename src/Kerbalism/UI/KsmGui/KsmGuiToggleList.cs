using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KERBALISM.KsmGui
{
	class KsmGuiToggleList<T> : KsmGuiVerticalLayout
	{
		internal ToggleGroup ToggleGroupComponent { get; private set; }
		internal UnityAction<T> OnChildToggleActivated { get; private set; }
		internal List<KsmGuiToggleListElement<T>> ChildToggles { get; private set; } = new List<KsmGuiToggleListElement<T>>();

		internal KsmGuiToggleList(KsmGuiBase parent, UnityAction<T> onChildToggleActivated)
			: base(parent, 2, 0, 0, 0, 0, TextAnchor.UpperLeft)
		{
			ToggleGroupComponent = TopObject.AddComponent<ToggleGroup>();
			OnChildToggleActivated = onChildToggleActivated;
		}
	}

	class KsmGuiToggleListElement<T> : KsmGuiHorizontalLayout, IKsmGuiInteractable, IKsmGuiText, IKsmGuiToggle
	{
		KsmGuiText TextObject { get; set; }
		internal Toggle ToggleComponent { get; private set; }
		internal T ToggleId { get; private set; }
		KsmGuiToggleList<T> parent;

		internal KsmGuiToggleListElement(KsmGuiToggleList<T> parent, T toggleId, string text) : base(parent)
		{
			ToggleComponent = TopObject.AddComponent<Toggle>();
			ToggleComponent.transition = Selectable.Transition.None;
			ToggleComponent.navigation = new Navigation() { mode = Navigation.Mode.None };
			ToggleComponent.isOn = false;
			ToggleComponent.toggleTransition = Toggle.ToggleTransition.Fade;
			ToggleComponent.group = parent.ToggleGroupComponent;

			this.parent = parent;
			parent.ChildToggles.Add(this);
			ToggleId = toggleId;
			ToggleComponent.onValueChanged.AddListener(NotifyParent);

			Image image = TopObject.AddComponent<Image>();
			image.color = KsmGuiStyle.boxColor;

			SetLayoutElement(false, false, -1, -1, -1, 14);

			KsmGuiVerticalLayout highlightImage = new KsmGuiVerticalLayout(this);
			Image bgImage = highlightImage.TopObject.AddComponent<Image>();
			bgImage.color = KsmGuiStyle.selectedBoxColor;
			bgImage.raycastTarget = false;
			ToggleComponent.graphic = bgImage;

			TextObject = new KsmGuiText(highlightImage, text);
			TextObject.SetLayoutElement(true);
		}

		void NotifyParent(bool enabled)
		{
			if (enabled && parent.OnChildToggleActivated != null)
			{
				parent.OnChildToggleActivated(ToggleId);
			}
		}

		public bool Interactable
		{
			get => ToggleComponent.interactable;
			set => ToggleComponent.interactable = value;
		}

		public string Text
		{
			get => TextObject.Text;
			set => TextObject.Text = value;
		}

		public void SetToggleOnChange(UnityAction<bool> action)
		{
			ToggleComponent.onValueChanged.AddListener(action);
		}
	}
}

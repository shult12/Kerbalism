using System;
using UnityEngine;
using UnityEngine.UI;

namespace KERBALISM.KsmGui
{
	class KsmGuiBase
	{
		internal RectTransform TopTransform { get; private set; }
		internal GameObject TopObject { get; private set; }
		LayoutElement LayoutElement { get; set; }
		KsmGuiUpdateHandler UpdateHandler { get; set; }
		KsmGuiTooltip tooltip;
		KsmGuiLayoutOptimizer layoutOptimizer;

		/// <summary>
		/// transform that will be used as parent for child KsmGui objects.
		/// override this if you have an internal object hierarchy where child
		/// objects must be parented to a specific transform (ex : scroll view)
		/// </summary>
		internal virtual RectTransform ParentTransformForChilds => TopTransform;

		internal KsmGuiBase(KsmGuiBase parent)
		{
			TopObject = new GameObject(Name);
			TopTransform = TopObject.AddComponent<RectTransform>();
			TopObject.AddComponent<CanvasRenderer>();

			if (parent != null)
			{
				layoutOptimizer = parent.layoutOptimizer;
				TopTransform.SetParentFixScale(parent.ParentTransformForChilds);
			}
			else
			{
				layoutOptimizer = TopObject.AddComponent<KsmGuiLayoutOptimizer>();
			}

			TopObject.SetLayerRecursive(5);
		}

		internal virtual string Name => GetType().Name;

		internal virtual bool Enabled
		{
			get => TopObject.activeSelf;
			set
			{
				TopObject.SetActive(value);
				// if enabling and update frequency is more than every update, update immediately
				if (value && UpdateHandler != null)
					UpdateHandler.UpdateASAP();
			}
		}

		/// <summary> callback that will be called on this object Update(). Won't be called if Enabled = false </summary>
		/// <param name="updateFrequency">amount of Update() frames skipped between each call. 50 =~ 1 sec </param>
		internal void SetUpdateAction(Action action, int updateFrequency = 1)
		{
			if (UpdateHandler == null)
				UpdateHandler = TopObject.AddComponent<KsmGuiUpdateHandler>();

			UpdateHandler.updateAction = action;
			UpdateHandler.updateFrequency = updateFrequency;
			//UpdateHandler.UpdateASAP();
		}

		/// <summary> coroutine-like (IEnumerable) method that will be called repeatedly as long as Enabled = true </summary>
		internal void SetUpdateCoroutine(KsmGuiUpdateCoroutine coroutineFactory)
		{
			if (UpdateHandler == null)
				UpdateHandler = TopObject.AddComponent<KsmGuiUpdateHandler>();

			UpdateHandler.coroutineFactory = coroutineFactory;
		}

		internal void ForceExecuteCoroutine(bool fromStart = false)
		{
			if (UpdateHandler != null)
				UpdateHandler.ForceExecuteCoroutine(fromStart);
		}

		internal void SetTooltipText(string text)
		{
			if (text == null)
				return;

			if (tooltip == null)
				tooltip = TopObject.AddComponent<KsmGuiTooltip>();

			tooltip.SetTooltipText(text);
		}

		/// <summary> Add sizing constraints trough a LayoutElement component</summary>
		internal void SetLayoutElement(bool flexibleWidth = false, bool flexibleHeight = false, int preferredWidth = -1, int preferredHeight = -1, int minWidth = -1, int minHeight = -1)
		{
			if (LayoutElement == null)
				LayoutElement = TopObject.AddComponent<LayoutElement>();

			LayoutElement.flexibleWidth = flexibleWidth ? 1f : -1f;
			LayoutElement.flexibleHeight = flexibleHeight ? 1f : -1f;
			LayoutElement.preferredWidth = preferredWidth;
			LayoutElement.preferredHeight = preferredHeight;
			LayoutElement.minWidth = minWidth;
			LayoutElement.minHeight = minHeight;
		}

		internal void RebuildLayout() => layoutOptimizer.RebuildLayout();

		internal void MoveAsFirstChild()
		{
			TopTransform.SetAsFirstSibling();
		}

		void MoveAfter(KsmGuiBase afterThis)
		{
			
		}
	}
}

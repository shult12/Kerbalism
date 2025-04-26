using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KERBALISM.KsmGui
{
	class KsmGuiUpdateCoroutine : IEnumerable
	{
		Func<IEnumerator> updateMethod;
		internal KsmGuiUpdateCoroutine(Func<IEnumerator> updateMethod) => this.updateMethod = updateMethod;
		public IEnumerator GetEnumerator() => updateMethod();
	}

	class KsmGuiUpdateHandler : MonoBehaviour
	{
		int updateCounter;
		internal int updateFrequency = 1;
		internal Action updateAction;
		internal KsmGuiUpdateCoroutine coroutineFactory;
		IEnumerator currentCoroutine;

		internal void UpdateASAP() => updateCounter = updateFrequency;

		void Start()
		{
			// always update on start
			updateCounter = updateFrequency;
		}

		void Update()
		{
			if (updateAction != null)
			{
				updateCounter++;
				if (updateCounter >= updateFrequency)
				{
					updateCounter = 0;
					updateAction();
				}
			}

			if (coroutineFactory != null)
			{
				if (currentCoroutine == null || !currentCoroutine.MoveNext())
					currentCoroutine = coroutineFactory.GetEnumerator();
			}
		}

		internal void ForceExecuteCoroutine(bool fromStart = false)
		{
			if (coroutineFactory == null)
				return;

			if (fromStart || currentCoroutine == null || !currentCoroutine.MoveNext())
				currentCoroutine = coroutineFactory.GetEnumerator();

			while (currentCoroutine.MoveNext()) { }
		}

	}
}

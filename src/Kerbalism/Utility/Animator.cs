using System;
using System.Collections;
using UnityEngine;

namespace KERBALISM
{
	sealed class Animator
	{
		Animation anim;
		readonly string name;
		internal bool reversed = false;

		internal bool IsDefined => anim != null;

		internal Animator(Part p, string anim_name)
		{
			anim = null;
			name = string.Empty;

			if (anim_name.Length > 0)
			{
				Animation[] animations = p.FindModelAnimators(anim_name);
				if (animations.Length > 0)
				{
					anim = animations[0];
					name = anim_name;
				}
			}
		}

		// Note: This function resets animation to the beginning
		internal void Play(bool reverse, bool loop, Action callback = null)
		{
			if(anim == null)
			{
				callback?.Invoke();
				return;
			}

			Kerbalism.Fetch.StartCoroutine(PlayAnimation(reverse, loop, callback));
		}

		IEnumerator PlayAnimation(bool reverse, bool loop, Action callback = null)
		{
			if (reverse && callback != null) callback();

			var playDirection = reverse;
			if (reversed) playDirection = !playDirection;

			anim[name].normalizedTime = !playDirection ? 0.0f : 1.0f;
			anim[name].speed = !playDirection ? 1.0f : -1.0f;
			anim[name].wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
			anim.Play(name);
			yield return new WaitForSeconds(anim[name].length);

			if (!reverse && callback != null) callback();
		}

		internal void Stop(Action callback = null)
		{
			if (anim == null)
			{
				callback?.Invoke();
				return;
			}

			Kerbalism.Fetch.StartCoroutine(StopAnimation(callback));
		}

		IEnumerator StopAnimation(Action callback = null)
		{
			anim.Stop(name);
			yield return new WaitForSeconds(anim[name].length);
			callback?.Invoke();
		}

		internal void Pause()
		{
			if (anim != null)
			{
				anim[name].speed = 0.0f;
			}
		}

		internal void Resume(bool reverse)
		{
			if (anim != null)
			{
				if (reversed) reverse = !reverse;
				anim[name].speed = !reverse ? 1.0f : -1.0f;
			}
		}

		internal void Still(double t)
		{
			if (anim != null)
			{
				t = reversed ? 1 - t : t;
				anim[name].normalizedTime = (float)t;
				anim[name].speed = 0.0f;
				anim.Play(name);
			}
		}

		internal bool Playing()
		{
			if (anim != null)
			{
				return (anim[name].speed > float.Epsilon) && anim.IsPlaying(name);
			}
			return false;
		}
   	}
}

using System;
using System.Reflection;

namespace KERBALISM
{
	class Reflection
	{
		static readonly BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

		///<summary>
		/// return a value from a module using reflection
		/// note: useful when the module is from another assembly, unknown at build time
		/// note: useful when the value isn't persistent
		/// note: this function break hard when external API change, by design
		/// </summary>
		internal static T ReflectionValue<T>(PartModule m, string value_name)
		{
			return (T)m.GetType().GetField(value_name, flags).GetValue(m);
		}

		internal static T? SafeReflectionValue<T>(PartModule m, string value_name) where T : struct
		{
			FieldInfo fi = m.GetType().GetField(value_name, flags);
			if (fi == null)
				return null;
			return (T)fi.GetValue(m);
		}

		///<summary>
		/// set a value from a module using reflection
		/// note: useful when the module is from another assembly, unknown at build time
		/// note: useful when the value isn't persistent
		/// note: this function break hard when external API change, by design
		///</summary>
		internal static void ReflectionValue<T>(PartModule m, string value_name, T value)
		{
			m.GetType().GetField(value_name, flags).SetValue(m, value);
		}

		///<summary> Sets the value of a private field via reflection </summary>
		internal static void ReflectionValue<T>(object instance, string value_name, T value)
		{
			instance.GetType().GetField(value_name, flags).SetValue(instance, value);
		}

		///<summary> Returns the value of a private field via reflection </summary>
		internal static T ReflectionValue<T>(object instance, string field_name)
		{
			return (T)instance.GetType().GetField(field_name, flags).GetValue(instance);
		}

		internal static void ReflectionCall(object m, string call_name)
		{
			m.GetType().GetMethod(call_name, flags).Invoke(m, null);
		}

		internal static T ReflectionCall<T>(object m, string call_name)
		{
			return (T)(m.GetType().GetMethod(call_name, flags).Invoke(m, null));
		}

		internal static void ReflectionCall(object m, string call_name, Type[] types, object[] parameters)
		{
			m.GetType().GetMethod(call_name, flags, null, types, null).Invoke(m, parameters);
		}

		internal static T ReflectionCall<T>(object m, string call_name, Type[] types, object[] parameters)
		{
			return (T)(m.GetType().GetMethod(call_name, flags, null, types, null).Invoke(m, parameters));
		}
	}
}

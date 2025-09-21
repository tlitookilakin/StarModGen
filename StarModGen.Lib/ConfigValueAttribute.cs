using System;
using System.Diagnostics;

#pragma warning disable IDE0060
namespace StarModGen.Lib
{
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Property)]
	public class ConfigValueAttribute : Attribute
	{
		/// <summary>Set this property to be registered with GMCM.</summary>
		/// <param name="value">The "default" value to use for this property.</param>
		public ConfigValueAttribute(object value) { }

		/// <summary>Set this property to be registered with GMCM on a specific page.</summary>
		/// <param name="value">The "default" value to use for this property.</param>
		/// <param name="page">The page id this option appears on.</param>
		public ConfigValueAttribute(object value, string page) { }
	}
}

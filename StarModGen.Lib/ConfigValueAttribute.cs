using System;
using System.Diagnostics;

#pragma warning disable IDE0060
namespace StarModGen.Lib
{
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Property)]
	public class ConfigValueAttribute : Attribute
	{
		public ConfigValueAttribute(object value) { }
		public ConfigValueAttribute(object value, string page) { }
	}
}

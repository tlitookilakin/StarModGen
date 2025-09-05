using System;
using System.Diagnostics;

#pragma warning disable CS9113
namespace StarModGen.Lib
{
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Property)]
	public class ConfigValueAttribute(object value, string? page = null) : Attribute
	{
	}
}

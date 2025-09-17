using System;
using System.Diagnostics;

namespace StarModGen.Lib
{
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Property)]
	public class ConfigRangeAttribute : Attribute
	{
		public object? Min { get; set; }
		public object? Max { get; set; }
		public object? Step { get; set; }
	}
}

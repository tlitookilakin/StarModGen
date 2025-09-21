using System;
using System.Diagnostics;

namespace StarModGen.Lib
{
	/// <summary>
	/// Adds range information to a config option. <br/> 
	/// When placed on a partial property, will generate code to enforce the range and interval.
	/// </summary>
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Property)]
	public class ConfigRangeAttribute : Attribute
	{
		public object? Min { get; set; }
		public object? Max { get; set; }
		public object? Step { get; set; }
	}
}

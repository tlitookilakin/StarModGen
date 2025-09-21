using System;
using System.Diagnostics;

namespace StarModGen.Lib
{
	/// <summary>Mark a partial method as the entry point on an asset handler class.</summary>
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Method)]
	public class AssetEntryAttribute : Attribute
	{
	}
}

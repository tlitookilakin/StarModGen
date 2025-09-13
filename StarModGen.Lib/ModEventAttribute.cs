using System;
using System.Diagnostics;

namespace StarModGen.Lib
{
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Event)]
	public class ModEventAttribute : Attribute
	{
	}
}

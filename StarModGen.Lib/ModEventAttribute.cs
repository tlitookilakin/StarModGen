using System;
using System.Diagnostics;

namespace StarModGen.Lib
{
	/// <summary>Registers a method or event with the EventBus.</summary>
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Event)]
	public class ModEventAttribute : Attribute
	{
	}
}

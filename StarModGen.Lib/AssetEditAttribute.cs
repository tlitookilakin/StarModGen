using System;
using System.Diagnostics;

# pragma warning disable CS9113
namespace StarModGen.Lib
{
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Method)]
	public class AssetEditAttribute(string Name) : Attribute
	{
	}
}

using System;
using System.Diagnostics;

# pragma warning disable CS9113
namespace StarModGen.Lib
{
	/// <summary>Defines the method as an asset editor.</summary>
	/// <param name="Name">The name of the asset to edit</param>

	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Method)]
	public class AssetEditAttribute(string Name) : Attribute
	{
	}
}

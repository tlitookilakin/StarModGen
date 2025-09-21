using System;
using System.Diagnostics;

#pragma warning disable CS9113
namespace StarModGen.Lib
{
	/// <summary>Mark this method as a provider for a specific asset.</summary>
	/// <param name="Name">The name of the asset it provides</param>
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Method)]
	public class AssetLoadAttribute(string Name) : Attribute
	{
	}
}

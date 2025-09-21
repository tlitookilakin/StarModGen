using System;
using System.Diagnostics;

namespace StarModGen.Lib
{
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Property)]
	public class AssetAttribute : Attribute
	{
		/// <summary>Apply to a partial property to make it lazy-load a game asset.</summary>
		/// <param name="Name">The asset to load</param>
		public AssetAttribute(string Name)
		{
		}

		/// <summary>Apply to a partial property to make it lazy-load a game asset, and supply a default asset from a local file.</summary>
		/// <param name="Name">The asset to load</param>
		/// <param name="File">The local file to use for the asset</param>
		public AssetAttribute(string Name, string File) : this(Name)
		{
		}
	}
}

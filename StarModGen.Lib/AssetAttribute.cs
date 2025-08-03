using System;
using System.Diagnostics;

namespace StarModGen.Lib
{
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Property)]
	public class AssetAttribute : Attribute
	{
		public string AssetName { get; set; }
		public string? LocalName { get; set; }

		public AssetAttribute(string Name)
		{
			AssetName = Name;
		}

		public AssetAttribute(string Name, string File) : this(Name)
		{
			LocalName = File;
		}
	}
}

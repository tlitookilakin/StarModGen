using System;
using System.Diagnostics;

namespace StarModGen.Lib
{
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Method)]
	public class AssetIncludeAttribute : Attribute
	{
		public string Name { get; set; }
		public string Source { get; set; }

		public AssetIncludeAttribute(string name, string source)
		{
			Name = name;
			Source = source;
		}
	}
}

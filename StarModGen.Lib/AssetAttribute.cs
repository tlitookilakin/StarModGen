using System;
using System.Diagnostics;

namespace StarModGen.Lib
{
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Property)]
	public class AssetAttribute : Attribute
	{

		public AssetAttribute(string Name)
		{
		}

		public AssetAttribute(string Name, string File) : this(Name)
		{
		}
	}
}

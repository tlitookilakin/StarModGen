using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;
using System.Reflection;

namespace StarModGen
{
	internal static class Utilities
	{
		public static string FullName(this INamedTypeSymbol type)
		{
			if (type.ContainingNamespace is INamespaceSymbol s)
				return $"{s.Name}.{type.Name}";
			return type.Name;
		}

		public static string GetTemplate(string name)
		{
			var asm = Assembly.GetExecutingAssembly();
			var asmName = asm.GetName().Name;
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{asmName}.Templates.{name}.txt");
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}

		public static void AddIncludes(this IncrementalGeneratorPostInitializationContext ctx, params string[] includes)
		{
			var asm = Assembly.GetExecutingAssembly();
			var asmName = asm.GetName().Name;

			foreach (var include in includes)
			{
				using var stream = asm.GetManifestResourceStream($"{asmName}.Includes.{include}.txt");
				using var reader = new StreamReader(stream);
				ctx.AddSource(include, reader.ReadToEnd());
			}
		}

		public static string ToSyntax(this Accessibility a)
		{
			return a switch
			{
				Accessibility.Private => "private ",
				Accessibility.Protected => "protected ",
				Accessibility.Public => "public ",
				Accessibility.Internal => "internal ",
				Accessibility.ProtectedAndInternal => "internal protected ",
				_ => ""
			};
		}

		public static string ToVarname(this string asset)
		{
			return asset.Replace('/', '_').Replace('\\', '_');
		}

		public static string MakeLocal(this string name)
		{
			if (name.Length is 0 || name[0] is not '/')
				return $"\"{name}\"";

			return $"\"Mods/\" + MOD_ID + \"{name}\"";
		}
	}
}

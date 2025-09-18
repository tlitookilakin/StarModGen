using Fluid;
using Fluid.Values;
using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

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

		public static void AddIncludes(Action<string, string> addWith, params string[] includes)
		{
			var asm = Assembly.GetExecutingAssembly();
			var asmName = asm.GetName().Name;

			foreach (var include in includes)
			{
				using var stream = asm.GetManifestResourceStream($"{asmName}.Includes.{include}.txt");
				using var reader = new StreamReader(stream);
				addWith(include, reader.ReadToEnd());
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

		public static bool TryGetNamedParam(this AttributeData data, string key, out TypedConstant val)
		{
			val = default;
			var args = data.NamedArguments;

			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].Key == key)
				{
					val = args[i].Value;
					return true;
				}
			}

			return false;
		}

		public static ValueTask<FluidValue> VarNameFilter(FluidValue i, FilterArguments args, TemplateContext ctx)
			=> FluidValue.Create(i.ToStringValue()?.ToVarname(), ctx.Options);

		public static ValueTask<FluidValue> MakeLocalFilter(FluidValue i, FilterArguments args, TemplateContext ctx)
			=> FluidValue.Create(i.ToStringValue()?.MakeLocal(), ctx.Options);

		public static string ToVarname(this string asset)
		{
			int dot = asset.LastIndexOf('.');
			if (dot > 0)
				asset = asset[..dot];
			return asset.Replace('/', '_').Replace('\\', '_');
		}

		public static string MakeLocal(this string name)
		{
			if (name.Length is 0 || name[0] is not '/')
				return $"\"{name}\"";

			return $"\"Mods/\" + MOD_ID + \"{name}\"";
		}

		public static string WithoutPrefix(this string s, string? prefix)
		{
			if (prefix is null || !s.StartsWith(prefix))
				return s;
			return s[prefix.Length..];
		}

		public static string GuessTypeByName(this string file)
			=> Path.GetExtension(file).ToLowerInvariant() switch
				{
					".png" => "global::Microsoft.Xna.Framework.Graphics.Texture2D",
					".tmx" or ".tbin" => "global::xTile.Map",
					".json" => "object",
					_ => "object"
				};
	}
}

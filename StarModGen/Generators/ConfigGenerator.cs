using Fluid;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarModGen.Generators
{
	// TODO add ranges
	[Generator]
	public class ConfigGenerator : IIncrementalGenerator
	{
		private IFluidTemplate? Template;
		private readonly TemplateOptions options = new();
		private readonly FluidParser parser = new();

		public void Initialize(IncrementalGeneratorInitializationContext ctx)
		{
			options.MemberAccessStrategy.Register<IGrouping<string, ConfigProperty>>();
			options.MemberAccessStrategy.Register<ConfigType>();
			options.MemberAccessStrategy.Register<ConfigProperty>();
			options.MemberAccessStrategy.Register<TemplateData>();
			options.ValueConverters.Add(static v => v is Enum e ? e.ToString() : null);

			var configs = ctx.SyntaxProvider.ForAttributeWithMetadataName<ConfigType>("StarModGen.Lib.ConfigAttribute",
				static (node, cancel) => node is ClassDeclarationSyntax c && c.Modifiers.Any(SyntaxKind.PartialKeyword),
				static (ctx, cancel) => new(
					ctx.TargetSymbol.ContainingNamespace?.ToDisplayString()!,
					ctx.TargetSymbol.Name,
					ctx.TargetSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
					(bool)ctx.Attributes[0].ConstructorArguments[0].Value!
				)
			);

			var props = ctx.SyntaxProvider.ForAttributeWithMetadataName<ConfigProperty>("StarModGen.Lib.ConfigValueAttribute",
				static (node, cancel) => node is PropertyDeclarationSyntax,
				static (ctx, cancel) => new(
					ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)!,
					ctx.TargetSymbol.Name,
					GetType(((IPropertySymbol)ctx.TargetSymbol).Type),
					ctx.Attributes[0].ConstructorArguments[0].ToCSharpString(),
					((IPropertySymbol)ctx.TargetSymbol).Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
					((IPropertySymbol)ctx.TargetSymbol).Type.Name,
					ctx.Attributes[0].ConstructorArguments.Length > 1 ? (string?)ctx.Attributes[0].ConstructorArguments[1].Value : null
				)
			);

			var grouped = configs.Combine(props.Collect());

			ctx.RegisterPostInitializationOutput(static ctx =>
			{
				ctx.AddIncludes("IGMCM");
			});

			ctx.RegisterSourceOutput(configs, static (ctx, cfg) => 
			{
				ctx.AddSource($"{cfg.space}.{cfg.type}_stub", GenerateStub(cfg));
			});

			ctx.RegisterImplementationSourceOutput(grouped, (ctx, g) => 
			{
				ctx.AddSource($"{g.Left.space}.{g.Left.type}", Generate(g.Left, g.Right));
			});

			if (!parser.TryParse(Utilities.GetTemplate("Config"), out Template, out string error))
				throw new Exception(error);
		}

		private static ConfigValueType GetType(ITypeSymbol type)
		{
			return type.Name switch
			{
				"String" => ConfigValueType.String,
				"Int32" => ConfigValueType.Int,
				"Boolean" => ConfigValueType.Bool,
				"Single" => ConfigValueType.Float,
				"SButton" => ConfigValueType.KeyBind,
				"KeybindList" => ConfigValueType.KeyBindList,
				_ => type.BaseType?.Name is "Enum" ? ConfigValueType.Enum : ConfigValueType.None
			};
		}

		private enum ConfigValueType { None, String, Int, Bool, Enum, Float, KeyBind, KeyBindList };
		private record struct ConfigType(string space, string type, string fullName, bool titleOnly);
		private record struct ConfigProperty(string owner, string name, ConfigValueType type, string defaultValue, string typeName, string simpleType, string? page);
		
		private class TemplateData(ConfigType type, IList<ConfigProperty> props, IList<IGrouping<string?, ConfigProperty>> pages)
		{
			public ConfigType Type => type;
			public IList<IGrouping<string?, ConfigProperty>> Pages => pages;
			public IList<ConfigProperty> Props => props;
		}

		private string Generate(ConfigType cfg, IList<ConfigProperty> props)
		{
			List<ConfigProperty> LocalProps = [];
			foreach (var prop in props)
			{
				if (prop.type is ConfigValueType.None)
					continue;

				if (prop.owner == cfg.fullName)
					LocalProps.Add(prop);
			}

			var model = new TemplateData(cfg, LocalProps, [.. LocalProps.GroupBy(static l => l.page)]);
			return Template.Render(new(model, options));
		}

		private static string GenerateStub(ConfigType cfg)
		{
			return $@"
using StardewModdingAPI;
using System;
using StarModGen.Utils;

namespace {cfg.space};

partial class {cfg.type}
{{
	internal static partial {cfg.type} Create(IModHelper helper, IManifest manifest);
	public static event EventHandler<IGMCMApi>? Registering;
	public static event EventHandler<IGMCMApi>? Registered;
	public static event Action<{cfg.type}>? Applied;
	public static event Action<{cfg.type}>? Reset;
}}";
		}
	}
}

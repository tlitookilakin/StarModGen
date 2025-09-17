using Fluid;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StarModGen.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarModGen.Generators;

[Generator]
public class ConfigGenerator : IIncrementalGenerator
{
	private IFluidTemplate? Template;
	private readonly TemplateOptions options = new();
	private readonly FluidParser parser = new();

	public void Initialize(IncrementalGeneratorInitializationContext ctx)
	{
		options.MemberAccessStrategy.Register<IGrouping<string, RangedProperty>>();
		options.MemberAccessStrategy.Register<ConfigType>();
		options.MemberAccessStrategy.Register<ConfigProperty>();
		options.MemberAccessStrategy.Register<RangedProperty>();
		options.MemberAccessStrategy.Register<ConfigRange>();
		options.MemberAccessStrategy.Register<TemplateData>();

		options.ValueConverters.Add(static v => v is Enum e ? e.ToString() : null);
		options.ValueConverters.Add(
			static v => v is IGrouping<string?, RangedProperty> g ? new StringGroupConverter<RangedProperty>(g) : null
		);

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
			static (node, cancel) => node is PropertyDeclarationSyntax p && p.Modifiers.Any(SyntaxKind.PublicKeyword),
			static (ctx, cancel) => new(
				ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)!,
				ctx.TargetSymbol.Name,
				GetType(((IPropertySymbol)ctx.TargetSymbol).Type),
				ctx.Attributes[0].ConstructorArguments[0].ToCSharpString(),
				((IPropertySymbol)ctx.TargetSymbol).Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				((IPropertySymbol)ctx.TargetSymbol).Type.Name,
				ctx.Attributes[0].ConstructorArguments.Length > 1 ? ctx.Attributes[0].ConstructorArguments[1].Value?.ToString() : null
			)
		);

		var ranges = ctx.SyntaxProvider.ForAttributeWithMetadataName<ConfigRange>("StarModGen.Lib.ConfigRangeAttribute",
			static (node, cancel) => node is PropertyDeclarationSyntax,
			static (ctx, cancel) => new(
				ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)!,
				ctx.TargetSymbol.Name,
				((IPropertySymbol)ctx.TargetSymbol).Type.Name is "Single",
				((IPropertySymbol)ctx.TargetSymbol).IsPartialDefinition,
				ctx.Attributes[0].TryGetNamedParam("Min", out var v) ? v.ToCSharpString() : "null",
				ctx.Attributes[0].TryGetNamedParam("Max", out v) ? v.ToCSharpString() : "null",
				ctx.Attributes[0].TryGetNamedParam("Step", out v) ? v.ToCSharpString() : "null"
			)
		);

		var grouped = configs
			.Combine(props.Collect())
			.Combine(ranges.Collect());

		ctx.RegisterPostInitializationOutput(static ctx =>
		{
			Utilities.AddIncludes(ctx.AddSource, "IGMCM");
		});

		ctx.RegisterSourceOutput(configs, static (ctx, cfg) => 
		{
			ctx.AddSource($"{cfg.space}.{cfg.type}_stub", GenerateStub(cfg));
		});

		ctx.RegisterImplementationSourceOutput(grouped, (ctx, g) => 
		{
			ctx.AddSource($"{g.Left.Left.space}.{g.Left.Left.type}", Generate(g.Left.Left, g.Left.Right, g.Right));
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
	private record struct ConfigRange(string owner, string name, bool isFloat, bool partial, string min, string max, string step);

	private class TemplateData(ConfigType type, IList<RangedProperty> props, IList<IGrouping<string?, RangedProperty>> pages)
	{
		public ConfigType Type => type;
		public IList<IGrouping<string?, RangedProperty>> Pages => pages;
		public IList<RangedProperty> Props => props;
	}

	private class RangedProperty(ConfigProperty prop)
	{
		public ConfigProperty Prop => prop;
		public bool HasRange => hasRange;
		public ConfigRange Range
		{
			get => range;
			set {
				range = value;
				hasRange = true;
			}
		}
		private ConfigRange range;
		private bool hasRange = false;
	}

	private string Generate(ConfigType cfg, IList<ConfigProperty> props, IList<ConfigRange> ranges)
	{
		List<RangedProperty> LocalProps = [];
		Dictionary<string, int> RangedBox = [];

		foreach (var prop in props)
		{
			if (prop.type is ConfigValueType.None)
				continue;

			if (prop.owner == cfg.fullName)
			{
				LocalProps.Add(new(prop));
				RangedBox[prop.name] = LocalProps.Count - 1;
			}
		}

		foreach (var range in ranges)
		{
			if (range.owner != cfg.fullName)
				continue;

			if (RangedBox.TryGetValue(range.name, out int i))
				LocalProps[i].Range = range;
		}

		var model = new TemplateData(cfg, LocalProps, [.. LocalProps.GroupBy(static l => l.Prop.page)]);
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

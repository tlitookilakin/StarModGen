using Fluid;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace StarModGen.Generators
{
	// TODO generics???
	[Generator]
	public class AssetGenerator : IIncrementalGenerator
	{
		private IFluidTemplate? Template;
		private readonly TemplateOptions options = new();
		private readonly FluidParser parser = new();

		public void Initialize(IncrementalGeneratorInitializationContext ctx)
		{
			options.MemberAccessStrategy.Register<TemplateData>();
			options.MemberAccessStrategy.Register<AssetEntryMethod>();
			options.MemberAccessStrategy.Register<AssetProperty>();
			options.MemberAccessStrategy.Register<AssetGroup>();
			options.MemberAccessStrategy.Register<AssetEdit>();
			options.MemberAccessStrategy.Register<AssetLoad>();
			options.MemberAccessStrategy.Register<AssetInclude>();
			options.MemberAccessStrategy.Register<KeyValuePair<string, string>>();
			options.MemberAccessStrategy.Register(typeof(IList<>));

			var assetEntries = ctx.SyntaxProvider.ForAttributeWithMetadataName<AssetEntryMethod>("StarModGen.Lib.AssetEntryAttribute",
				static (node, cancel) => node is MethodDeclarationSyntax m && m.Modifiers.Any(SyntaxKind.PartialKeyword),
				static (ctx, cancel) => new(
					ctx.TargetSymbol.ContainingType.ContainingNamespace?.ToDisplayString(), 
					ctx.TargetSymbol.ContainingType.Name, ctx.TargetSymbol.Name,
					ctx.TargetSymbol.ContainingType.AllInterfaces.Any(static i => i.Name == nameof(INotifyPropertyChanged)),
					ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
					ctx.TargetSymbol.DeclaredAccessibility.ToSyntax()
				)
			);

			var assetProps = ctx.SyntaxProvider.ForAttributeWithMetadataName<AssetProperty>("StarModGen.Lib.AssetAttribute", 
				static (node, cancel) => node is PropertyDeclarationSyntax p && p.Modifiers.Any(SyntaxKind.PartialKeyword), 
				static (ctx, cancel) => {
					var args = ctx.Attributes[0].ConstructorArguments;
					return new(
						ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
						ctx.TargetSymbol.Name,
						((IPropertySymbol)ctx.TargetSymbol).Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
						((string)args[0].Value!), 
						args.Length > 1 ? (string?)args[1].Value : null,
						ctx.TargetSymbol.DeclaredAccessibility.ToSyntax(),
						((string)args[0].Value!).MakeLocal()
					);
				}
			);

			var assetEdits = ctx.SyntaxProvider.ForAttributeWithMetadataName<AssetEdit>("StarModGen.Lib.AssetEditAttribute",
				static (node, cancel) => node is MethodDeclarationSyntax m && m.ParameterList.Parameters.Count is 1,
				static (ctx, cancel) =>
				{
					var args = ctx.Attributes[0].ConstructorArguments;
					return new(
						ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
						ctx.TargetSymbol.Name,
						((string)args[0].Value!)
					);
				}
			);

			var assetLoads = ctx.SyntaxProvider.ForAttributeWithMetadataName<AssetLoad>("StarModGen.Lib.AssetLoadAttribute",
				static (node, cancel) => node is MethodDeclarationSyntax m && m.ParameterList.Parameters.Count is 0,
				static (ctx, cancel) =>
				{
					var args = ctx.Attributes[0].ConstructorArguments;
					return new(
						ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
						ctx.TargetSymbol.Name,
						((string)args[0].Value!)
					);
				}
			);

			var assetIncludes = ctx.SyntaxProvider.ForAttributeWithMetadataName<AssetInclude>("StarModGen.Lib.AssetIncludeAttribute",
				static (node, cancel) => node is MethodDeclarationSyntax m,
				static (ctx, cancel) =>
				{
					var args = ctx.Attributes[0].ConstructorArguments;
					return new(
						ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
						((string)args[0].Value!),
						((string)args[1].Value!),
						((string)args[1].Value!).ToVarname()
					);
				}
			);

			var anonLoads = ctx.SyntaxProvider.ForAttributeWithMetadataName<AssetProperty>("StarModGen.Lib.AssetAttribute",
				static (node, cancel) => node is MethodDeclarationSyntax m && m.Modifiers.Any(SyntaxKind.PartialKeyword),
				static (ctx, cancel) => {
					var args = ctx.Attributes[0].ConstructorArguments;
					return new(
						ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
						ctx.TargetSymbol.Name,
						args.Length > 1 ? ((string)args[1].Value!).GuessTypeByName() : "",
						((string)args[0].Value!),
						args.Length > 1 ? (string?)args[1].Value : null,
						ctx.TargetSymbol.DeclaredAccessibility.ToSyntax(),
						((string)args[0].Value!).MakeLocal()
					);
				}
			);

			var grouped = assetEntries
				.Combine(assetProps.Collect())
				.Combine(assetIncludes.Collect())
				.Combine(assetEdits.Collect())
				.Combine(assetLoads.Collect())
				.Combine(anonLoads.Collect());

			ctx.RegisterPostInitializationOutput(static ctx => 
			{
				Utilities.AddIncludes(ctx.AddSource, "AssetHelper");
			});

			ctx.RegisterImplementationSourceOutput(grouped, (ctx, val) => 
			{
				Write(val.Left.Left.Left.Left.Left, val.Left.Left.Left.Left.Right, val.Left.Left.Left.Right, val.Left.Left.Right, val.Left.Right, val.Right, ctx);
			});

			if(!parser.TryParse(Utilities.GetTemplate("AssetManager"), out Template, out string error))
				throw new Exception(error);
		}

		private record struct AssetProperty(string Type, string Prop, string PropType, string Asset, string? Local, string access, string RawAsset);
		private record struct AssetEntryMethod(string? Space, string Type, string Method, bool IsNotify, string fullName, string access);
		private record struct TemplateData(
			AssetEntryMethod Entry, IReadOnlyList<AssetProperty> LocalProps, 
			IEnumerable<AssetGroup> Assets, IEnumerable<KeyValuePair<string, string>> IncludeSources
		);
		private record struct AssetInclude(string Owner, string Target, string Source, string SourceVar);
		private record struct AssetEdit(string Owner, string Method, string Target);
		private record struct AssetLoad(string Owner, string Method, string Target);

		private class AssetGroup
		{
			public string Target;
			public AssetProperty? Prop;
			public readonly IList<AssetEdit> Edits = [];
			public AssetLoad? Load;
			public readonly IList<AssetInclude> Includes = [];
			public string VarName;
			public bool isProperty;

			private AssetGroup(string Target)
			{
				this.Target = Target.MakeLocal();
				this.VarName = Target.ToVarname();
			}

			public AssetGroup(AssetProperty Prop, bool isProperty) : this(Prop.Asset)
			{
				this.Prop = Prop;
				this.isProperty = isProperty;
			}

			public AssetGroup(AssetLoad Load) : this(Load.Target)
			{
				this.Load = Load;
			}

			public AssetGroup(string Target, IList<AssetEdit> Edits) : this(Target)
			{
				this.Edits = Edits;
			}

			public AssetGroup(string Target, IList<AssetInclude> Includes) : this(Target)
			{
				this.Includes = Includes;
			}

			public bool HasAnyHandlers
				=> Prop?.Local is not null || Load is not null || Includes.Count > 0 || Edits.Count > 0;
		}

		private void Write(
			AssetEntryMethod entry, IReadOnlyList<AssetProperty> props, IReadOnlyList<AssetInclude> includes, 
			IReadOnlyList<AssetEdit> edits, IReadOnlyList<AssetLoad> loads, IReadOnlyList<AssetProperty> anons, SourceProductionContext ctx
		)
		{
			if (Template is null)
				return;

			var file = $"{entry.Space}.{entry.Type}.impl";
			string fullName = entry.fullName;
			List<AssetProperty> LocalProps = [];
			Dictionary<string, string> IncludeSources = [];
			Dictionary<string, AssetGroup> Groups = [];

			foreach (var p in props)
			{
				if (p.Type != fullName)
					continue;

				LocalProps.Add(p);
				if (Groups.TryGetValue(p.Asset, out var g))
				{
					g.Prop = p;
					g.isProperty = true;
					continue;
				}

				Groups.Add(p.Asset, new(p, true));
			}

			foreach (var e in edits)
			{
				if (e.Owner != fullName)
					continue;

				if (!Groups.TryGetValue(e.Target, out var g))
					Groups.Add(e.Target, new(e.Target, [e]));
				else
					g.Edits.Add(e);
			}

			foreach (var l in loads)
			{
				if (l.Owner != fullName)
					continue;

				if (!Groups.TryGetValue(l.Target, out var g))
					Groups.Add(l.Target, new(l));
				else
					g.Load = l;
			}

			foreach (var i in includes)
			{
				if (i.Owner != fullName)
					continue;

				IncludeSources[i.Source] = i.SourceVar;
				if (!Groups.TryGetValue(i.Target, out var g))
					Groups.Add(i.Target, new(i.Target, [i]));
				else
					g.Includes.Add(i);
			}

			foreach (var a in anons)
			{
				if (a.Type != fullName)
					continue;

				if (a.PropType.Length is 0)
					continue;

				if (Groups.TryGetValue(a.Asset, out var g))
				{
					if (g.Prop is AssetProperty p && g.isProperty)
						LocalProps.Remove(p);

					g.Prop = a;
					g.isProperty = false;
					continue;
				}

				Groups.Add(a.Asset, new(a, false));
			}

			var data = new TemplateData(entry, props, Groups.Values, IncludeSources);
			ctx.AddSource(file, Template.Render(new(data, options)));
		}
	}
}

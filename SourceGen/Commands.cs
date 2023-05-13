using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Text;

namespace Chireiden.TShock.SourceGen;

[Generator(LanguageNames.CSharp)]
public class CommandsGenerator : IIncrementalGenerator
{
    internal class ValueTree<V>
    {
        internal Dictionary<string, ValueTree<V>> Children { get; set; } = new Dictionary<string, ValueTree<V>>();
        internal Dictionary<string, V> Values { get; set; } = new Dictionary<string, V>();
        internal void Add(string key, V value)
        {
            var sk = key.Split('.');
            if (sk.Length == 1)
            {
                this.Values.Add(sk[0], value);
            }
            else
            {
                if (!this.Children.TryGetValue(sk[0], out var child))
                {
                    child = new ValueTree<V>();
                    this.Children.Add(sk[0], child);
                }
                child.Add(string.Join(".", sk.Skip(1)), value);
            }
        }
    }

    internal class DeclaredCommand
    {
        internal required AttributeSyntax AttributeSyntax { get; set; }
        internal required string? Namespace { get; set; }
        internal required string? ClassIdentifier { get; set; }
        internal required string? MethodIdentifier { get; set; }
    }

    private static bool FilterAttributes(SyntaxNode syntaxNode, CancellationToken _)
    {
        return syntaxNode is AttributeListSyntax m && m.Attributes.Count > 0;
    }

    private static DeclaredCommand? Transform(GeneratorSyntaxContext context, CancellationToken _)
    {
        var methodDeclarationSyntax = (context.Node.Parent as MethodDeclarationSyntax)!;
        foreach (var attributeSyntax in (context.Node as AttributeListSyntax)!.Attributes)
        {
            if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol)
            {
                var attr = attributeSymbol.ContainingType.ToDisplayString();
                if (attr == "Chireiden.TShock.CommandAttribute" || attr == "Chireiden.TShock.RelatedPermissionAttribute")
                {
                    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax!);
                    var decl = (methodDeclarationSyntax.Parent as ClassDeclarationSyntax)!;
                    return new DeclaredCommand
                    {
                        AttributeSyntax = attributeSyntax,
                        Namespace = methodSymbol?.ContainingType?.ContainingNamespace?.ToDisplayString(),
                        ClassIdentifier = decl.Identifier.ToString(),
                        MethodIdentifier = methodDeclarationSyntax.Identifier.ToString(),
                    };
                }
            }
        }

        return null;
    }

    private static StringBuilder Indent(StringBuilder sb, int value)
    {
        return sb.Append(new string(' ', value * 4));
    }

    private static void WriteCommands(ValueTree<List<string>> ca, ref StringBuilder sb, ref int indent)
    {
        foreach ((var cmdName, var aliases) in ca.Values)
        {
            Indent(sb, indent)
                .Append("public static string ")
                .Append(cmdName)
                .Append(" => ")
                .Append(aliases[0])
                .Append(";")
                .AppendLine();
        }
        foreach ((var childName, var sub) in ca.Children)
        {
            Indent(sb, indent).Append("public static partial class ").Append(childName).AppendLine();
            Indent(sb, indent).Append("{").AppendLine();
            indent += 1;
            WriteCommands(sub, ref sb, ref indent);
            indent -= 1;
            Indent(sb, indent).Append("}").AppendLine();
        }
    }

    private static void WriteAliases(ValueTree<List<string>> ca, ref StringBuilder sb, ref int indent)
    {
        foreach ((var cmdName, var aliases) in ca.Values)
        {
            Indent(sb, indent)
                .Append("public static List<string> ")
                .Append(cmdName)
                .Append(" => new List<string> { ")
                .Append(string.Join(", ", aliases))
                .Append(" };")
                .AppendLine();
        }
        foreach ((var childName, var sub) in ca.Children)
        {
            Indent(sb, indent).Append("public static partial class ").Append(childName).AppendLine();
            Indent(sb, indent).Append("{").AppendLine();
            indent += 1;
            WriteAliases(sub, ref sb, ref indent);
            indent -= 1;
            Indent(sb, indent).Append("}").AppendLine();
        }
    }

    private static void WritePermission(ValueTree<string> ca, ref StringBuilder sb, ref int indent)
    {
        foreach ((var cmdName, var aliases) in ca.Values)
        {
            Indent(sb, indent)
                .Append("public static string ")
                .Append(cmdName)
                .Append(" => ")
                .Append(aliases)
                .Append(";")
                .AppendLine();
        }
        foreach ((var childName, var sub) in ca.Children)
        {
            Indent(sb, indent).Append("public static partial class ").Append(childName).AppendLine();
            Indent(sb, indent).Append("{").AppendLine();
            indent += 1;
            WritePermission(sub, ref sb, ref indent);
            indent -= 1;
            Indent(sb, indent).Append("}").AppendLine();
        }
    }

    private static (Diagnostic? Diagnostic, (string Key, string Permission, (string AddCode, List<string> Aliases)? AddStyle)? Result) Extract(DeclaredCommand attribute, SemanticModel semanticModel)
    {
        var methodIdentifier = attribute.MethodIdentifier;
        var args = attribute.AttributeSyntax.ArgumentList!.Arguments.ToArray();
        foreach (var arg in args)
        {
            if (arg.NameColon != null)
            {
                return (Diagnostic.Create(new DiagnosticDescriptor("TSCG01", "Use positional arguments instead of named arguments", "Expecting positional arguments but got {0}", "", DiagnosticSeverity.Warning, true), arg.Expression.GetLocation(), arg.Expression.ToString()), null);
            }
        }
        if (args.Length < 2)
        {
            return (Diagnostic.Create(new DiagnosticDescriptor("TSCG03", "Incorrect Attribute Arguments", "Expecting at least two but got {0}", "", DiagnosticSeverity.Warning, true), args[0].Expression.GetLocation(), attribute.AttributeSyntax.ArgumentList.ToString()), null);
        }
        if (args[0].Expression is not LiteralExpressionSyntax les)
        {
            return (Diagnostic.Create(new DiagnosticDescriptor("TSCG02", "Non-literal value is used for attribute", "Expecting literal value but got {0}", "", DiagnosticSeverity.Warning, true), args[0].Expression.GetLocation(), args[0].Expression.ToString()), null);
        }
        var commandName = les.Token.ValueText;
        var commandPermission = args[1].Expression.ToString();
        var symbol = semanticModel.GetSymbolInfo(attribute.AttributeSyntax).Symbol;
        if (symbol is not IMethodSymbol attributeSymbol)
        {
            return (Diagnostic.Create(new DiagnosticDescriptor("TSCG04", "Unable to resolve known attribute", "Failed to get symbol of {0}", "", DiagnosticSeverity.Warning, true), args[0].Expression.GetLocation(), attribute.AttributeSyntax.ToString()), null);
        }
        if (attributeSymbol.ContainingType.ToDisplayString() == "Chireiden.TShock.RelatedPermissionAttribute")
        {
            return (null, (commandName, commandPermission, null));
        }
        var aliases = new List<string>();
        var extras = new List<string>();
        foreach (var arg in args.Skip(2))
        {
            if (arg.NameEquals != null)
            {
                if (arg.NameEquals.Name.Identifier.ValueText == "AllowServer")
                {
                    if (!bool.TryParse(arg.Expression.ToString(), out var r) || !r)
                    {
                        extras.Add("AllowServer = false");
                    }
                }
                else if (arg.NameEquals.Name.Identifier.ValueText == "DoLog")
                {
                    if (!bool.TryParse(arg.Expression.ToString(), out var r) || !r)
                    {
                        extras.Add("DoLog = false");
                    }
                }
                else if (arg.NameEquals.Name.Identifier.ValueText == "HelpText")
                {
                    extras.Add("HelpText = " + arg.Expression.ToString());
                }
            }
            else
            {
                aliases.Add(arg.Expression.ToString());
            }
        }
        var addCommand = $"Commands.ChatCommands.Add(new Command({commandPermission}, {methodIdentifier}, {string.Join(", ", aliases)})";
        if (extras.Any())
        {
            addCommand += $" {{ {string.Join(", ", extras)} }}";
        }
        addCommand += ");";
        return (null, (commandName, commandPermission, (addCommand, aliases)));
    }

    private static void Execute(SourceProductionContext context, (Compilation Compilation, ImmutableArray<DeclaredCommand?> Commands) values)
    {
        foreach (var group in values.Commands.Select(v => v!).GroupBy(m => $"{m.Namespace}.{m.ClassIdentifier}"))
        {
            var namespaceName = group.First().Namespace;
            var className = group.First().ClassIdentifier;
            var addCommand = new List<string>();
            var commandAliases = new ValueTree<List<string>>();
            var commandPermissions = new ValueTree<string>();

            foreach (var attribute in group)
            {
                var (diag, result) = Extract(attribute, values.Compilation.GetSemanticModel(attribute.AttributeSyntax.SyntaxTree));
                if (diag != null)
                {
                    context.ReportDiagnostic(diag);
                    continue;
                }
                var (key, permission, add) = result!.Value;
                commandPermissions.Add(key, permission);
                if (add.HasValue)
                {
                    var (ac, aliases) = add.Value;
                    addCommand.Add(ac);
                    commandAliases.Add(key, aliases);
                }
            }

            {
                var source = new StringBuilder();
                source.Append("// <auto-generated>").AppendLine();
                source.AppendLine();
                source.Append("using TShockAPI;").AppendLine();
                source.AppendLine();
                var indent = 0;
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    Indent(source, indent).Append("namespace ").Append(namespaceName).Append(";").AppendLine();
                }

                Indent(source, indent).Append("partial class ").Append(className).AppendLine();
                Indent(source, indent).Append("{").AppendLine();
                indent += 1;

                Indent(source, indent).Append("private void InitCommands()").AppendLine();
                Indent(source, indent).Append("{").AppendLine();
                indent += 1;
                foreach (var ac in addCommand)
                {
                    Indent(source, indent).Append(ac).AppendLine();
                }
                indent -= 1;
                Indent(source, indent).Append("}").AppendLine();
                indent -= 1;
                Indent(source, indent).Append("}").AppendLine();
                context.AddSource($"{className}.init.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
            }

            {
                var source = new StringBuilder();
                source.Append("// <auto-generated>").AppendLine();
                source.AppendLine();
                var indent = 0;
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    Indent(source, indent).Append("namespace ").Append(namespaceName).Append(".DefinedConsts;").AppendLine();
                }
                Indent(source, indent).Append("public static partial class Commands").AppendLine();
                Indent(source, indent).Append("{").AppendLine();
                indent += 1;
                WriteCommands(commandAliases, ref source, ref indent);
                indent -= 1;
                Indent(source, indent).Append("}").AppendLine();
                Indent(source, indent).Append("public static partial class CommandAliases").AppendLine();
                Indent(source, indent).Append("{").AppendLine();
                indent += 1;
                WriteAliases(commandAliases, ref source, ref indent);
                indent -= 1;
                Indent(source, indent).Append("}").AppendLine();
                Indent(source, indent).Append("public static partial class Permissions").AppendLine();
                Indent(source, indent).Append("{").AppendLine();
                indent += 1;
                WritePermission(commandPermissions, ref source, ref indent);
                indent -= 1;
                Indent(source, indent).Append("}").AppendLine();
                context.AddSource($"{namespaceName}.{className}.consts.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
            }
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        initContext.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "Attributes.g.cs",
            SourceText.From(@"using System;

namespace Chireiden.TShock
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CommandAttribute : Attribute
    {
        public bool AllowServer { get; set; }
        public bool DoLog { get; set; }
        public string HelpText { get; set; }

        public CommandAttribute(string key, string permission, string name)
        {
        }

        public CommandAttribute(string key, string permission, string name, params string[] aliases)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RelatedPermissionAttribute : Attribute
    {
        public RelatedPermissionAttribute(string key, string permission)
        {
        }
    }
}
", Encoding.UTF8)));

        var methods = initContext.SyntaxProvider.CreateSyntaxProvider(predicate: FilterAttributes, transform: Transform)
            .Where(static m => m is not null)
            .Collect();

        var combined = initContext.CompilationProvider.Combine(methods);
        initContext.RegisterSourceOutput(combined, Execute);
    }
}
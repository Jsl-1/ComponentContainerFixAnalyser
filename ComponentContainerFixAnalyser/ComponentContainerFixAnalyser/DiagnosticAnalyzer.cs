using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.ComponentModel;
using ComponentContainerFixAnalyser;

namespace ComponentContainerFixAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ComponentContainerFixAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ComponentContainerFixAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            context.RegisterCompilationAction(AnalyzeCompilation);

        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {

            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
            if (namedTypeSymbol.AllInterfaces.Any(x => x.MetadataName == "IComponent"))
            {
                if (namedTypeSymbol.MemberNames.Any(x => x == "components"))
                {


                    var componentMember = namedTypeSymbol.GetMembers("components").First();
                    if (componentMember.Kind == SymbolKind.Field && ((IFieldSymbol)componentMember).Type.Name == "IContainer")
                    {
                        var fields = namedTypeSymbol.GetMembers()
                            .Where(x => x.Kind == SymbolKind.Field).Cast<IFieldSymbol>();

                        foreach (var field in fields)
                        {
                            var fieldTypes = fields.Select(x => x.Type).OfType<INamedTypeSymbol>();
                            foreach (var fieldType in fieldTypes)
                            {
                                foreach (IMethodSymbol constructor in fieldType.Constructors)
                                {
                                    if (constructor.Parameters.Length == 1)
                                    {
                                        var parameter = constructor.Parameters[0];
                                        if (parameter.Type.Name == "IContainer")
                                        {

                                            var shouldWarn = true;
                                            var initializeComponentMEthods = namedTypeSymbol.GetMembers("InitializeComponent").OfType<IMethodSymbol>();
                                            foreach (var initializeComponentMEthod in initializeComponentMEthods)
                                            {

                                                foreach (var location in initializeComponentMEthod.Locations)
                                                {
                                                    var source = location.SourceTree.GetText();
                                                    if (source.ToString().Contains("this.components = new System.ComponentModel.Container();"))
                                                    {
                                                        shouldWarn = false;
                                                        break;
                                                    }
                                                }
                                            }
                                            if (shouldWarn)
                                            {
                                                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                                                context.ReportDiagnostic(diagnostic);
                                                return;
                                            }


                                        }
                                    }

                                }
                            }
                        }


                    }
                }
            }

        }
        private void AnalyzeCompilation(CompilationAnalysisContext context)
        {

        }
        //private static void AnalyzeSymbol(SymbolAnalysisContext context)
        //{
        //    if( context.Compilation.ObjectType.BaseType == typeof.ComponentMode)

        //    // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find


        //    var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        //    // Find just those named type symbols with names containing lowercase letters.
        //    if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
        //    {
        //        // For all such symbols, produce a diagnostic.
        //        var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

        //        context.ReportDiagnostic(diagnostic);
        //    }
        //}
    }
}

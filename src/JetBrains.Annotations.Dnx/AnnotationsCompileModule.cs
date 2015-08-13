using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Framework.Runtime.Roslyn;

namespace JetBrains.Annotations.Dnx
{
    public class AnnotationsCompileModule : ICompileModule
    {
        public void BeforeCompile(BeforeCompileContext context)
        {
            var projectName = context.ProjectContext.Name;

            var members = GetMembers(context.Compilation).ToImmutableArray();

            if (members.Length > 0)
            {
                var path = InitializeDirectory(context, projectName);

                var assembly = new Assembly(projectName, members);

                CreateDocument(path, assembly);
            }
        }

        public void AfterCompile(AfterCompileContext context)
        {
            // We don't care, we've done our job already ;)
        }

        #region Parsing

        private static IEnumerable<AnnotationMember> GetMembers(CSharpCompilation compilation)
        {
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree, true);

                var nodes = syntaxTree.GetRoot().DescendantNodes();

                foreach (var member in GetMembers(semanticModel, nodes))
                {
                    yield return member;
                }
            }
        }

        private static IEnumerable<AnnotationMember> GetMembers(SemanticModel semanticModel, IEnumerable<SyntaxNode> nodes)
        {
            foreach (var node in nodes)
            {
                var type = node as BaseTypeDeclarationSyntax;
                if (type != null)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(type);

                    AnnotationMember member;
                    if (TryGetMember(symbol, out member))
                    {
                        yield return member;
                    }

                    continue;
                }

                var method = node as MethodDeclarationSyntax;
                if (method != null)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(method);

                    AnnotationMember member;
                    if (TryGetMember(symbol, out member))
                    {
                        yield return member;
                    }

                    continue;
                }

                var constructor = node as ConstructorDeclarationSyntax;
                if (constructor != null)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(constructor);

                    AnnotationMember member;
                    if (TryGetMember(symbol, out member))
                    {
                        yield return member;
                    }

                    continue;
                }

                var property = node as PropertyDeclarationSyntax;
                if (property != null)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(property);

                    // Properties can yield multiple members (getter, setter and property itself)
                    var members = GetMembers(symbol).ToImmutableArray();

                    foreach (var member in members)
                    {
                        yield return member;
                    }

                    continue;
                }

                var field = node as FieldDeclarationSyntax;
                if (field != null)
                {
                    // Fields can have multi-declarations.
                    foreach (var variable in field.Declaration.Variables)
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(variable);

                        AnnotationMember member;
                        if (TryGetMember(symbol, out member))
                        {
                            yield return member;
                        }
                    }
                }
            }
        }

        private static bool TryGetMember(ISymbol symbol, out AnnotationMember member)
        {
            var attributes = GetAttributes(symbol).ToImmutableArray();

            if (ShouldIncludeInResult(attributes))
            {
                var memberName = GetMemberName(symbol);

                member = new AnnotationMember(memberName, attributes);
                return true;
            }

            member = null;
            return false;
        }

        private static IEnumerable<AnnotationMember> GetMembers(IPropertySymbol property)
        {
            var symbols = new ISymbol[] { property, property.GetMethod, property.SetMethod };

            foreach (var symbol in symbols)
            {
                AnnotationMember member;
                if (TryGetMember(symbol, out member))
                {
                    yield return member;
                }
            }
        }

        private static bool TryGetMember(IMethodSymbol method, out AnnotationMember member)
        {
            var attributes = GetAttributes(method).ToImmutableArray();

            var parameters = GetParameters(method).ToImmutableArray();

            if (ShouldIncludeInResult(attributes, parameters))
            {
                var memberName = GetMemberName(method);

                member = new AnnotationMember(memberName, attributes, parameters);
                return true;
            }

            member = null;
            return false;
        }

        private static IEnumerable<AnnotationAttribute> GetAttributes(ISymbol symbol)
        {
            foreach (var attributeData in symbol.GetAttributes())
            {
                AnnotationAttribute attribute;
                if (TryGetAttribute(attributeData, out attribute))
                {
                    yield return attribute;
                }
            }
        }

        private static IEnumerable<AnnotationParameter> GetParameters(IMethodSymbol method)
        {
            foreach (var parameter in method.Parameters)
            {
                var attributes = GetAttributes(parameter).ToImmutableArray();

                if (ShouldIncludeInResult(attributes))
                {
                    yield return new AnnotationParameter(parameter.Name, attributes);
                }
            }
        }

        private static string GetMemberName(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    return $"M:{symbol.ToDisplayString()}";
                case SymbolKind.Property:
                    return $"P:{symbol.ToDisplayString()}";
                case SymbolKind.NamedType:
                    return $"T:{symbol.ToDisplayString()}";
                case SymbolKind.Field:
                    return $"F:{symbol.ToDisplayString()}";
                default:
                    throw new NotSupportedException($"The symbol kind '{symbol.Kind}' is not supported.");
            }
        }

        private static bool ShouldIncludeInResult(ImmutableArray<AnnotationAttribute> attributes)
        {
            return ShouldIncludeInResult(attributes, ImmutableArray<AnnotationParameter>.Empty);
        }

        private static bool ShouldIncludeInResult(ImmutableArray<AnnotationAttribute> attributes, ImmutableArray<AnnotationParameter> parameters)
        {
            if (attributes.Length > 0)
            {
                return true;
            }

            foreach (var parameter in parameters)
            {
                if (parameter.Attributes.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetAttribute(AttributeData attributeData, out AnnotationAttribute attribute)
        {
            var @namespace = attributeData.AttributeClass.ContainingNamespace.ToDisplayString();

            if (!@namespace.Equals("JetBrains.Annotations"))
            {
                attribute = null;
                return false;
            }

            var arguments = attributeData.ConstructorArguments
                .Select(GetArgumentValue)
                .ToImmutableArray();

            var constructor = attributeData.AttributeConstructor.ToDisplayString();

            attribute = new AnnotationAttribute(constructor, arguments);
            return true;
        }

        private static string GetArgumentValue(TypedConstant typedConstant)
        {
            switch (typedConstant.Kind)
            {
                case TypedConstantKind.Error:
                case TypedConstantKind.Enum:
                case TypedConstantKind.Primitive:
                    return typedConstant.Value.ToString();
                case TypedConstantKind.Type:
                case TypedConstantKind.Array:
                    return typedConstant.ToCSharpString();
                default:
                    throw new ArgumentOutOfRangeException(nameof(typedConstant));
            }
        }

        #endregion

        #region Writing

        private static string InitializeDirectory(BeforeCompileContext context, string projectName)
        {
            var projectDirectory = context.ProjectContext.ProjectDirectory;

            var fileName = $"{projectName}.ExternalConfiguration.xml";

            var configuration = context.ProjectContext.Configuration;

            var binDirectory = Path.Combine(projectDirectory, "bin", configuration);

            Directory.CreateDirectory(binDirectory);

            return Path.Combine(binDirectory, fileName);
        }

        private static void CreateDocument(string path, Assembly assembly)
        {
            var members = assembly.Members.Select(CreateMemberElement).ToList();

            var assemblyElement = new XElement("assembly", members);

            assemblyElement.SetAttributeValue("name", assembly.Name);

            var document = new XDocument(assemblyElement);

            using (var stream = File.Create(path))
            {
                document.Save(stream);
            }
        }

        private static XElement CreateMemberElement(AnnotationMember member)
        {
            var attributes = member.Attributes.Select(CreateAttributeElement).ToList();

            var parameters = member.Parameters.Select(CreateParameterArgument).ToList();

            var memberElement = new XElement("member", attributes.Concat(parameters));

            memberElement.SetAttributeValue("name", member.Name);

            return memberElement;
        }

        private static XElement CreateParameterArgument(AnnotationParameter parameter)
        {
            var attributes = parameter.Attributes.Select(CreateAttributeElement).ToList();

            var parameterElement = new XElement("parameter", attributes);

            parameterElement.SetAttributeValue("name", parameter.Name);

            return parameterElement;
        }

        private static XElement CreateAttributeElement(AnnotationAttribute attribute)
        {
            var arguments = attribute.Arguments
                .Select(argument => new XElement("argument", argument))
                .ToList();

            var attributeElement = new XElement("attribute", arguments);

            attributeElement.SetAttributeValue("ctor", attribute.Constructor);

            return attributeElement;
        }

        #endregion

        #region Annotation Model

        private class Assembly
        {
            public Assembly(string name, ImmutableArray<AnnotationMember> members)
            {
                Name = name;
                Members = members;
            }

            public string Name { get; }

            public ImmutableArray<AnnotationMember> Members { get; }
        }

        private class AnnotationMember : AnnotationParameter
        {
            public AnnotationMember(string name, ImmutableArray<AnnotationAttribute> attributes)
                : this(name, attributes, ImmutableArray<AnnotationParameter>.Empty)
            {
            }

            public AnnotationMember(string name, ImmutableArray<AnnotationAttribute> attributes, ImmutableArray<AnnotationParameter> parameters)
                : base(name, attributes)
            {
                Parameters = parameters;
            }

            public ImmutableArray<AnnotationParameter> Parameters { get; }
        }

        private class AnnotationParameter
        {
            public AnnotationParameter(string name, ImmutableArray<AnnotationAttribute> attributes)
            {
                Name = name;
                Attributes = attributes;
            }

            public string Name { get; }

            public ImmutableArray<AnnotationAttribute> Attributes { get; }
        }

        private class AnnotationAttribute
        {
            public AnnotationAttribute(string constructor, ImmutableArray<string> arguments)
            {
                Constructor = constructor;
                Arguments = arguments;
            }

            public string Constructor { get; }

            public ImmutableArray<string> Arguments { get; }
        }

        #endregion
    }
}
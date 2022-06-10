using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MemberAccess
{
    /// <summary>
    /// adds upper-case public property members for all lower-case private variables found in a public class
    /// these articles helped in creating the source generator:
    /// https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/syntax-analysis
    /// https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/
    /// http://stevetalkscode.co.uk/debug-source-generators-with-vs2019-1610
    /// https://stackoverflow.com/questions/64926889/generate-code-for-classes-with-an-attribute
    /// https://andrewlock.net/creating-a-source-generator-part-5-finding-a-type-declarations-namespace-and-type-hierarchy/
    /// </summary>

    // Copy this attribute class into your project to make your code analyzer happy (If you wonder what might be the background, read
    /// https://andrewlock.net/creating-a-source-generator-part-7-solving-the-source-generator-marker-attribute-problem-part1/ ):
    [AttributeUsage(AttributeTargets.Class/* ToDo: | System.AttributeTargets.Struct */, AllowMultiple = false, Inherited = false)]
    public sealed class GeneratePropertiesForAllPrivateVariablesAttribute : Attribute
    {
        public GeneratePropertiesForAllPrivateVariablesAttribute(bool snakeCase2CamelCase = false)
        {
        }
    }

    [Generator]
    public class GeneratePropertiesForAllPrivateVariables : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                // Debugger.Launch();
            }
#endif

            var classesWithAttribute = context.Compilation.SyntaxTrees
                            .SelectMany(st => st.GetRoot()
                                    .DescendantNodes()
                                    .Where(n => n is ClassDeclarationSyntax)
                                    .Select(n => n as ClassDeclarationSyntax)
                                    .Where(r => r.AttributeLists
                                        .SelectMany(al => al.Attributes)
                                        .Any(a => a.Name.GetText().ToString() == "GeneratePropertiesForAllPrivateVariables")));



            foreach (var declaredClass in classesWithAttribute)
            {
                if (declaredClass.Members.Count > 0)
                {
                    // ToDo: Check for public partial class modifiers here
                    string className = declaredClass.Identifier.ToString();
                    var generatedClass = this.GenerateClass(declaredClass);

                    // get the attributes parameter to see if snake_case to CamelCase transformations has to be done
                    bool snakeCase2CamelCase = false;
                    var attributes = declaredClass.AttributeLists.SelectMany(al => al.Attributes)
                                        .Where(a => a.Name.GetText().ToString() == "GeneratePropertiesForAllPrivateVariables");
                    
                    if (attributes.Count() >= 1)
                    {
                        var attargs = attributes.ElementAt(0).ArgumentList;
                        if (attargs?.Arguments.Count() == 1 && attargs.Arguments.ElementAt(0).ToString() == "true")
                            snakeCase2CamelCase = true;
                    }

                    foreach (var classMember in declaredClass.Members)
                    {
                        // is field declaration?
                        if (classMember.Kind().ToString() == "FieldDeclaration")
                        {
                            var fieldDeclaration = (classMember as FieldDeclarationSyntax);

                            // and is private variable?
                            if (fieldDeclaration != null
                                && fieldDeclaration.Declaration is VariableDeclarationSyntax
                                && classMember.Modifiers.Where(token => token.Text == "public").Count() == 0)
                            {
                                var variableDeclaration = fieldDeclaration.Declaration as VariableDeclarationSyntax;

                                var declarator = variableDeclaration.DescendantNodes().Where(n => n is VariableDeclaratorSyntax).First() as VariableDeclaratorSyntax;
                                if (declarator != null)
                                {
                                    string privateIdentifier = declarator.Identifier.ToString();
                                    if (!string.IsNullOrEmpty(privateIdentifier))
                                    {
                                        // strip possible 'private' modifier
                                        foreach (var modifier in classMember.Modifiers)
                                            if (modifier.Text == "private")
                                                classMember.Modifiers.Remove(modifier);

                                        // get uppercase identifier for public accessors
                                        string? publicIdentifier = null;
                                        if (char.IsLower(privateIdentifier[0]))
                                            publicIdentifier = privateIdentifier[0].ToString().ToUpper() + privateIdentifier.Substring(1);
                                        else if (privateIdentifier[0] == '_')
                                            publicIdentifier = privateIdentifier[1].ToString().ToUpper() + privateIdentifier.Substring(2);
                                        else if (privateIdentifier.Substring(0, 2) == "m_")
                                            publicIdentifier = privateIdentifier[2].ToString().ToUpper() + privateIdentifier.Substring(3);

                                        if (snakeCase2CamelCase)  // e.g. first_name -> FirstName
                                            publicIdentifier = ToCamelCase(publicIdentifier);

                                        if (publicIdentifier != null)
                                        {
                                            // ToDo: didn't gigure out how to replace the private identifier with public one in the declarator
                                            // so using a hack with Sting.Replace in GeneratePropery :-/

                                            this.GeneratePropery(ref generatedClass, classMember.ToString(), privateIdentifier, publicIdentifier);
                                        }
                                    }

                                }
                            }
                        }
                    }

                    this.CloseClass(generatedClass);
                    context.AddSource($"{GetNamespace(declaredClass)}_{className}.g", SourceText.From(generatedClass.ToString(), Encoding.UTF8));
                }
            }
        }

        private string ToCamelCase(string str)
        {
            var words = str.Split(new[] { "_", " " }, StringSplitOptions.RemoveEmptyEntries);
            words = words
                .Select(word => char.ToUpper(word[0]) + word.Substring(1))
                .ToArray();
            return string.Join(string.Empty, words);
        }

        private StringBuilder GenerateClass(ClassDeclarationSyntax c)
        {
            var sb = new StringBuilder();

            sb.Append(@"
using System;
using System.Collections.Generic;

namespace ");
            sb.Append(GetNamespace(c));
            sb.Append(@"
{
    public partial class " + c.Identifier);

            sb.Append(@"
    {");

            return sb;
        }

        private void GeneratePropery(ref StringBuilder builder, string declaration, /*FieldDeclarationSyntax fds,*/ string privId, string pubId)
        {
            string replaceIdentifier = declaration.Replace(privId, pubId);  // ToDo: make sure that Replace only hits once -- or even better, find out
            string removeSemicolon = replaceIdentifier;                     //       how to replace elements of a syntax and pass that as argument.
            if (removeSemicolon[removeSemicolon.Length - 1] == ';')
                removeSemicolon = removeSemicolon.Substring(0, removeSemicolon.Length - 1);
            string decl = $"public {removeSemicolon}";
            string getter = $"get => {privId};";
            string setter = $"set => {privId} = value;";

            builder.AppendLine(@"
        " + decl + @"
        {
            " + getter + @"
            " + setter + @"
        }");
        }

        private void CloseClass(StringBuilder generatedClass)
        {
            generatedClass.Append(
@"    }
}");
        }

        // determine the namespace the class/enum/struct is declared in, if any
        private string GetNamespace(BaseTypeDeclarationSyntax syntax)
        {
            // If we don't have a namespace at all we'll return an empty string
            // This accounts for the "default namespace" case
            string nameSpace = string.Empty;

            // Get the containing syntax node for the type declaration
            // (could be a nested type, for example)
            SyntaxNode? potentialNamespaceParent = syntax.Parent;

            // Keep moving "out" of nested classes etc until we get to a namespace
            // or until we run out of parents
            while (potentialNamespaceParent != null &&
                    potentialNamespaceParent is not NamespaceDeclarationSyntax
                    && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
            {
                potentialNamespaceParent = potentialNamespaceParent.Parent;
            }

            // Build up the final namespace by looping until we no longer have a namespace declaration
            if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
            {
                // We have a namespace. Use that as the type
                nameSpace = namespaceParent.Name.ToString();

                // Keep moving "out" of the namespace declarations until we 
                // run out of nested namespace declarations
                while (true)
                {
                    if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                    {
                        break;
                    }

                    // Add the outer namespace as a prefix to the final namespace
                    nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                    namespaceParent = parent;
                }
            }

            // return the final namespace
            return nameSpace;
        }
    }
}
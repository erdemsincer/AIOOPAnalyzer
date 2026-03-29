using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Services
{
    public class CodeParserService
    {
        public CodeStructure Parse(string code)
        {
            var result = new CodeStructure();

            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var cls in classes)
            {
                var classInfo = new ClassInfo
                {
                    Name = cls.Identifier.ToString()
                };

                // Fields
                var fields = cls.Members.OfType<FieldDeclarationSyntax>();
                foreach (var field in fields)
                {
                    bool isPublic = field.Modifiers.Any(m => m.Kind() == SyntaxKind.PublicKeyword);
                    foreach (var variable in field.Declaration.Variables)
                    {
                        classInfo.Fields.Add(new Models.FieldInfo
                        {
                            Name = variable.Identifier.ToString(),
                            IsPublic = isPublic
                        });
                    }
                }

                // Properties
                var properties = cls.Members.OfType<PropertyDeclarationSyntax>();
                foreach (var prop in properties)
                {
                    classInfo.Properties.Add(prop.Identifier.ToString());
                }

                // Methods
                var methods = cls.Members.OfType<MethodDeclarationSyntax>();

                foreach (var method in methods)
                {
                    var methodInfo = new MethodInfo
                    {
                        Name = method.Identifier.ToString(),
                        // Use Kind() on the SyntaxToken to avoid depending on an extension method
                        IsVirtual = method.Modifiers.Any(m => m.Kind() == SyntaxKind.VirtualKeyword),
                        IsOverride = method.Modifiers.Any(m => m.Kind() == SyntaxKind.OverrideKeyword)
                    };

                    classInfo.Methods.Add(methodInfo);
                }

                // OBJECT CREATION (new ...)
                var creations = cls.DescendantNodes()
                    .OfType<ObjectCreationExpressionSyntax>();

                foreach (var creation in creations)
                {
                    classInfo.ObjectCreations.Add(creation.Type.ToString());
                }
                // INTERFACES & BASE CLASS
                if (cls.BaseList != null)
                {
                    foreach (var baseType in cls.BaseList.Types)
                    {
                        var typeName = baseType.Type.ToString();

                        // Interface isimleri genelde 'I' ile baslar (IService, IRepository vb.)
                        // veya root'ta interface declaration olarak tanimlanmis mi kontrol et
                        var isInterface = typeName.Length > 1 && typeName.StartsWith("I") && char.IsUpper(typeName[1]);

                        // Ayrica kaynak kodda interface olarak tanimlanmis mi bak
                        var interfaceDecls = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
                        if (interfaceDecls.Any(i => i.Identifier.ToString() == typeName))
                        {
                            isInterface = true;
                        }

                        if (isInterface)
                        {
                            classInfo.Interfaces.Add(typeName);
                        }
                        else
                        {
                            // Base class (kalitim)
                            classInfo.BaseClassName = typeName;
                        }
                    }
                }

                result.Classes.Add(classInfo);
            }

            return result;
        }
    }
}
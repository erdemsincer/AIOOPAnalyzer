using System.Collections.Generic;
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
                var fieldNames = new HashSet<string>();
                foreach (var field in fields)
                {
                    bool isPublic = field.Modifiers.Any(m => m.Kind() == SyntaxKind.PublicKeyword);
                    // Field tipini ReferencedTypes'a ekle (CK-CBO için)
                    var fieldTypeName = field.Declaration.Type.ToString();
                    AddReferencedType(classInfo, fieldTypeName);

                    foreach (var variable in field.Declaration.Variables)
                    {
                        var fname = variable.Identifier.ToString();
                        fieldNames.Add(fname);
                        classInfo.Fields.Add(new Models.FieldInfo
                        {
                            Name = fname,
                            IsPublic = isPublic
                        });
                    }
                }

                // Properties
                var properties = cls.Members.OfType<PropertyDeclarationSyntax>();
                foreach (var prop in properties)
                {
                    classInfo.Properties.Add(prop.Identifier.ToString());
                    // Property tipini ReferencedTypes'a ekle
                    AddReferencedType(classInfo, prop.Type.ToString());
                }

                // Methods
                var methods = cls.Members.OfType<MethodDeclarationSyntax>();

                foreach (var method in methods)
                {
                    var methodInfo = new MethodInfo
                    {
                        Name = method.Identifier.ToString(),
                        IsVirtual = method.Modifiers.Any(m => m.Kind() == SyntaxKind.VirtualKeyword),
                        IsOverride = method.Modifiers.Any(m => m.Kind() == SyntaxKind.OverrideKeyword),
                        Complexity = CalculateCyclomaticComplexity(method),
                        ParameterCount = method.ParameterList.Parameters.Count
                    };

                    // Parametre tiplerini ReferencedTypes'a ekle
                    foreach (var param in method.ParameterList.Parameters)
                    {
                        if (param.Type != null)
                            AddReferencedType(classInfo, param.Type.ToString());
                    }

                    // Return tipini ReferencedTypes'a ekle
                    if (method.ReturnType != null)
                        AddReferencedType(classInfo, method.ReturnType.ToString());

                    // Çağrılan metotları bul (RFC hesabı için)
                    var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();
                    foreach (var inv in invocations)
                    {
                        string calledName = "";
                        if (inv.Expression is MemberAccessExpressionSyntax memberAccess)
                            calledName = memberAccess.Name.ToString();
                        else if (inv.Expression is IdentifierNameSyntax identifier)
                            calledName = identifier.ToString();

                        if (!string.IsNullOrEmpty(calledName))
                            methodInfo.CalledMethods.Add(calledName);
                    }

                    // Erişilen alanları bul (LCOM hesabı için)
                    var identifiers = method.DescendantNodes().OfType<IdentifierNameSyntax>();
                    foreach (var id in identifiers)
                    {
                        var name = id.Identifier.ToString();
                        // Field isimlerinden biri mi? (_name veya name gibi)
                        if (fieldNames.Contains(name))
                            methodInfo.AccessedFields.Add(name);
                    }

                    // MemberAccess ile erişilen alanlar (this._name gibi)
                    var memberAccesses = method.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
                    foreach (var ma in memberAccesses)
                    {
                        if (ma.Expression is ThisExpressionSyntax)
                        {
                            var name = ma.Name.ToString();
                            if (fieldNames.Contains(name) && !methodInfo.AccessedFields.Contains(name))
                                methodInfo.AccessedFields.Add(name);
                        }
                    }

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

        /// <summary>
        /// Cyclomatic complexity hesaplar: if/else/for/while/switch/case/&&/|| + 1
        /// </summary>
        private int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
        {
            int complexity = 1; // Başlangıç

            var nodes = method.DescendantNodes();

            complexity += nodes.OfType<IfStatementSyntax>().Count();
            complexity += nodes.OfType<ElseClauseSyntax>().Count();
            complexity += nodes.OfType<ForStatementSyntax>().Count();
            complexity += nodes.OfType<ForEachStatementSyntax>().Count();
            complexity += nodes.OfType<WhileStatementSyntax>().Count();
            complexity += nodes.OfType<DoStatementSyntax>().Count();
            complexity += nodes.OfType<CaseSwitchLabelSyntax>().Count();
            complexity += nodes.OfType<CasePatternSwitchLabelSyntax>().Count();
            complexity += nodes.OfType<CatchClauseSyntax>().Count();
            complexity += nodes.OfType<ConditionalExpressionSyntax>().Count(); // ternary

            // && ve || operatörleri
            var binaryExpressions = nodes.OfType<BinaryExpressionSyntax>();
            foreach (var binary in binaryExpressions)
            {
                if (binary.OperatorToken.Kind() == SyntaxKind.AmpersandAmpersandToken ||
                    binary.OperatorToken.Kind() == SyntaxKind.BarBarToken)
                {
                    complexity++;
                }
            }

            return complexity;
        }

        /// <summary>
        /// Primitive olmayan tipleri ReferencedTypes'a ekler (CBO hesabı için).
        /// </summary>
        private void AddReferencedType(ClassInfo cls, string typeName)
        {
            // Generic tip varsa içindekileri de çıkar: List<string> → List, string
            var cleanName = typeName.Replace("?", "").Trim();

            // Primitive değilse ekle
            var primitives = new HashSet<string>
            {
                "string", "int", "double", "float", "bool", "decimal",
                "long", "short", "byte", "char", "void", "object",
                "String", "Int32", "Int64", "Double", "Boolean", "Decimal",
                "var", "dynamic"
            };

            if (!primitives.Contains(cleanName) &&
                !string.IsNullOrEmpty(cleanName) &&
                cleanName != cls.Name &&
                !cls.ReferencedTypes.Contains(cleanName))
            {
                cls.ReferencedTypes.Add(cleanName);
            }
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using At.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using atSyntax = At.Syntax;
using cs       = Microsoft.CodeAnalysis.CSharp;
using csSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace At
{
class SyntaxTreeConverter
{
    internal const string defaultClassName = "_";

    readonly AtSyntaxTree atSyntaxTree;
    private csSyntax.ClassDeclarationSyntax defaultClass;

    public SyntaxTreeConverter(AtSyntaxTree atSyntaxTree)
    {
       this.atSyntaxTree = atSyntaxTree;
       this.defaultClass = ClassDeclaration(defaultClassName).WithModifiers(TokenList(Token(PartialKeyword)));
    }
    
    public CSharpSyntaxTree ConvertToCSharpTree()
    {
        var atRoot = atSyntaxTree.GetRoot();
        var csRoot = CsharpCompilationUnitSyntax(atRoot);        
        var csharpTree = CSharpSyntaxTree.Create(csRoot);

        return (CSharpSyntaxTree) csharpTree;
    }

    csSyntax.CompilationUnitSyntax CsharpCompilationUnitSyntax(atSyntax.CompilationUnitSyntax atRoot)
    {
       //160316: this is mainly for making tests fail
       var error = atRoot.DescendantNodes().OfType<ErrorNode>().FirstOrDefault();
       if (error != null)
       {
            throw new Exception(error.GetDiagnostics().FirstOrDefault()?.Message ?? error.Message);
       }
    
       var csharpSyntax = CompilationUnit();
       var members      = new List<csSyntax.MemberDeclarationSyntax>();
       var statements   = new List<csSyntax.StatementSyntax>();

       processNodes(atRoot.ChildNodes(), members, statements);

       //class _ { <fields> static int Main() { <statements>; return 0; } }
       defaultClass = defaultClass.AddMembers(members.OfType<FieldDeclarationSyntax>().ToArray())
                                  .AddMembers(members.OfType<csSyntax.MethodDeclarationSyntax>().ToArray())
                                  .AddMembers(MethodDeclaration(ParseTypeName("int"),"Main")
                                                .AddModifiers(ParseToken("static"))
                                                .AddBodyStatements(statements.ToArray())
                                                
                                                .AddBodyStatements
                                                (
                                                    (
                                                        from ns  in members.OfType<csSyntax.NamespaceDeclarationSyntax>()
                                                        from cls in ns.Members.OfType<csSyntax.ClassDeclarationSyntax>()
                                                        where cls.Identifier.Text == ns.Name.ToString()
                                                        select ExpressionStatement(
                                                                    InvocationExpression(
                                                                            MemberAccessExpression
                                                                            (
                                                                                SimpleMemberAccessExpression,
                                                                                MemberAccessExpression(
                                                                                    SimpleMemberAccessExpression,
                                                                                    IdentifierName(ns.Name.ToString()),
                                                                                    IdentifierName(cls.Identifier.ToString())),
                                                                                IdentifierName("Init")))

                                                                            )
                                                    ).ToArray()
                                                )

                                                //return 0
                                                .AddBodyStatements(new StatementSyntax[]{ReturnStatement(LiteralExpression(NumericLiteralExpression,ParseToken("0")))}));
                                                         
       csharpSyntax = csharpSyntax.AddMembers(defaultClass)
                                  .AddMembers(members.Where(_=>!(_ is FieldDeclarationSyntax || _ is csSyntax.MethodDeclarationSyntax)).ToArray());
       return csharpSyntax;
    }

    //# processNodes()
    void processNodes(IEnumerable<AtSyntaxNode> nodes,List<MemberDeclarationSyntax> members,List<StatementSyntax> statements)
    {
       foreach(var node in nodes)
       {
          var d = node as DeclarationSyntax;
          if (d != null)
          {
            var cSharpDecl = MemberDeclarationSyntax(d);
            members.Add(cSharpDecl);
            continue;
          }

          //! SHOULD ALWAYS BE LAST
          var expr = node as atSyntax.ExpressionSyntax;
          if (expr != null)
          {
             var csExprStmt = ExpressionStatementSyntax(expr);
             statements.Add(csExprStmt);
             continue;
          }

          //throw new NotSupportedException(node.GetType().ToString());  
       }
    }

    csSyntax.ExpressionStatementSyntax ExpressionStatementSyntax(atSyntax.ExpressionSyntax expr)
    {
        return cs.SyntaxFactory.ExpressionStatement(ExpressionSyntax(expr));
    }

    csSyntax.ExpressionSyntax ExpressionSyntax(atSyntax.ExpressionSyntax expr)
    {
        var id = expr as atSyntax.NameSyntax;
        if (id != null) 
            return cs.SyntaxFactory.IdentifierName(id.Identifier.Text);

        throw new NotImplementedException(expr.GetType().ToString());
    }

    csSyntax.MemberDeclarationSyntax MemberDeclarationSyntax(DeclarationSyntax d)
    {
        var nsDecl = d as atSyntax.NamespaceDeclarationSyntax;
        if (nsDecl != null)
        {
            var csharpNs = NamespaceDeclarationSyntax(nsDecl);
            return csharpNs;
        }

        var classDecl = d as atSyntax.TypeDeclarationSyntax;
        if (classDecl != null)
        { 
            var csharpClass = ClassDeclarationSyntax(classDecl);
            return csharpClass;
        }

        var varDecl = d as atSyntax.VariableDeclarationSyntax;
        if (varDecl != null)
        {
            var csharpField = FieldDeclarationSyntax(varDecl);
            return csharpField;
        }

        var methodDecl = d as atSyntax.MethodDeclarationSyntax;
        if (methodDecl != null)
        {
            var csharpMethod = MethodDeclarationSyntax(methodDecl);
            return csharpMethod;
        }

        throw new NotSupportedException(d.GetType().ToString());
    }

    csSyntax.ClassDeclarationSyntax ClassDeclarationSyntax(At.Syntax.TypeDeclarationSyntax classDecl)
    {
        var classId = classDecl.Identifier;
        var csId = csIdentifer(classId);
        var csClass = ClassDeclaration(csId).AddModifiers(
                             ParseToken("public"));
        var csTypeParams = classDecl.TypeParameters.List.Select(_=>
                                cs.SyntaxFactory.TypeParameter(_.Text));

        if (csTypeParams != null) 
            csClass = csClass.AddTypeParameterListParameters(csTypeParams.ToArray());

        if (classDecl.BaseTypes != null) 
            csClass = csClass.AddBaseListTypes(classDecl.BaseTypes.List.Select(_=>
                            cs.SyntaxFactory.SimpleBaseType(
                                cs.SyntaxFactory.ParseTypeName(_.Text))).ToArray());

        if (classDecl.Members != null)
            csClass = csClass.AddMembers(classDecl.Members.Select(MemberDeclarationSyntax).ToArray());

        return csClass;
    }

     
    csSyntax.FieldDeclarationSyntax FieldDeclarationSyntax(atSyntax.VariableDeclarationSyntax varDecl)
    {
        var fieldId = varDecl.Identifier;
        var csId = csIdentifer(fieldId);        
        var csVarDeclr = cs.SyntaxFactory.VariableDeclarator(csId);
        var csVarDecl = cs.SyntaxFactory.VariableDeclaration(cs.SyntaxFactory.ParseTypeName(varDecl.Type?.Text ?? "System.Object"))
                            .AddVariables(csVarDeclr);
        var csField = cs.SyntaxFactory.FieldDeclaration(csVarDecl)
                                                .AddModifiers(cs.SyntaxFactory.ParseToken("public"),cs.SyntaxFactory.ParseToken("static"));
        
        return csField;
    }

    csSyntax.MethodDeclarationSyntax MethodDeclarationSyntax(atSyntax.MethodDeclarationSyntax methodDecl)
    {
        var methodId = methodDecl.Identifier;
        var returnType = methodDecl.ReturnType != null
                            ? cs.SyntaxFactory.ParseTypeName(methodDecl.ReturnType.Text)
                            : cs.SyntaxFactory.ParseTypeName("System.Object");
        var csMethod = cs.SyntaxFactory.MethodDeclaration(returnType,csIdentifer(methodId))
                                            .AddModifiers(ParseToken("public"))
                                            .AddBodyStatements(ParseStatement("return null;"));
                            

        return csMethod;
    }

    csSyntax.NamespaceDeclarationSyntax NamespaceDeclarationSyntax(atSyntax.NamespaceDeclarationSyntax nsDecl)
    {
        var nsId = nsDecl.Identifier;
        var csId = csIdentifer(nsId);         
        var members    = new List<csSyntax.MemberDeclarationSyntax>();
        var statements = new List<csSyntax.StatementSyntax>();

       processNodes(nsDecl.Members, members, statements);

       //class _ { <fields> static int Main() { <statements>; return 0; } }
       var defaultClass = ClassDeclaration(nsId.Text).WithModifiers(TokenList(Token(PartialKeyword)))
                                  .AddMembers(members.OfType<FieldDeclarationSyntax>().ToArray())
                                  .AddMembers(members.OfType<csSyntax.MethodDeclarationSyntax>().ToArray())
                                  .AddMembers(MethodDeclaration(PredefinedType(Token(VoidKeyword)),"Init")
                                                .AddModifiers(Token(InternalKeyword),Token(StaticKeyword))
                                                .AddBodyStatements(statements.ToArray()));
                                                         
        var csNs = cs.SyntaxFactory.NamespaceDeclaration(IdentifierName(csId))
                                         .AddMembers(defaultClass)
                                         .AddMembers(members.Where(_=>!(_ is FieldDeclarationSyntax || _ is csSyntax.MethodDeclarationSyntax)).ToArray());
        
        return csNs;
    }

    SyntaxToken csIdentifer(AtToken classId) => cs.SyntaxFactory.Identifier(lTrivia(classId),classId.Text,tTrivia(classId));       
    SyntaxTriviaList lTrivia(AtToken token)  => cs.SyntaxFactory.ParseLeadingTrivia(string.Join("",token.leadingTrivia.Select(_=>_.FullText)));
    SyntaxTriviaList tTrivia(AtToken token)  => cs.SyntaxFactory.ParseTrailingTrivia(string.Join("",token.trailingTrivia.Select(_=>_.FullText)));
}
}

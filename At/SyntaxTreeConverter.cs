﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using At.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharp = Microsoft.CodeAnalysis.CSharp;

namespace At
{
class SyntaxTreeConverter
{
    readonly AtSyntaxTree atSyntaxTree;

    public SyntaxTreeConverter(AtSyntaxTree atSyntaxTree)
    {
       this.atSyntaxTree = atSyntaxTree;
    }

    
    public CSharpSyntaxTree ConvertToCSharpTree()
    {
        var atRoot = atSyntaxTree.GetRoot();
        var csRoot = CsharpCompilationUnitSyntax(atRoot);        
        var csharpTree = CSharpSyntaxTree.Create(csRoot);

        return (CSharpSyntaxTree) csharpTree;
    }

    CSharp.Syntax.CompilationUnitSyntax CsharpCompilationUnitSyntax(At.Syntax.CompilationUnitSyntax atRoot)
    {
       var csharpSyntax = CSharp.SyntaxFactory.CompilationUnit();
       
       var members    = new List<CSharp.Syntax.MemberDeclarationSyntax>();
       var statements = new List<CSharp.Syntax.StatementSyntax>();

       foreach(var node in atRoot.Nodes)
       {
          var d = node as DeclarationSyntax;
          if (d != null)
          {
            var cSharpDecl = MemberDeclarationSyntax(d);
            members.Add(cSharpDecl);
            continue;
          }

          var expr = node as At.Syntax.ExpressionSyntax;
          if (expr != null)
          {
             var csExprStmt = ExpressionStatementSyntax(expr);
             statements.Add(csExprStmt);
             continue;
          }

          throw new NotSupportedException(node.ToString());       
       }

       //class _ { static int Main() { <statements>; return 0; } }
       var defaultClass = CSharp.SyntaxFactory.ClassDeclaration("_")
                                .AddMembers(CSharp.SyntaxFactory.MethodDeclaration(CSharp.SyntaxFactory.ParseTypeName("int"),"Main")
                                                  .AddModifiers(CSharp.SyntaxFactory.ParseToken("static"))
                                                  .AddBodyStatements(statements.ToArray())
                                                  //return 0
                                                  .AddBodyStatements(new StatementSyntax[]{CSharp.SyntaxFactory.ReturnStatement(CSharp.SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,CSharp.SyntaxFactory.ParseToken("0")))}));
                                
       csharpSyntax = csharpSyntax.AddMembers(defaultClass).AddMembers(members.ToArray());
       return csharpSyntax;
    }
    
    CSharp.Syntax.ExpressionStatementSyntax ExpressionStatementSyntax(At.Syntax.ExpressionSyntax expr)
    {
        return CSharp.SyntaxFactory.ExpressionStatement(ExpressionSyntax(expr));
    }

    CSharp.Syntax.ExpressionSyntax ExpressionSyntax(At.Syntax.ExpressionSyntax expr)
    {
        var id = expr as IdentifierSyntax;
        if (id != null) return CSharp.SyntaxFactory.IdentifierName(id.Identifier);

        throw new NotImplementedException(expr.ToString());
    }

    CSharp.Syntax.MemberDeclarationSyntax MemberDeclarationSyntax(DeclarationSyntax d)
    {
        var classDecl = d as At.Syntax.ClassDeclarationSyntax;
        if (classDecl != null)
        { 
           var csharpClass = ClassDeclarationSyntax(classDecl);
           return csharpClass;
        }

        throw new NotSupportedException(d.ToString());
    }

    CSharp.Syntax.ClassDeclarationSyntax ClassDeclarationSyntax(At.Syntax.ClassDeclarationSyntax classDecl)
    {
        var csClass = CSharp.SyntaxFactory.ClassDeclaration(classDecl.Name);
        var csTypeParams = classDecl.TypeParameterList.Parameters.Nodes().Select(_=>CSharp.SyntaxFactory.TypeParameter(_.Text));
        if (csTypeParams != null) 
            csClass = csClass.AddTypeParameterListParameters(csTypeParams.ToArray());
        if (classDecl.BaseClass != null) 
        csClass = csClass.AddBaseListTypes(CSharp.SyntaxFactory.SimpleBaseType(CSharp.SyntaxFactory.ParseTypeName(classDecl.BaseClass)));
        return csClass;
    }
}
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using At.Syntax;

namespace At
{
//SyntaxTree + ParsedSyntaxTree
public class AtSyntaxTree
{
    readonly CompilationUnitSyntax compilationUnit;
    readonly AtSourceText text;

    public AtSyntaxTree(AtSourceText text, CompilationUnitSyntax compilationUnit)
    {
        this.text = text;
        this.compilationUnit = compilationUnit;
    }

    //GetDiagnostics()
    public IEnumerable<AtDiagnostic> GetDiagnostics()
    {
       return this.compilationUnit.GetDiagnostics().Concat(this.compilationUnit.DescendantNodes(null,true).OfType<ErrorNode>().SelectMany(_=>_.Diagnostics));
    }

    //GetRoot()
    public CompilationUnitSyntax GetRoot()
    {
        return this.compilationUnit;
    }

    //ParseText(string)
    public static AtSyntaxTree ParseText(string text)
    {
         return parseText(AtSourceText.From(text));
    }

    public override string ToString()
    {
        return $"[{compilationUnit.ToString()}]";
    }

    //parseText(AtSourceText)
    static AtSyntaxTree parseText(AtSourceText text)
    {
        using (var lexer  = new AtLexer(text))
        using (var parser = new AtParser(lexer))
        {
            var compilationUnit = parser.ParseCompilationUnit();
            var tree = new AtSyntaxTree(text, compilationUnit);
            //tree.VerifySource();
            return tree;
        }
    }
}
}
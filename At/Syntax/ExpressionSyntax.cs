﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace At.Syntax 
{
public abstract class ExpressionSyntax : AtSyntaxNode
{
    protected ExpressionSyntax(IEnumerable<AtSyntaxNode> nodes, IExpressionSource expressionSource, IEnumerable<AtDiagnostic> diagnostics) : base(nodes,diagnostics)
    {
        ExpressionSource = expressionSource;
    }

    //The expression definition that created this expression
    public IExpressionSource ExpressionSource {get;}

    //e.g., semicolon
    public ExpressionSyntax WithEndToken(AtToken endToken)
    {
        nodes.append(endToken);
        _text = null;
        return this;
    }

    public override bool MatchesPattern(SyntaxPattern pattern)
    {
        return     (pattern.Text=="Expr" || pattern.Text=="Node")
                && pattern.Token1==null
                && pattern.Token2==null
                && pattern.Content==null;
    }

    public override IEnumerable<string> PatternStrings()
    {
        yield return "Expr";
        yield return "Node";
    }

    public override string PatternName
    {
        get
        {
            if (_PatternName==null)
            {
                var name = base.PatternName;
                _PatternName=   name.EndsWith("ExpressionSyntax") ? name.Substring(0,name.Length-16)
                              : name.EndsWith("Syntax") ? name.Substring(0,name.Length-6)
                              : name;
            }

            return _PatternName;
        }
    }
}
}
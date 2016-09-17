﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using At.Syntax;

namespace At
{
//SyntaxNode + CSharpSyntaxNode + GreenNode
public abstract class AtSyntaxNode
{
    internal readonly AtSyntaxList<AtSyntaxNode> nodes;
    readonly ImmutableArray<AtDiagnostic> diagnostics;

    protected AtSyntaxNode(IEnumerable<AtSyntaxNode> nodes, IEnumerable<AtDiagnostic> diagnostics, bool isMissing = false) 
    { 
        this.nodes = new AtSyntaxList<AtSyntaxNode>(this,nodes);
        this.diagnostics = ImmutableArray<AtDiagnostic>.Empty;
        
        if (diagnostics != null) 
            this.diagnostics = this.diagnostics.AddRange(diagnostics.Where(_=>_!=null));
    
        IsMissing = isMissing;
    }

    public AtSyntaxNode   Parent {get; internal set;}
    public virtual bool   IsTrivia => false;
    public virtual bool   IsToken  => false;
    public virtual int    Position => nodes[0].Position;
    public virtual string Text => FullText.Trim();

    /// <summary>True if absent from source.</summary>
    public bool IsMissing {get; internal set;}

    /// <summary>Includes trivia</summary>
    public virtual string FullText
    {
        get
        {
            if (_text == null)
                _text = string.Concat(nodes.Select(_=>_?.FullText));

            return _text;
        }
    } internal string _text;

    public virtual string PatternName
    {
        get
        {
            if (_PatternName==null)
            {
                var t = GetType();
                _PatternName = (t.Assembly==typeof(AtSyntaxNode).Assembly)
                                ? t.Name
                                : t.FullName;
            }

            return _PatternName;
        }
    } internal string _PatternName;

    public AtToken AsToken() => this as AtToken;
    public virtual AtSyntaxNode Clone() => (AtSyntaxNode) MemberwiseClone();
    public override string ToString() => FullText;
    public IEnumerable<AtDiagnostic> GetDiagnostics() => diagnostics;
    public IReadOnlyList<AtSyntaxNode> ChildNodes(bool includeTokens = false) => nodes.Where(_=>includeTokens || !_.IsToken).ToImmutableList(); 
    public IEnumerable<AtSyntaxNode> DescendantNodes(Func<AtSyntaxNode,bool> filter = null,bool includeTokens = false) => nodesRecursive(this,includeTokens,filter);

    public static IEnumerable<string> PatternStrings(IReadOnlyList<AtSyntaxNode> nodes)
        => getPatternStringsRecursive(0,nodes);

    public virtual bool MatchesPattern(SyntaxPattern pattern)
    {
        return     pattern.Text=="Node"
                && pattern.Token1==null
                && pattern.Token2==null
                && pattern.Content==null;
    }

    public static bool MatchesPattern(SyntaxPattern pattern, params AtSyntaxNode[] nodes)
    {
        if (nodes?.Length > 1)
        {
            if (pattern.Text != null || pattern.Content?.Length != nodes?.Length)
                return false;

            return MatchesPatterns(pattern.Content,nodes);
        }
        else if (nodes?.Length == 1)
        {
            return nodes[0].MatchesPattern(pattern);
        }
        else //nodes.Length < 1
        {
            throw new ArgumentException("nodes.Length must be more than 1",nameof(nodes));
        }
    }

    public static bool MatchesPatterns(SyntaxPattern[] patterns, IReadOnlyList<AtSyntaxNode> nodes)
    {
        if (patterns.Length != nodes.Count)
            return false;
        
        for(int i=0; i < nodes.Count; ++i)
                if (!nodes[i].MatchesPattern(patterns[i]))
                    return false;

        return true;
    }

    /// <summary>(...from most specific)</summary>
    public virtual IEnumerable<string> PatternStrings()
    {
        yield return "Node";
    }

    static IEnumerable<string> getPatternStringsRecursive(int index, IReadOnlyList<AtSyntaxNode> nodes)
    {
        if (nodes.Count <= index)
            yield break;

        foreach(var x in nodes[index].PatternStrings())
        {
            var ys = getPatternStringsRecursive(index+1,nodes);

            if (ys.Any())
                foreach(var y in ys)
                    yield return $"{x},{y}";
            else
                yield return x;           
        }
    }

    static IEnumerable<AtSyntaxNode> nodesRecursive(AtSyntaxNode parent, bool includeTokens,Func<AtSyntaxNode,bool> predicate)
    {
        foreach(var node in parent?.nodes.Where
        (ifNode=>  
               (ifNode!=null)
            && (includeTokens   || !ifNode.IsToken) 
            && (predicate==null || predicate(ifNode)))) {

                yield return node;

                foreach(var descendant in nodesRecursive(node,includeTokens, predicate))
                    yield return descendant;
        }
    }
}
}
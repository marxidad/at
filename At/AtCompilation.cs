﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace At
{
public class AtCompilation
{
    readonly string assemblyName;
    readonly SyntaxAndDeclarationManager _syntaxAndDeclarations;

    AtCompilation(//string assemblyName,
                  //...
                  SyntaxAndDeclarationManager syntaxAndDeclarations = null)
    {
        this._syntaxAndDeclarations = syntaxAndDeclarations ?? 
                                      new SyntaxAndDeclarationManager(new ImmutableArray<AtSyntaxTree>());
    }

    public static AtCompilation Create(AtSyntaxTree[] trees)
    {
        var compilation = new AtCompilation();

        if (trees != null)
            compilation = compilation.AddSyntaxTrees(new ImmutableArray<AtSyntaxTree>().AddRange(trees));

        return compilation;
    }

    public AtCompilation AddSyntaxTrees(ImmutableArray<AtSyntaxTree> trees) 
    {
        if (trees == null)
        {
            throw new ArgumentNullException(nameof(trees));
        }    

        if (!trees.Any())
            return this;

         var syntaxAndDeclarations = _syntaxAndDeclarations;
         syntaxAndDeclarations = syntaxAndDeclarations.AddSyntaxTrees(trees);
         return Update(syntaxAndDeclarations);
    }

    private AtCompilation Update(SyntaxAndDeclarationManager syntaxAndDeclarations)
    {
        return new AtCompilation(syntaxAndDeclarations);
    }

    public AtEmitResult Emit(Stream peStream, CancellationToken cancellationToken = default(CancellationToken)) 
    {
        if (peStream == null)
        {
            throw new ArgumentNullException(nameof(peStream));
        }
 
        if (!peStream.CanWrite)
        {
            throw new ArgumentException("peStream must support write", nameof(peStream));
        }
 
        var cSharpCompilation = CSharpCompilation.Create(  assemblyName
                                                          ,csharpSyntaxTrees(_syntaxAndDeclarations.syntaxTrees)
                                                          ,references: null
                                                          ,options: null);

        var result = cSharpCompilation.Emit(peStream, cancellationToken: cancellationToken);
        return atEmitREsult(result);
    }

    private IEnumerable<CSharpSyntaxTree> csharpSyntaxTrees(ImmutableArray<AtSyntaxTree> atSyntaxTrees)
    {
       
        foreach(var tree in atSyntaxTrees)
        { 
           var converter = new SyntaxTreeConverter(atSyntaxTree: tree);        
           yield return converter.ConvertToCSharpTree();
        }
    }

    AtEmitResult atEmitREsult(EmitResult result)
    {
        return new AtEmitResult(  result.Success
                                 ,result.Diagnostics);
    }
}
}
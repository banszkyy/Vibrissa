namespace LanguageCore.Compiler;

[Flags]
public enum StatementWalkerOptions
{
    None = 0x0,
    IncludingLambdaBodies = 0x2,
}

public static partial class StatementWalkerFilter
{
    public static bool All(CompiledStatement _) => true;
    public static Predicate<CompiledStatement> In(Uri file) => v => v.Location.File == file;
}

public static partial class StatementWalker
{
    public static IEnumerable<CompiledFunction> OutgoingCalls(IEnumerable<CompiledFunction> functions, IEnumerable<CompiledStatement> statements, Predicate<CompiledStatement> predicate)
        => statements.SelectMany(v => OutgoingCalls(functions, v, predicate));

    public static IEnumerable<CompiledFunction> OutgoingCalls(IEnumerable<CompiledFunction> functions, CompiledStatement statement, Predicate<CompiledStatement> predicate)
    {
        IEnumerable<CompiledFunction> TryFunctionCallback(ICompiledFunctionDefinition? function)
        {
            if (function is null) return Enumerable.Empty<CompiledFunction>();
            CompiledFunction? f = functions.FirstOrDefault(w => Utils.ReferenceEquals(w.Function, function) && StatementCompiler.TypeArgumentsEquals(w.TypeArguments, null));
            if (f is null) return Enumerable.Empty<CompiledFunction>();
            return Enumerable.Repeat(f, 1);
        }

        foreach (CompiledStatement item in EnumerateNodes(statement, predicate))
        {
            switch (item)
            {
                case CompiledCleanup v:
                    foreach (CompiledFunction w in TryFunctionCallback(v.Deallocator?.Template)) yield return w;
                    foreach (CompiledFunction w in TryFunctionCallback(v.Deinitializer?.Template)) yield return w;
                    break;
                case CompiledFunctionCall v:
                    foreach (CompiledFunction w in TryFunctionCallback(v.Function.Template)) yield return w;
                    break;
                case CompiledExternalFunctionCall v:
                    foreach (CompiledFunction w in TryFunctionCallback(v.Declaration)) yield return w;
                    break;
                case CompiledHeapAllocation v:
                    foreach (CompiledFunction w in TryFunctionCallback(v.Allocator.Template)) yield return w;
                    break;
                case CompiledConstructorCall v:
                    foreach (CompiledFunction w in TryFunctionCallback(v.Function.Template)) yield return w;
                    break;
                case CompiledDesctructorCall v:
                    foreach (CompiledFunction w in TryFunctionCallback(v.Function.Template)) yield return w;
                    break;
                case CompiledFunctionReference v:
                    foreach (CompiledFunction w in TryFunctionCallback(v.Function.Template)) yield return w;
                    break;
            }
        }
    }

    public static IEnumerable<CompiledStatement> EnumerateNodes(CompilerResult compilerResult, Predicate<CompiledStatement> predicate)
    {
        if (!compilerResult.Statements.IsDefault)
        {
            foreach (CompiledStatement w in EnumerateNodes(compilerResult.Statements, predicate, StatementWalkerOptions.IncludingLambdaBodies)) yield return w;
        }

        if (!compilerResult.Enums.IsDefault)
        {
            foreach (CompiledExpression? item in compilerResult.Enums.SelectMany(v => v.Members).Select(v => v.Value))
            {
                foreach (CompiledStatement w in EnumerateNodes(item, predicate, StatementWalkerOptions.IncludingLambdaBodies)) yield return w;
            }
        }

        if (!compilerResult.Functions.IsDefault)
        {
            foreach (CompiledFunction function in compilerResult.Functions)
            {
                foreach (CompiledStatement w in EnumerateNodes(function.Body, predicate, StatementWalkerOptions.IncludingLambdaBodies)) yield return w;
            }
        }
    }

    public static IEnumerable<CompiledStatement> EnumerateNodes(IEnumerable<CompiledStatement>? statement, Predicate<CompiledStatement> predicate, StatementWalkerOptions options = StatementWalkerOptions.None)
    {
        if (statement is null) yield break;
        foreach (CompiledStatement item in statement)
        {
            foreach (CompiledStatement w in EnumerateNodes(item, predicate, options)) yield return w;
        }
    }
    public static IEnumerable<CompiledStatement> EnumerateNodes(CompiledStatement? statement, Predicate<CompiledStatement> predicate, StatementWalkerOptions options = StatementWalkerOptions.None)
    {
        if (statement is null) yield break;
        if (!predicate(statement)) yield break;
        yield return statement;

        switch (statement)
        {
            case CompiledTypeExpression v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v, predicate, options)) yield return w;
                break;
            }
            case CompiledDummyExpression v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Statement, predicate, options)) yield return w;
                break;
            }
            case CompiledList v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Values, predicate, options)) yield return w;
                break;
            }
            case CompiledRuntimeCall v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Function, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Arguments, predicate, options)) yield return w;
                break;
            }
            case CompiledFunctionCall v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Arguments, predicate, options)) yield return w;
                break;
            }
            case CompiledExternalFunctionCall v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Arguments, predicate, options)) yield return w;
                break;
            }
            case CompiledSizeof v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Of, predicate, options)) yield return w;
                break;
            }
            case CompiledArgument v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Value, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Cleanup, predicate, options)) yield return w;
                break;
            }
            case CompiledBinaryOperatorCall v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Left, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Right, predicate, options)) yield return w;
                break;
            }
            case CompiledUnaryOperatorCall v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Expression, predicate, options)) yield return w;
                break;
            }
            case CompiledConstantValue v:
            {
                break;
            }
            case CompiledGetReference v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Of, predicate, options)) yield return w;
                break;
            }
            case CompiledDereference v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Address, predicate, options)) yield return w;
                break;
            }
            case CompiledStackAllocation v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.TypeExpression, predicate, options)) yield return w;
                break;
            }
            case CompiledHeapAllocation v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.TypeExpression, predicate, options)) yield return w;
                break;
            }
            case CompiledConstructorCall v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Object, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Arguments, predicate, options)) yield return w;
                break;
            }
            case CompiledDesctructorCall v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Value, predicate, options)) yield return w;
                break;
            }
            case CompiledCast v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Value, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.TypeExpression, predicate, options)) yield return w;
                break;
            }
            case CompiledReinterpretation v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Value, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.TypeExpression, predicate, options)) yield return w;
                break;
            }
            case CompiledElementAccess v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Base, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Index, predicate, options)) yield return w;
                break;
            }
            case CompiledVariableAccess: break;
            case CompiledExpressionVariableAccess: break;
            case CompiledParameterAccess: break;
            case CompiledFieldAccess v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Object, predicate, options)) yield return w;
                break;
            }
            case CompiledRegisterAccess: break;
            case CompiledString v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Allocator, predicate, options)) yield return w;
                break;
            }
            case CompiledStackString: break;
            case CompiledFunctionReference: break;
            case CompiledLabelReference: break;
            case CompiledCompilerVariableAccess: break;
            case CompiledLambda v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Allocator, predicate, options)) yield return w;
                if (options.HasFlag(StatementWalkerOptions.IncludingLambdaBodies))
                {
                    foreach (CompiledStatement w in EnumerateNodes(v.Block, predicate, options)) yield return w;
                }
                break;
            }
            case CompiledEnumMemberAccess v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.EnumMember.Value, predicate, options)) yield return w;
                break;
            }
            case CompiledMeowExpression v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Statements, predicate, options)) yield return w;
                break;
            }
            case CompiledEmptyStatement: break;
            case CompiledBlock v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Statements, predicate, options)) yield return w;
                break;
            }
            case CompiledIf v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Condition, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Body, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Next, predicate, options)) yield return w;
                break;
            }
            case CompiledElse v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Body, predicate, options)) yield return w;
                break;
            }
            case CompiledVariableDefinition v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.TypeExpression, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.InitialValue, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Cleanup, predicate, options)) yield return w;
                break;
            }
            case CompiledCrash v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Value, predicate, options)) yield return w;
                break;
            }
            case CompiledDelete v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Value, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Cleanup, predicate, options)) yield return w;
                break;
            }
            case CompiledReturn v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Value, predicate, options)) yield return w;
                break;
            }
            case CompiledBreak: break;
            case CompiledGoto v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Value, predicate, options)) yield return w;
                break;
            }
            case CompiledLabelDeclaration: break;
            case CompiledWhileLoop v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Condition, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Body, predicate, options)) yield return w;
                break;
            }
            case CompiledForLoop v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Initialization, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Condition, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Step, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Body, predicate, options)) yield return w;
                break;
            }
            case CompiledSetter v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Target, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Value, predicate, options)) yield return w;
                break;
            }
            case CompiledCleanup: break;
            default: throw new NotImplementedException(statement.GetType().Name);
        }
    }
    public static IEnumerable<CompiledStatement> EnumerateNodes(CompiledTypeExpression? statement, Predicate<CompiledStatement> predicate, StatementWalkerOptions options = StatementWalkerOptions.None)
    {
        if (statement is null) yield break;
        if (!predicate(statement)) yield break;
        yield return statement;

        switch (statement)
        {
            case CompiledAliasTypeExpression v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Value, predicate, options)) yield return w;
                break;
            }
            case CompiledEnumTypeExpression: break;
            case CompiledArrayTypeExpression v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.Of, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Length, predicate, options)) yield return w;
                break;
            }
            case CompiledFunctionTypeExpression v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.ReturnType, predicate, options)) yield return w;
                foreach (CompiledStatement w in EnumerateNodes(v.Parameters, predicate, options)) yield return w;
                break;
            }
            case CompiledPointerTypeExpression v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.To, predicate, options)) yield return w;
                break;
            }
            case CompiledReferenceTypeExpression v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.To, predicate, options)) yield return w;
                break;
            }
            case CompiledStructTypeExpression v:
            {
                foreach (CompiledStatement w in EnumerateNodes(v.TypeArguments.Values, predicate, options)) yield return w;
                break;
            }
            case CompiledBuiltinTypeExpression: break;
            case CompiledGenericTypeExpression: break;
            default: throw new UnreachableException();
        }
    }
}

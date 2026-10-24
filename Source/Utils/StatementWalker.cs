using LanguageCore.Parser;
using LanguageCore.Parser.Statements;

namespace LanguageCore.Compiler;

public static partial class StatementWalkerFilter
{
    public static bool FrameOnlyFilter(ISmallNode statement) => statement is not LambdaExpression;
    public static bool All(ISmallNode _) => true;
    public static Predicate<ISmallNode> At(SinglePosition p) => v => v.Position.Range.Contains(p);
    public static Predicate<ISmallNode> At(Range<SinglePosition> p) => v => RangeUtils.Overlaps(v.Position.Range, p);
    public static Predicate<ISmallNode> Before(SinglePosition p) => v => v.Position.Range.Start <= p;
    public static Predicate<ISmallNode> After(SinglePosition p) => v => v.Position.Range.End >= p;
}

public static partial class StatementWalker
{
    public static IEnumerable<ISmallNode> EnumerateNodes(ParserResult parserResult, Predicate<ISmallNode> predicate)
    {
        foreach (ISmallNode v in parserResult.TopLevelStatements.IsDefault ? Enumerable.Empty<ISmallNode>() : parserResult.TopLevelStatements.SelectMany(v => EnumerateNodes(v, predicate)))
        { yield return v; }

        foreach (FunctionThingDefinition item in parserResult.AllFunctions)
        {
            foreach (ISmallNode statement in item.Parameters.Parameters.SelectMany(v => EnumerateNodes(v.DefaultValue, predicate)))
            { yield return statement; }

            foreach (ISmallNode statement in item.Parameters.Parameters.SelectMany(v => EnumerateNodes(v.Type, predicate)))
            { yield return statement; }

            if (item is IHaveType haveType)
            {
                foreach (ISmallNode statement in EnumerateNodes(haveType.Type, predicate))
                { yield return statement; }
            }
        }

        foreach (EnumDefinition enumDefinition in parserResult.EnumDefinitions.IsDefault ? ImmutableArray<EnumDefinition>.Empty : parserResult.EnumDefinitions)
        {
            foreach (ISmallNode statement in enumDefinition.Members.SelectMany(v => EnumerateNodes(v.Value, predicate)))
            { yield return statement; }

            foreach (ISmallNode statement in EnumerateNodes(enumDefinition.Type, predicate))
            { yield return statement; }
        }

        foreach (AliasDefinition aliasDefinition in parserResult.AliasDefinitions.IsDefault ? ImmutableArray<AliasDefinition>.Empty : parserResult.AliasDefinitions)
        {
            foreach (ISmallNode statement in EnumerateNodes(aliasDefinition.Value, predicate))
            { yield return statement; }
        }
    }

    public static IEnumerable<ISmallNode> EnumerateNodes(IEnumerable<ISmallNode>? statement, Predicate<ISmallNode> predicate)
        => statement?.SelectMany(v => EnumerateNodes(v, predicate)) ?? Enumerable.Empty<ISmallNode>();

    public static IEnumerable<ISmallNode> EnumerateNodes(ISmallNode? statement, Predicate<ISmallNode> predicate) => statement switch
    {
        TypeInstance v => EnumerateNodes(v, predicate),
        Statement v => EnumerateNodes(v, predicate),
        null => Enumerable.Empty<ISmallNode>(),
        _ => throw new NotImplementedException(statement.GetType().Name),
    };

    public static IEnumerable<ISmallNode> EnumerateNodes(TypeInstance? type, Predicate<ISmallNode> predicate)
    {
        if (type is null) yield break;
        if (!predicate(type)) yield break;
        yield return type;

        switch (type)
        {
            case TypeInstanceFunction v:
                foreach (ISmallNode w in EnumerateNodes(v.FunctionReturnType, predicate)) yield return w;
                foreach (TypeInstance item in v.FunctionParameterTypes) foreach (ISmallNode w in EnumerateNodes(item, predicate)) yield return w;
                break;
            case TypeInstancePointer v:
                foreach (ISmallNode w in EnumerateNodes(v.To, predicate)) yield return w;
                break;
            case TypeInstanceReference v:
                foreach (ISmallNode w in EnumerateNodes(v.To, predicate)) yield return w;
                break;
            case TypeInstanceSimple v:
                foreach (ISmallNode w in EnumerateNodes(v.TypeArguments, predicate)) yield return w;
                break;
            case TypeInstanceStackArray v:
                foreach (ISmallNode w in EnumerateNodes(v.StackArrayOf, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.StackArraySize, predicate)) yield return w;
                break;
            case MissingTypeInstance:
                break;
            default: throw new UnreachableException(type.GetType().Name);
        }
    }

    public static IEnumerable<ISmallNode> EnumerateNodes(Statement? type) => EnumerateNodes(type, StatementWalkerFilter.All);
    public static IEnumerable<ISmallNode> EnumerateNodes(Statement? statement, Predicate<ISmallNode> predicate)
    {
        if (statement is null) yield break;
        if (!predicate(statement)) yield break;
        yield return statement;

        switch (statement)
        {
            case AnyCallExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Expression, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.TypeArguments, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Arguments, predicate)) yield return w;
                break;
            case MissingArgumentExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Value, predicate)) yield return w;
                break;
            case ArgumentExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Value, predicate)) yield return w;
                break;
            case BinaryOperatorCallExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Left, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Right, predicate)) yield return w;
                break;
            case ConstructorCallExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Type, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Arguments, predicate)) yield return w;
                break;
            case DereferenceExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Expression, predicate)) yield return w;
                break;
            case FieldExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Object, predicate)) yield return w;
                break;
            case FunctionCallExpression v:
                if (v.Object is not null) foreach (ISmallNode w in EnumerateNodes(v.Object, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Arguments, predicate)) yield return w;
                break;
            case GetReferenceExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Expression, predicate)) yield return w;
                break;
            case IdentifierExpression v:
                break;
            case IndexCallExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Object, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Index, predicate)) yield return w;
                break;
            case LambdaExpression v:
                //foreach (var item in v.Parameters.Parameters)
                //{
                //    EnumerateNodes(item.Type, callback);
                //}
                foreach (ISmallNode w in EnumerateNodes(v.Body, predicate)) yield return w;
                break;
            case ListExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Values, predicate)) yield return w;
                break;
            case LiteralExpression v:
                break;
            case ManagedTypeCastExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Expression, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Type, predicate)) yield return w;
                break;
            case NewInstanceExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Type, predicate)) yield return w;
                break;
            case ReinterpretExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.PrevStatement, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Type, predicate)) yield return w;
                break;
            case UnaryOperatorCallExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Expression, predicate)) yield return w;
                break;
            case ShortOperatorCall v:
                foreach (ISmallNode w in EnumerateNodes(v.Target, predicate)) yield return w;
                break;
            case Block v:
                foreach (ISmallNode w in EnumerateNodes(v.Statements, predicate)) yield return w;
                break;
            case CompoundAssignmentStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Target, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Value, predicate)) yield return w;
                break;
            case IfBranchStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Condition, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Body, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Else, predicate)) yield return w;
                break;
            case ElseBranchStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Body, predicate)) yield return w;
                break;
            case ForLoopStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Initialization, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Condition, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Step, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Body, predicate)) yield return w;
                break;
            case InstructionLabelDeclaration v:
                break;
            case KeywordCallStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Arguments, predicate)) yield return w;
                break;
            case SimpleAssignmentStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Target, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Value, predicate)) yield return w;
                break;
            case VariableDefinition v:
                foreach (ISmallNode w in EnumerateNodes(v.Type, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.InitialValue, predicate)) yield return w;
                break;
            case WhileLoopStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Condition, predicate)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Body, predicate)) yield return w;
                break;
            case EmptyStatement:
                break;
            case TypeArgumentListExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Arguments, predicate)) yield return w;
                break;
            case ArgumentListExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Arguments, predicate)) yield return w;
                break;
            case MissingExpression:
            case MissingStatement:
                break;
            default: throw new NotImplementedException(statement.GetType().Name);
        }
    }
}

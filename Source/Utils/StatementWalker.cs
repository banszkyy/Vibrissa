using LanguageCore.Parser;
using LanguageCore.Parser.Statements;

namespace LanguageCore.Compiler;

public static partial class StatementWalkerFilter
{
    public static bool FrameOnlyFilter(INode statement, int _) => statement is not LambdaExpression;
    public static bool All(INode _0, int _1) => true;
    public static bool AllTopLevel(INode _0, int _1) => _1 == 0;
    public static NodePredicate At(SinglePosition p) => (v, _) => v.Position.Range.Contains(p);
    public static NodePredicate At(Range<SinglePosition> p) => (v, _) => RangeUtils.Overlaps(v.Position.Range, p);
    public static NodePredicate Before(SinglePosition p) => (v, _) => v.Position.Range.Start <= p;
    public static NodePredicate After(SinglePosition p) => (v, _) => v.Position.Range.End >= p;

    public delegate bool NodePredicate(INode node, int depth);
}

public static partial class StatementWalker
{
    public static IEnumerable<INode> EnumerateNodes(ParserResult parserResult, StatementWalkerFilter.NodePredicate predicate, int depth = 0)
    {
        foreach (UsingDefinition v in parserResult.Usings.IsDefault ? Enumerable.Empty<UsingDefinition>() : parserResult.Usings)
        {
            if (!predicate(v, depth)) continue;
            yield return v;
        }

        foreach (ISmallNode v in parserResult.TopLevelStatements.IsDefault ? Enumerable.Empty<ISmallNode>() : parserResult.TopLevelStatements.SelectMany(v => EnumerateNodes(v, predicate, depth)))
        { yield return v; }

        foreach (FunctionThingDefinition item in parserResult.Functions.Append(parserResult.Operators))
        {
            if (!predicate(item, depth)) continue;
            yield return item;

            foreach (ISmallNode statement in item.Parameters.Parameters.SelectMany(v => EnumerateNodes(v.DefaultValue, predicate, depth + 1)))
            { yield return statement; }

            foreach (ISmallNode statement in item.Parameters.Parameters.SelectMany(v => EnumerateNodes(v.Type, predicate, depth + 1)))
            { yield return statement; }

            if (item is IHaveType haveType)
            {
                foreach (ISmallNode statement in EnumerateNodes(haveType.Type, predicate, depth + 1))
                { yield return statement; }
            }

            if (item.Block is not null)
            {
                foreach (ISmallNode statement in EnumerateNodes(item.Block, predicate, depth + 1))
                { yield return statement; }
            }
        }

        foreach (StructDefinition item in parserResult.Structs)
        {
            if (!predicate(item, depth)) yield break;
            yield return item;

            foreach (INode v in EnumerateNodes(item, predicate, depth + 1)) yield return v;
        }

        foreach (EnumDefinition enumDefinition in parserResult.EnumDefinitions.IsDefault ? ImmutableArray<EnumDefinition>.Empty : parserResult.EnumDefinitions)
        {
            if (!predicate(enumDefinition, depth)) continue;
            yield return enumDefinition;

            foreach (ISmallNode statement in EnumerateNodes(enumDefinition.Type, predicate, depth + 1))
            { yield return statement; }

            foreach (ISmallNode statement in enumDefinition.Members.SelectMany(v => EnumerateNodes(v.Value, predicate, depth + 1)))
            { yield return statement; }
        }

        foreach (AliasDefinition aliasDefinition in parserResult.AliasDefinitions.IsDefault ? ImmutableArray<AliasDefinition>.Empty : parserResult.AliasDefinitions)
        {
            if (!predicate(aliasDefinition, depth)) continue;
            yield return aliasDefinition;

            foreach (ISmallNode statement in EnumerateNodes(aliasDefinition.Value, predicate, depth + 1))
            { yield return statement; }
        }
    }

    public static IEnumerable<INode> EnumerateNodes(StructDefinition structDefinition, StatementWalkerFilter.NodePredicate predicate, int depth = 0)
    {
        foreach (FieldDefinition field in structDefinition.Fields)
        {
            if (!predicate(field, depth)) continue;
            yield return field;

            foreach (ISmallNode statement in EnumerateNodes(field.Type, predicate, depth + 1))
            { yield return statement; }
        }

        foreach (FunctionThingDefinition method in structDefinition.Functions.Cast<FunctionThingDefinition>().Append(structDefinition.Operators).Append(structDefinition.GeneralFunctions))
        {
            if (!predicate(method, depth)) continue;
            yield return method;

            foreach (ISmallNode statement in method.Parameters.Parameters.SelectMany(v => EnumerateNodes(v.DefaultValue, predicate, depth + 1)))
            { yield return statement; }

            foreach (ISmallNode statement in method.Parameters.Parameters.SelectMany(v => EnumerateNodes(v.Type, predicate, depth + 1)))
            { yield return statement; }

            if (method is IHaveType haveType)
            {
                foreach (ISmallNode statement in EnumerateNodes(haveType.Type, predicate, depth + 1))
                { yield return statement; }
            }

            if (method.Block is not null)
            {
                foreach (ISmallNode statement in EnumerateNodes(method.Block, predicate, depth + 1))
                { yield return statement; }
            }
        }
    }

    public static IEnumerable<ISmallNode> EnumerateNodes(IEnumerable<ISmallNode>? statement, StatementWalkerFilter.NodePredicate predicate, int depth = 0)
        => statement?.SelectMany(v => EnumerateNodes(v, predicate, depth)) ?? Enumerable.Empty<ISmallNode>();

    public static IEnumerable<ISmallNode> EnumerateNodes(ISmallNode? statement, StatementWalkerFilter.NodePredicate predicate, int depth = 0) => statement switch
    {
        TypeInstance v => EnumerateNodes(v, predicate, depth),
        Statement v => EnumerateNodes(v, predicate, depth),
        null => Enumerable.Empty<ISmallNode>(),
        _ => throw new UnreachableException(statement.GetType().Name),
    };

    public static IEnumerable<ISmallNode> EnumerateNodes(TypeInstance? type, StatementWalkerFilter.NodePredicate predicate, int depth = 0)
    {
        if (type is null) yield break;
        if (!predicate(type, depth)) yield break;
        yield return type;

        switch (type)
        {
            case TypeInstanceFunction v:
                foreach (ISmallNode w in EnumerateNodes(v.FunctionReturnType, predicate, depth + 1)) yield return w;
                foreach (TypeInstance item in v.FunctionParameterTypes) foreach (ISmallNode w in EnumerateNodes(item, predicate)) yield return w;
                break;
            case TypeInstancePointer v:
                foreach (ISmallNode w in EnumerateNodes(v.To, predicate, depth + 1)) yield return w;
                break;
            case TypeInstanceReference v:
                foreach (ISmallNode w in EnumerateNodes(v.To, predicate, depth + 1)) yield return w;
                break;
            case TypeInstanceSimple v:
                foreach (ISmallNode w in EnumerateNodes(v.TemplateArguments, predicate, depth + 1)) yield return w;
                break;
            case TypeInstanceStackArray v:
                foreach (ISmallNode w in EnumerateNodes(v.StackArrayOf, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.StackArraySize, predicate, depth + 1)) yield return w;
                break;
            case MissingTypeInstance:
                break;
            default: throw new UnreachableException(type.GetType().Name);
        }
    }

    public static IEnumerable<ISmallNode> EnumerateNodes(Statement? type) => EnumerateNodes(type, StatementWalkerFilter.All);
    public static IEnumerable<ISmallNode> EnumerateNodes(Statement? statement, StatementWalkerFilter.NodePredicate predicate, int depth = 0)
    {
        if (statement is null) yield break;
        if (!predicate(statement, depth)) yield break;
        yield return statement;

        switch (statement)
        {
            case AnyCallExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Expression, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.TemplateArguments, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Arguments, predicate, depth + 1)) yield return w;
                break;
            case MissingArgumentExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Value, predicate, depth + 1)) yield return w;
                break;
            case ArgumentExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Value, predicate, depth + 1)) yield return w;
                break;
            case BinaryOperatorCallExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Left, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Right, predicate, depth + 1)) yield return w;
                break;
            case ConstructorCallExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Type, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Arguments, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Initialization, predicate, depth + 1)) yield return w;
                break;
            case DereferenceExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Expression, predicate, depth + 1)) yield return w;
                break;
            case FieldExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Object, predicate, depth + 1)) yield return w;
                break;
            case FunctionCallExpression v:
                if (v.Object is not null) foreach (ISmallNode w in EnumerateNodes(v.Object, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Arguments, predicate, depth + 1)) yield return w;
                break;
            case GetReferenceExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Expression, predicate, depth + 1)) yield return w;
                break;
            case IdentifierExpression v:
                break;
            case IndexCallExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Object, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Index, predicate, depth + 1)) yield return w;
                break;
            case LambdaExpression v:
                //foreach (var item in v.Parameters.Parameters)
                //{
                //    EnumerateNodes(item.Type, callback);
                //}
                foreach (ISmallNode w in EnumerateNodes(v.Body, predicate, depth + 1)) yield return w;
                break;
            case ListExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Values, predicate, depth + 1)) yield return w;
                break;
            case PackExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Values, predicate, depth + 1)) yield return w;
                break;
            case LiteralExpression v:
                break;
            case ManagedTypeCastExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Expression, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Type, predicate, depth + 1)) yield return w;
                break;
            case NewInstanceExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Type, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Initialization, predicate, depth + 1)) yield return w;
                break;
            case ReinterpretExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.PrevStatement, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Type, predicate, depth + 1)) yield return w;
                break;
            case UnaryOperatorCallExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Expression, predicate, depth + 1)) yield return w;
                break;
            case ShortOperatorCall v:
                foreach (ISmallNode w in EnumerateNodes(v.Target, predicate, depth + 1)) yield return w;
                break;
            case Block v:
                foreach (ISmallNode w in EnumerateNodes(v.Statements, predicate, depth + 1)) yield return w;
                break;
            case CompoundAssignmentStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Target, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Value, predicate, depth + 1)) yield return w;
                break;
            case IfBranchStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Condition, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Body, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Else, predicate, depth + 1)) yield return w;
                break;
            case ElseBranchStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Body, predicate, depth + 1)) yield return w;
                break;
            case ForLoopStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Initialization, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Condition, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Step, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Body, predicate, depth + 1)) yield return w;
                break;
            case InstructionLabelDeclaration v:
                break;
            case KeywordCallStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Arguments, predicate, depth + 1)) yield return w;
                break;
            case SimpleAssignmentStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Target, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Value, predicate, depth + 1)) yield return w;
                break;
            case VariableDefinition v:
                foreach (ISmallNode w in EnumerateNodes(v.Type, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.InitialValue, predicate, depth + 1)) yield return w;
                break;
            case WhileLoopStatement v:
                foreach (ISmallNode w in EnumerateNodes(v.Condition, predicate, depth + 1)) yield return w;
                foreach (ISmallNode w in EnumerateNodes(v.Body, predicate, depth + 1)) yield return w;
                break;
            case EmptyStatement:
                break;
            case TemplateArgumentListExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Arguments, predicate, depth + 1)) yield return w;
                break;
            case ArgumentListExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Arguments, predicate, depth + 1)) yield return w;
                break;
            case MissingExpression:
            case MissingStatement:
                break;
            case TemplateArgumentExpressionType v:
                foreach (ISmallNode w in EnumerateNodes(v.Value, predicate, depth + 1)) yield return w;
                break;
            case TemplateArgumentExpressionValue v:
                foreach (ISmallNode w in EnumerateNodes(v.Value, predicate, depth + 1)) yield return w;
                break;
            case InitializationExpression v:
                foreach (ISmallNode w in EnumerateNodes(v.Values, predicate, depth + 1)) yield return w;
                break;
            case InitializationExpressionItem v:
                foreach (ISmallNode w in EnumerateNodes(v.Value, predicate, depth + 1)) yield return w;
                break;
            default: throw new UnreachableException(statement.GetType().Name);
        }
    }
}

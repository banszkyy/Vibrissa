using LanguageCore.Parser;

namespace LanguageCore.Compiler;

public partial class StatementCompiler
{
    public static class FunctionQuery
    {
        [DebuggerStepThrough]
        public static FunctionQuery<TFunction, GeneralType> Create<TFunction>(
            string? identifier,
            ImmutableArray<CompiledTemplateArgument>? templateArguments = null,
            Uri? relevantFile = null,
            Action<TemplateInstance<TFunction>>? addCompilable = null)
            where TFunction : notnull
            => new()
            {
                Identifier = identifier,
                Arguments = null,
                ArgumentCount = null,
                TemplateArguments = templateArguments,
                Converter = v => v,
                RelevantFile = relevantFile,
                ReturnType = null,
                AddCompilable = addCompilable,
            };

        [DebuggerStepThrough]
        public static FunctionQuery<TFunction, CompiledExpression> Create<TFunction>(
            string? identifier,
            ImmutableArray<CompiledExpression> arguments,
            ImmutableArray<CompiledTemplateArgument>? templateArguments = null,
            Uri? relevantFile = null,
            GeneralType? returnType = null,
            Action<TemplateInstance<TFunction>>? addCompilable = null)
            where TFunction : notnull
            => new()
            {
                Identifier = identifier,
                Arguments = arguments,
                ArgumentCount = arguments.Length,
                TemplateArguments = templateArguments,
                Converter = v => v.Type,
                RelevantFile = relevantFile,
                ReturnType = returnType,
                AddCompilable = addCompilable,
            };

        [DebuggerStepThrough]
        public static FunctionQuery<TFunction, GeneralType> Create<TFunction>(
            string? identifier,
            ImmutableArray<GeneralType> arguments,
            ImmutableArray<CompiledTemplateArgument>? templateArguments = null,
            Uri? relevantFile = null,
            GeneralType? returnType = null,
            Action<TemplateInstance<TFunction>>? addCompilable = null)
            where TFunction : notnull
            => new()
            {
                Identifier = identifier,
                Arguments = arguments,
                ArgumentCount = arguments.Length,
                TemplateArguments = templateArguments,
                Converter = v => v,
                RelevantFile = relevantFile,
                ReturnType = returnType,
                AddCompilable = addCompilable,
            };
    }

    public readonly struct FunctionQuery<TFunction, TArgument>
        where TFunction : notnull
    {
        public string? Identifier { get; init; }
        public Uri? RelevantFile { get; init; }
        public TArgument? Object { get; init; }
        public ImmutableArray<TArgument>? Arguments { get; init; }
        public ImmutableArray<CompiledTemplateArgument>? TemplateArguments { get; init; }
        public int? ArgumentCount { get; init; }
        public GeneralType? ReturnType { get; init; }
        public Action<TemplateInstance<TFunction>>? AddCompilable { get; init; }
        public Func<TArgument, GeneralType> Converter { get; init; }
    }

    public class FunctionQueryResult<TFunction> where TFunction : notnull
    {
        public required TFunction Function { get; init; }
        public required ImmutableArray<CompiledExpression> Arguments { get; init; }
        public ImmutableDictionary<string, CompiledTemplateArgument>? TemplateArguments { get; init; }
        public bool Success { get; init; }

        public void ReplaceArgumentsIfNeeded(ref ImmutableArray<CompiledExpression> arguments)
        {
            if (Arguments.IsDefault) return;
            arguments = Arguments;
        }

        public void Deconstruct(
            out TFunction function,
            out ImmutableDictionary<string, CompiledTemplateArgument>? templateArguments)
        {
            function = Function;
            templateArguments = TemplateArguments;
        }

        public override string? ToString() => Function.ToString();
    }

    class MatchList<TMatch, TDefinition> : IEnumerable<TMatch>
        where TMatch : notnull, DefinitionMatch<TDefinition>
        where TDefinition : notnull
    {
        readonly List<TMatch> _items = new();

        public int Count => _items.Count;

        public TMatch this[int i] => _items[i];
        public TMatch this[Index i] => _items[i];

        public void Add(TMatch match)
        {
            _items.AddSorted(match);
            for (int i = 1; i < _items.Count; i++)
            {
                if (_items[0].CompareTo(_items[i]) == -1)
                {
                    _items.RemoveAt(i--);
                }
            }
        }

        public IEnumerator<TMatch> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }

    class DefinitionMatch<TDefinition> :
        IComparable<DefinitionMatch<TDefinition>>,
        IEquatable<DefinitionMatch<TDefinition>>
        where TDefinition : notnull
    {
        public required TDefinition Definition { get; init; }
        public required List<PossibleDiagnostic> Errors { get; init; }

        public bool IsIdentifierMatched { get; set; }
        public int IdentifierBadness { get; set; }
        public bool IsFileMatches { get; set; }
        public bool IsGenericParameterCountMatches { get; set; }
        public bool IsProtectionRespected { get; set; }

        protected const int Better = -1;
        protected const int Same = 0;
        protected const int Worse = 1;

        public virtual int CompareTo(DefinitionMatch<TDefinition>? other)
        {
            if (Equals(other)) return Same;
            if (other is null) return Better;

            if (IsIdentifierMatched && !other.IsIdentifierMatched) return Better;
            if (!IsIdentifierMatched && other.IsIdentifierMatched) return Worse;

            if (IdentifierBadness < other.IdentifierBadness) return Better;
            if (IdentifierBadness > other.IdentifierBadness) return Worse;
            if (!IsIdentifierMatched || !other.IsIdentifierMatched) return Same;

            if (IsGenericParameterCountMatches && !other.IsGenericParameterCountMatches) return Better;
            if (!IsGenericParameterCountMatches && other.IsGenericParameterCountMatches) return Worse;
            if (!IsGenericParameterCountMatches || !other.IsGenericParameterCountMatches) return Same;

            if (IsProtectionRespected && !other.IsProtectionRespected) return Better;
            if (!IsProtectionRespected && other.IsProtectionRespected) return Worse;

            if (IsFileMatches && !other.IsFileMatches) return Better;
            if (!IsFileMatches && other.IsFileMatches) return Worse;

            return Same;
        }

        public virtual bool Equals(DefinitionMatch<TDefinition>? other)
        {
            if (other is null) return false;
            if (IdentifierBadness != other.IdentifierBadness) return false;
            if (IsIdentifierMatched != other.IsIdentifierMatched) return false;
            if (IsFileMatches != other.IsFileMatches) return false;
            if (IsGenericParameterCountMatches != other.IsGenericParameterCountMatches) return false;
            return true;
        }

        public override string? ToString() => Definition.ToString();
    }

    class FunctionMatch<TDefinition> : DefinitionMatch<TDefinition>,
        IComparable<FunctionMatch<TDefinition>>,
        IEquatable<FunctionMatch<TDefinition>>
        where TDefinition : notnull
    {
        public bool IsParameterCountMatches { get; set; }
        public bool IsTemplateArgumentCountMatches { get; set; }

        public CastLevel ReturnTypeMatch { get; set; }
        public int UsedUpDefaultParameterValues { get; set; }
        public bool UsedUpSpreadParameters { get; set; }
        public CastLevel? ParameterTypeMatch { get; set; }

        public ImmutableDictionary<string, CompiledTemplateArgument>? TemplateArguments { get; set; }
        public CompiledExpression? Object { get; set; }
        public ImmutableArray<CompiledExpression> Arguments { get; set; }

        public override int CompareTo(DefinitionMatch<TDefinition>? other) => CompareTo(other as FunctionMatch<TDefinition>);
        public override bool Equals(DefinitionMatch<TDefinition>? other) => Equals(other as FunctionMatch<TDefinition>);

        public int CompareTo(FunctionMatch<TDefinition>? other)
        {
            if (other is null) return Better;
            if (Equals(other)) return Same;

            if (IsIdentifierMatched && !other.IsIdentifierMatched) return Better;
            if (!IsIdentifierMatched && other.IsIdentifierMatched) return Worse;

            if (IdentifierBadness < other.IdentifierBadness) return Better;
            if (IdentifierBadness > other.IdentifierBadness) return Worse;
            if (!IsIdentifierMatched || !other.IsIdentifierMatched) return Same;

            if (IsTemplateArgumentCountMatches && !other.IsTemplateArgumentCountMatches) return Better;
            if (!IsTemplateArgumentCountMatches && other.IsTemplateArgumentCountMatches) return Worse;
            if (!IsTemplateArgumentCountMatches || !other.IsTemplateArgumentCountMatches) return Same;

            if (IsParameterCountMatches && !other.IsParameterCountMatches) return Better;
            if (!IsParameterCountMatches && other.IsParameterCountMatches) return Worse;
            if (!IsParameterCountMatches || !other.IsParameterCountMatches) return Same;

            if (ParameterTypeMatch is not null && other.ParameterTypeMatch is not null)
            {
                CastLevel a = ParameterTypeMatch.Value;
                CastLevel b = other.ParameterTypeMatch.Value;
                if (a > b) return Better;
                if (a < b) return Worse;
                if (a == CastLevel.None || b == CastLevel.None) return Same;
            }

            if (ReturnTypeMatch > other.ReturnTypeMatch) return Better;
            if (ReturnTypeMatch < other.ReturnTypeMatch) return Worse;
            if (ReturnTypeMatch == CastLevel.None || other.ReturnTypeMatch == CastLevel.None) return Same;

            if (TemplateArguments is null && other.TemplateArguments is not null) return Better;
            if (TemplateArguments is not null && other.TemplateArguments is null) return Worse;

            if (UsedUpDefaultParameterValues < other.UsedUpDefaultParameterValues) return Better;
            if (UsedUpDefaultParameterValues > other.UsedUpDefaultParameterValues) return Worse;

            if (!UsedUpSpreadParameters && other.UsedUpSpreadParameters) return Better;
            if (UsedUpSpreadParameters && !other.UsedUpSpreadParameters) return Worse;

            if (IsProtectionRespected && !other.IsProtectionRespected) return Better;
            if (!IsProtectionRespected && other.IsProtectionRespected) return Worse;

            if (IsFileMatches && !other.IsFileMatches) return Better;
            if (!IsFileMatches && other.IsFileMatches) return Worse;

            return Same;
        }

        public bool Equals(FunctionMatch<TDefinition>? other)
        {
            if (other is null) return false;
            if (!base.Equals(other)) return false;
            if (IsTemplateArgumentCountMatches != other.IsTemplateArgumentCountMatches) return false;
            if (IsParameterCountMatches != other.IsParameterCountMatches) return false;
            if (ReturnTypeMatch != other.ReturnTypeMatch) return false;
            if (UsedUpDefaultParameterValues != other.UsedUpDefaultParameterValues) return false;
            if ((ParameterTypeMatch is null) != (other.ParameterTypeMatch is null)) return false;
            if (ParameterTypeMatch is null || other.ParameterTypeMatch is null) return false;
            if (ParameterTypeMatch.Value != other.ParameterTypeMatch.Value) return false;
            if ((TemplateArguments is null) != (other.TemplateArguments is null)) return false;
            if (TemplateArguments is null || other.TemplateArguments is null) return false;
            if (!Utils.SequenceEquals(TemplateArguments, other.TemplateArguments, (a, b) => a.Key == b.Key && a.Value.Equals(b.Value))) return false;
            return true;
        }
    }

    public delegate bool FunctionQueryIdentifierMatcher(
        string passed,
        string defined,
        out int badness);

    public bool GetFunction<TFunction, TArgument>(
        Functions<TFunction> functions,
        string kindName,
        string readableName,

        FunctionQuery<TFunction, TArgument> query,

        [NotNullWhen(true)] out FunctionQueryResult<TFunction>? result,
        [NotNullWhen(false)] out PossibleDiagnostic? error)
        where TFunction : class, ICompiledFunctionDefinition, IIdentifiable<string>, ICompiledDefinition<FunctionThingDefinition>
        where TArgument : notnull
    {
        string kindNameLower = kindName.ToLowerInvariant();
        string kindNameCapital = char.ToUpperInvariant(kindName[0]) + kindName[1..];

        MatchList<FunctionMatch<TFunction>, TFunction> functionMatches = new();

        foreach (TFunction function in functions.Compiled)
        {
            functionMatches.Add(GetFunctionMatch(function, query));
        }

        if (functionMatches.Count > 0)
        {
            FunctionMatch<TFunction> best = functionMatches[0];
            result = new FunctionQueryResult<TFunction>()
            {
                Function = best.Definition,
                Success = true,
                TemplateArguments = best.TemplateArguments,
                Arguments = best.Arguments,
            };

            if (best.Errors.Count > 0)
            {
                error = new PossibleDiagnostic($"{kindNameCapital} `{readableName}` not found", best.Errors.ToImmutableArray(), ignoreOnPartialSource: best.Errors.All(v => v.IgnoreOnPartialSource), isTellingNothing: true);
                return false;
            }

            if (!best.IsIdentifierMatched)
            {
                if (best.IdentifierBadness == 1)
                {
                    error = new PossibleDiagnostic($"No {kindName} found with name `{query.Identifier}` (did you mean `{best.Definition.Identifier}`?)");
                }
                else
                {
                    error = new PossibleDiagnostic($"No {kindName} found with name `{query.Identifier}`", isTellingNothing: true);
                }
                return false;
            }

            if (!best.IsTemplateArgumentCountMatches)
            {
                PossibleDiagnostic suberror = new($"Wrong number of template arguments passed: expected {(best.Definition.Definition is ITemplateable t ? (t.Template?.Parameters.Length ?? 0) : 0)} but got {query.TemplateArguments?.Length ?? 0}");
                if (best.Definition is FunctionThingDefinition ftd)
                { suberror = suberror.WithRelatedInfo(new DiagnosticRelatedInformationAt($"{kindNameCapital} `{best.Definition.ToReadable()}` defined here", new Location(ftd.Identifier.Position, ftd.File))); }
                error = new PossibleDiagnostic($"{kindNameCapital} `{readableName}` not found", ImmutableArray.Create(suberror), isTellingNothing: true);
                return false;
            }

            if (!best.IsParameterCountMatches)
            {
                PossibleDiagnostic suberror = new($"Wrong number of arguments passed: expected {best.Definition.Parameters.Length} but got {query.ArgumentCount}");
                if (best.Definition is FunctionThingDefinition ftd)
                { suberror = suberror.WithRelatedInfo(new DiagnosticRelatedInformationAt($"{kindNameCapital} `{best.Definition.ToReadable()}` defined here", new Location(ftd.Identifier.Position, ftd.File))); }
                error = new PossibleDiagnostic($"{kindNameCapital} `{readableName}` not found", ImmutableArray.Create(suberror), isTellingNothing: true);
                return false;
            }

            if (best.ParameterTypeMatch is not null &&
                best.ParameterTypeMatch.Value == CastLevel.None)
            {
                PossibleDiagnostic suberror = new($"Wrong types of arguments passed (sorry I can't tell any more info)");
                GetFunctionMatch(best.Definition, query);
                if (best.Definition is FunctionThingDefinition ftd)
                { suberror = suberror.WithRelatedInfo(new DiagnosticRelatedInformationAt($"{kindNameCapital} `{best.Definition.ToReadable()}` defined here", new Location(ftd.Identifier.Position, ftd.File))); }
                error = new PossibleDiagnostic($"{kindNameCapital} `{readableName}` not found", ImmutableArray.Create(suberror), isTellingNothing: true);
                return false;
            }

            if (best.ReturnTypeMatch == CastLevel.None)
            {
                PossibleDiagnostic suberror = new($"Wrong return type (sorry I can't tell any more info)");
                if (best.Definition is FunctionThingDefinition ftd)
                { suberror = suberror.WithRelatedInfo(new DiagnosticRelatedInformationAt($"{kindNameCapital} `{best.Definition.ToReadable()}` defined here", new Location(ftd.Identifier.Position, ftd.File))); }
                error = new PossibleDiagnostic($"{kindNameCapital} `{readableName}` not found", ImmutableArray.Create(suberror), isTellingNothing: true);
                return false;
            }

            if (functionMatches.Count > 1)
            {
                error = new PossibleDiagnostic($"Multiple functions matched ({string.Join(", ", functionMatches.Select(v => v.Definition.ToReadable()))})");
                GetFunctionMatch(functionMatches[0].Definition, query);
                GetFunctionMatch(functionMatches[1].Definition, query);
                foreach (FunctionMatch<TFunction> functionMatch in functionMatches)
                { error = error.WithRelatedInfo(new DiagnosticRelatedInformationAt($"{kindNameCapital} `{functionMatch.Definition.ToReadable()}` defined here", functionMatch.Definition.Location)); }
                return false;
            }

            if (best.Definition.Definition.IsTemplate)
            {
                if (best.TemplateArguments is null)
                {
                    PossibleDiagnostic suberror = new($"Failed to resolve the template types");
                    if (best.Definition is FunctionThingDefinition ftd)
                    { suberror = suberror.WithRelatedInfo(new DiagnosticRelatedInformationAt($"{kindNameCapital} `{best.Definition.ToReadable()}` defined here", new Location(ftd.Identifier.Position, ftd.File))); }
                    error = new PossibleDiagnostic($"{kindNameCapital} `{readableName}` not found", ImmutableArray.Create(suberror), isTellingNothing: true);
                    return false;
                }

                bool templateAlreadyAdded = false;
                foreach (TemplateInstance<TFunction> item in functions.Compilable)
                {
                    if (!FunctionEquals(item, best)) continue;
                    result = new FunctionQueryResult<TFunction>()
                    {
                        Function = item.Template,
                        Success = true,
                        TemplateArguments = best.TemplateArguments,
                        Arguments = best.Arguments,
                    };
                    templateAlreadyAdded = true;
                    break;
                }

                if (!templateAlreadyAdded)
                {
                    TemplateInstance<TFunction> template = new(best.Definition, best.TemplateArguments);
                    query.AddCompilable?.Invoke(template);
                    result = new FunctionQueryResult<TFunction>()
                    {
                        Function = template.Template,
                        Success = true,
                        TemplateArguments = best.TemplateArguments,
                        Arguments = best.Arguments,
                    };
                }
            }

            error = null;
            return true;
        }
        else
        {
            result = default;
            error = new PossibleDiagnostic($"{kindNameCapital} `{readableName}` not found", ignoreOnPartialSource: true, isTellingNothing: true);
            return false;
        }
    }

    FunctionMatch<TFunction> GetFunctionMatch<TFunction, TArgument>(
        TFunction function,
        FunctionQuery<TFunction, TArgument> query)
        where TFunction : ICompiledFunctionDefinition, IIdentifiable<string>, ICompiledDefinition<FunctionThingDefinition>
        where TArgument : notnull
    {
        FunctionMatch<TFunction> result = new()
        {
            Definition = function,
            Errors = new(),
        };

        if (query.Identifier is null)
        {
            result.IsIdentifierMatched = true;
            result.IdentifierBadness = 0;
        }
        else if (function.Identifier.Equals(query.Identifier))
        {
            result.IsIdentifierMatched = true;
            result.IdentifierBadness = 0;
        }
        else
        {
            result.IsIdentifierMatched = false;
            result.IdentifierBadness = 2;

            if (query.Identifier.Equals(function.Identifier, StringComparison.InvariantCultureIgnoreCase))
            {
                result.IdentifierBadness = 1;
            }

            if (result.IdentifierBadness == 1)
            {
                PossibleDiagnostic item = new($"Function `{query.Identifier}` does not match with `{function.Identifier}`");
                if (function is FunctionThingDefinition ftd)
                { item = item.WithRelatedInfo(new DiagnosticRelatedInformationAt($"Function `{function.ToReadable()}` defined here", new Location(ftd.Identifier.Position, ftd.File))); }
                result.Errors.Add(item);
            }
            else
            {
                result.Errors.Add(new($"No function found with name `{query.Identifier}`", ignoreOnPartialSource: true, isTellingNothing: true));
            }
            return result;
        }

        if (query.ArgumentCount.HasValue)
        {
            int requiredParameters = 0;
            for (int i = 0; i < function.Parameters.Length; i++)
            {
                if (function.Parameters[i].Definition.DefaultValue is not null) break;
                if (function.Parameters[i].Definition.SpreadOperator is not null) break;

                requiredParameters = i + 1;
            }

            if (query.ArgumentCount.Value < requiredParameters)
            {
                PossibleDiagnostic item = new($"Wrong number of arguments passed: expected {function.Parameters.Length} but passed {query.ArgumentCount.Value}");
                if (function is FunctionThingDefinition ftd)
                { item = item.WithRelatedInfo(new DiagnosticRelatedInformationAt($"Function `{function.ToReadable()}` defined here", new Location(ftd.Identifier.Position, ftd.File))); }
                result.Errors.Add(item);
                return result;
            }

            bool hasSpreadParameters = function.Parameters.Any(v => v.Definition.SpreadOperator is not null);

            if (!hasSpreadParameters && query.ArgumentCount.Value > function.Parameters.Length)
            {
                PossibleDiagnostic item = new($"Wrong number of arguments passed: expected {function.Parameters.Length} but passed {query.ArgumentCount.Value}");
                if (function is FunctionThingDefinition ftd)
                { item = item.WithRelatedInfo(new DiagnosticRelatedInformationAt($"Function `{function.ToReadable()}` defined here", new Location(ftd.Identifier.Position, ftd.File))); }
                result.Errors.Add(item);
                return result;
            }

            result.UsedUpDefaultParameterValues = function.Parameters.Length - query.ArgumentCount.Value;
            result.UsedUpSpreadParameters = hasSpreadParameters;
        }

        result.IsTemplateArgumentCountMatches = true;
        result.IsParameterCountMatches = true;
        result.IsGenericParameterCountMatches = true;

        if (query.RelevantFile is null ||
            function.File == query.RelevantFile)
        {
            result.IsFileMatches = true;
        }

        if (query.RelevantFile is null ||
            function.Definition.CanUse(query.RelevantFile))
        {
            result.IsProtectionRespected = true;
        }
        else
        {
            result.Errors.Add(new($"Cannot use function `{query.Identifier}` due it's protection level"));
        }

        void GetArgumentTypeMatch(ref CastLevel typeMatch, GeneralType definedType, ParameterDefinition definition, GeneralType passed, List<PossibleDiagnostic> errors)
        {
            if (IsAssignable(passed, definedType, out PossibleDiagnostic? castError, out CastLevel castLevel))
            {
                if (typeMatch > castLevel) typeMatch = castLevel;
                return;
            }
            else
            {
                typeMatch = CastLevel.None;
                errors.Add(new PossibleDiagnostic($"Invalid argument passed to `{definition.Identifier}`", passed as ILocated, castError));
                return;
            }
        }

        void GetArgumentExpressionMatch(ref CastLevel typeMatch, out CompiledExpression? compiledPassedArgument, GeneralType definedType, ParameterDefinition definition, CompiledExpression passed, List<PossibleDiagnostic> errors)
        {
            if (IsAssignable(passed, definedType, out compiledPassedArgument, out PossibleDiagnostic? castError, out CastLevel castLevel))
            {
                if (definition.Modifiers.Contains(ModifierKeywords.This) && castLevel == CastLevel.GotReferenceOf) castLevel = CastLevel.GotReferenceOfObject;

                if (typeMatch > castLevel) typeMatch = castLevel;
                return;
            }
            else
            {
                typeMatch = CastLevel.None;
                errors.Add(new PossibleDiagnostic($"Invalid argument passed to `{definition.Identifier}`", passed as ILocated, castError));
                return;
            }
        }

        CastLevel GetReturnTypeMatch(GeneralType target, GeneralType current, List<PossibleDiagnostic> errors)
        {
            if (IsAssignable(current, target, out PossibleDiagnostic? error, out CastLevel castLevel))
            {
                return castLevel;
            }
            else
            {
                errors.Add(new PossibleDiagnostic($"Return type mismatch", error));
                return CastLevel.None;
            }
        }

        Dictionary<string, CompiledTemplateArgument>? _templateArguments = function.Definition.IsTemplate ? new() : null;

        if (query.TemplateArguments is not null)
        {
            if ((query.TemplateArguments.Value.Length == 0 && !function.Definition.IsTemplate) ||
                (query.TemplateArguments.Value.Length != (function.Definition.Template?.Parameters.Length ?? 0)))
            {
                result.IsTemplateArgumentCountMatches = false;
            }

            int checkCount = Math.Min(query.TemplateArguments.Value.Length, function.Definition.Template?.Parameters.Length ?? 0);
            for (int i = 0; i < checkCount; i++)
            {
                CompiledTemplateArgument passed = query.TemplateArguments.Value[i];
                _templateArguments![function.Definition.Template!.Parameters[i].Content] = passed;
            }
        }

        if (!query.Arguments.HasValue)
        {
            result.ParameterTypeMatch = null;
        }
        else
        {
            result.ParameterTypeMatch = CastLevel.Equals;

            int di = 0;
            int pi = 0;

            List<CompiledExpression>? arguments = null;

            while (di < function.Parameters.Length)
            {
                CompiledParameter defined = function.Parameters[di];
                GeneralType definedType = GeneralType.TryInsertTemplateParameters(defined.Type, _templateArguments);
                CastLevel v = result.ParameterTypeMatch.Value;

                if (defined.Definition.SpreadOperator is not null)
                {
                    if (!definedType.Is(out ArrayType? definedArrayType))
                    {
                        result.Errors.Add(new PossibleDiagnostic($"Invalid spread operator", new Location(defined.Definition.SpreadOperator.Position, defined.File)));
                        break;
                    }
                    else if (pi >= query.Arguments.Value.Length)
                    {
                        arguments ??= new();
                        arguments.Add(new CompiledList()
                        {
                            Location = new Location(Position.UnknownPosition, query.RelevantFile ?? throw new NotImplementedException()),
                            SaveValue = true,
                            Type = new ArrayType(definedArrayType.Of, new CompiledConstantValue()
                            {
                                Location = new Location(Position.UnknownPosition, query.RelevantFile ?? throw new NotImplementedException()),
                                SaveValue = true,
                                Type = ArrayLengthType,
                                Value = 0,
                            }),
                            Values = ImmutableArray<CompiledExpression>.Empty,
                        });
                    }
                    else
                    {
                        TArgument passed = query.Arguments.Value[pi];
                        if (result.ParameterTypeMatch > CastLevel.None && _templateArguments is not null)
                        {
                            if (ResolveTemplateParameters(definedType, query.Converter(passed), out PossibleDiagnostic? templateResolveError, _templateArguments))
                            {
                                definedType = GeneralType.TryInsertTemplateParameters(defined.Type, _templateArguments);
                            }
                            else
                            {
                                result.Errors.Add(templateResolveError);
                            }
                        }

                        if (passed is CompiledExpression passedExpression)
                        {
                            List<CompiledExpression> assignedSpreadList = new();
                            int started = pi;
                            while (pi < query.Arguments.Value.Length)
                            {
                                passed = query.Arguments.Value[pi];
                                if (passed is not CompiledExpression passedExpression0) throw new UnreachableException();

                                GetArgumentExpressionMatch(ref v, out CompiledExpression? assignedArgumentItem, definedArrayType.Of, defined.Definition, passedExpression0, result.Errors);
                                if (v < result.ParameterTypeMatch) result.ParameterTypeMatch = v;

                                assignedSpreadList.Add(assignedArgumentItem ?? passedExpression);

                                if (result.ParameterTypeMatch > CastLevel.None && _templateArguments is not null)
                                {
                                    if (!ResolveTemplateParameters(definedArrayType.Of, assignedArgumentItem?.Type ?? query.Converter(passed), out PossibleDiagnostic? templateResolveError, _templateArguments))
                                    {
                                        result.Errors.Add(templateResolveError);
                                    }
                                }
                                pi++;
                            }

                            arguments ??= new();
                            arguments.Add(new CompiledList()
                            {
                                Location = new Location(new Position(assignedSpreadList), passedExpression.Location.File),
                                SaveValue = true,
                                Type = new ArrayType(passedExpression.Type, new CompiledConstantValue()
                                {
                                    Location = new Location(new Position(assignedSpreadList), passedExpression.Location.File),
                                    SaveValue = true,
                                    Type = ArrayLengthType,
                                    Value = assignedSpreadList.Count,
                                }),
                                Values = assignedSpreadList.ToImmutableArray(),
                            });
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                else
                {
                    if (pi >= query.Arguments.Value.Length) break;

                    TArgument passed = query.Arguments.Value[pi];
                    if (result.ParameterTypeMatch > CastLevel.None && _templateArguments is not null)
                    {
                        if (ResolveTemplateParameters(definedType, query.Converter(passed), out PossibleDiagnostic? templateResolveError, _templateArguments))
                        {
                            definedType = GeneralType.TryInsertTemplateParameters(defined.Type, _templateArguments);
                        }
                        else
                        {
                            result.Errors.Add(templateResolveError);
                        }
                    }

                    if (passed is CompiledExpression passedExpression1)
                    {
                        GetArgumentExpressionMatch(ref v, out CompiledExpression? assignedArgument, definedType, defined.Definition, passedExpression1, result.Errors);
                        if (v < result.ParameterTypeMatch) result.ParameterTypeMatch = v;

                        arguments ??= new();
                        arguments.Add(assignedArgument ?? passedExpression1);

                        if (result.ParameterTypeMatch > CastLevel.None && _templateArguments is not null)
                        {
                            if (!ResolveTemplateParameters(definedType, assignedArgument?.Type ?? query.Converter(passed), out PossibleDiagnostic? templateResolveError, _templateArguments))
                            {
                                result.Errors.Add(templateResolveError);
                            }
                        }
                        pi++;
                    }
                    else
                    {
                        GeneralType passedType = query.Converter.Invoke(passed);

                        GetArgumentTypeMatch(ref v, definedType, defined.Definition, passedType, result.Errors);
                        if (v < result.ParameterTypeMatch) result.ParameterTypeMatch = v;

                        if (result.ParameterTypeMatch > CastLevel.None && _templateArguments is not null)
                        {
                            if (!ResolveTemplateParameters(definedType, passedType, out PossibleDiagnostic? templateResolveError, _templateArguments))
                            {
                                result.Errors.Add(templateResolveError);
                            }
                        }
                        pi++;
                    }
                }

                di++;
            }

            result.Arguments = arguments?.ToImmutableArray() ?? default;
        }

        result.TemplateArguments = _templateArguments?.ToImmutableDictionary();

        if (query.ReturnType is not null)
        {
            result.ReturnTypeMatch = GetReturnTypeMatch(GeneralType.TryInsertTemplateParameters(function.Type, _templateArguments), query.ReturnType, result.Errors);
        }
        else
        {
            result.ReturnTypeMatch = CastLevel.Equals;
        }

        return result;
    }

    public static bool ResolveTemplateParameters(GeneralType defined, GeneralType passed, [NotNullWhen(false)] out PossibleDiagnostic? error, Dictionary<string, CompiledTemplateArgument> templateArguments)
    {
        error = null;

        switch (defined.FinalValue, passed.FinalValue)
        {
            case (_, UnresolvedTemplateType):
            {
                error = new PossibleDiagnostic($"This shouldn't be a template type");
                return false;
            }
            case (UnresolvedTemplateType v, _):
            {
                if (templateArguments.TryGetValue(v.Identifier, out CompiledTemplateArgument? addedTemplateParameter))
                {
                    if (addedTemplateParameter is not CompiledTemplateArgumentType w)
                    {
                        error = new PossibleDiagnostic($"Template parameter `{addedTemplateParameter.Definition}` is not a type");
                        return false;
                    }

                    if (!w.Value.SameAs(passed))
                    {
                        error = new PossibleDiagnostic($"Type `{passed}` is not the same as `{w.Value}`");
                        return false;
                    }
                }
                else
                {
                    templateArguments.Add(v.Identifier, new CompiledTemplateArgumentType()
                    {
                        Value = passed,
                        Definition = v.Definition,
                    });
                }

                return true;
            }
            case (IReferenceType v, IReferenceType w):
            {
                return ResolveTemplateParameters(v.To, w.To, out error, templateArguments);
            }
            case (FunctionType v, FunctionType w):
            {
                if (v.Parameters.Length != w.Parameters.Length)
                {
                    error = new PossibleDiagnostic($"Type `{passed}` is not the same as `{defined}`");
                    return false;
                }

                for (int i = 0; i < v.Parameters.Length; i++)
                {
                    if (!ResolveTemplateParameters(v.Parameters[i], w.Parameters[i], out error, templateArguments)) return false;
                }

                if (!ResolveTemplateParameters(v.ReturnType, w.ReturnType, out error, templateArguments)) return false;
                else return true;
            }
            case (StructType v, StructType w):
            {
                if (!Utils.ReferenceEquals(v.Struct.Definition, w.Struct.Definition))
                {
                    error = new PossibleDiagnostic($"Type `{passed}` is not the same as `{defined}`");
                    return false;
                }
                if (v.Struct.Definition.Template is null) return true;
                if (w.Struct.Definition.Template is null) return true;
                if (v.Struct.Definition.Template.Parameters.Length != w.TemplateArguments.Count) return true;

                for (int i = 0; i < v.Struct.Definition.Template.Parameters.Length; i++)
                {
                    string typeParamName = v.Struct.Definition.Template.Parameters[i].Content;
                    CompiledTemplateArgument typeParamValue = w.TemplateArguments[typeParamName];

                    if (templateArguments.TryGetValue(typeParamName, out CompiledTemplateArgument? addedTemplateParameter))
                    {
                        if (!addedTemplateParameter.SameAs(typeParamValue))
                        {
                            error = new PossibleDiagnostic($"Type `{typeParamValue}` is not the same as `{addedTemplateParameter}`");
                            return false;
                        }
                    }
                    else
                    { templateArguments.Add(typeParamName, typeParamValue); }
                }

                return true;
            }
            case (ArrayType v, ArrayType w):
            {
                if (!ResolveTemplateParameters(v.Of, w.Of, out error, templateArguments)) return true;
                if (v.Length is not CompiledTemplateParameterAccess templateParameterAccess) return true;

                if (w.Length is CompiledTemplateParameterAccess)
                {
                    error = new PossibleDiagnostic($"Type `{passed}` is not the same as `{defined}`");

                    if (templateArguments.ContainsKey(templateParameterAccess.Parameter.Content))
                    { return false; }
                    throw new NotImplementedException($"This should not be a template parameter");
                }

                if (w.Length is not CompiledConstantValue constantValue) return true;

                string typeParamName = templateParameterAccess.Parameter.Content;
                CompiledTemplateArgument typeParamValue = new CompiledTemplateArgumentValue()
                {
                    Definition = templateParameterAccess.Parameter,
                    Value = constantValue.Value,
                };

                if (templateArguments.TryGetValue(typeParamName, out CompiledTemplateArgument? addedTemplateParameter))
                {
                    if (!addedTemplateParameter.SameAs(typeParamValue))
                    {
                        error = new PossibleDiagnostic($"Type `{typeParamValue}` is not the same as `{addedTemplateParameter}`");
                        return false;
                    }
                }
                else
                {
                    templateArguments.Add(typeParamName, typeParamValue);
                    return true;
                }
                return true;
            }
            case (TupleType v, TupleType w):
            {
                if (v.Items.Length != w.Items.Length)
                {
                    error = new PossibleDiagnostic($"Type `{passed}` is not the same as `{defined}`");
                    return false;
                }

                for (int i = 0; i < v.Items.Length; i++)
                {
                    if (!ResolveTemplateParameters(v.Items[i], w.Items[i], out error, templateArguments)) return false;
                }

                return true;
            }
            default:
                return true;
        }
    }
}

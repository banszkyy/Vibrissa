namespace LanguageCore;

public readonly struct PreprocessorResult
{
    public readonly string Text;
    public readonly ImmutableArray<TrimmedRange> TrimmedRanges;

    public PreprocessorResult(string text, ImmutableArray<TrimmedRange> trimmedRanges)
    {
        Text = text;
        TrimmedRanges = trimmedRanges;
    }
}

enum PreprocessorParseState
{
    None,
    NewLine,
    Statement,
    Expression,
}

enum PreprocessorEvaluationState
{
    None,
    ConditionTrue,
    ConditionFalse,
    Fallthrough,
}

public readonly struct TrimmedRange
{
    public readonly int Index;
    public readonly int Absolute;
    public readonly int Lines;

    public TrimmedRange(int index, int absolute, int lines)
    {
        Index = index;
        Absolute = absolute;
        Lines = lines;
    }
}

public class Preprocessor
{
    readonly string Input;
    readonly ImmutableHashSet<string> Variables;
    readonly DiagnosticsCollection? Diagnostics;
    readonly StringBuilder Result;
    readonly ImmutableArray<TrimmedRange>.Builder TrimmedRanges;

    PreprocessorParseState ParseState;
    int SymbolStart;
    string? CurrentStatement;
    TrimmedRange? TrimmingRange;

    readonly Stack<PreprocessorEvaluationState> EvaluationState;

    Preprocessor(string input, ImmutableHashSet<string> variables, DiagnosticsCollection? diagnostics)
    {
        Input = input;
        Variables = variables;
        Diagnostics = diagnostics;
        Result = new StringBuilder();
        TrimmedRanges = ImmutableArray.CreateBuilder<TrimmedRange>();

        ParseState = PreprocessorParseState.NewLine;
        SymbolStart = -1;
        CurrentStatement = null;
        EvaluationState = new();
        EvaluationState.Push(PreprocessorEvaluationState.None);
    }

    public static PreprocessorResult Process(string input, ImmutableHashSet<string> variables, DiagnosticsCollection? diagnostics)
    {
        Preprocessor preprocessor = new(input, variables, diagnostics);
        preprocessor.ProcessImpl();
        return new PreprocessorResult(preprocessor.Result.ToString(), preprocessor.TrimmedRanges.ToImmutable());
    }

    void ProcessImpl()
    {
        for (int i = 0; i < Input.Length; i++)
        {
            char c = Input[i];

            switch (ParseState)
            {
                case PreprocessorParseState.None:
                    if (c is '\r' or '\n')
                    {
                        CurrentStatement = null;
                        ParseState = PreprocessorParseState.NewLine;
                        HandleCharacter(i, false);
                        continue;
                    }
                    else
                    {
                        CurrentStatement = null;
                        ParseState = PreprocessorParseState.None;
                        HandleCharacter(i, false);
                        continue;
                    }
                case PreprocessorParseState.NewLine:
                    if (c is '#')
                    {
                        CurrentStatement = null;
                        ParseState = PreprocessorParseState.Statement;
                        continue;
                    }
                    else if (c is '\r' or '\n')
                    {
                        CurrentStatement = null;
                        ParseState = PreprocessorParseState.NewLine;
                        HandleCharacter(i, false);
                        continue;
                    }
                    else
                    {
                        CurrentStatement = null;
                        ParseState = PreprocessorParseState.None;
                        HandleCharacter(i, false);
                        continue;
                    }
                case PreprocessorParseState.Statement:
                    if (SymbolStart == -1)
                    {
                        if (char.IsLetter(c))
                        {
                            CurrentStatement = null;
                            SymbolStart = i;
                            ParseState = PreprocessorParseState.Statement;
                            HandleCharacter(i - 1, true);
                            HandleCharacter(i, true);
                            continue;
                        }
                        else if (c is '\r' or '\n')
                        {
                            CurrentStatement = null;
                            ParseState = PreprocessorParseState.NewLine;
                            SymbolStart = -1;
                            HandleCharacter(i - i, false);
                            HandleCharacter(i, false);
                            continue;
                        }
                        else
                        {
                            CurrentStatement = null;
                            ParseState = PreprocessorParseState.None;
                            SymbolStart = -1;
                            HandleCharacter(i - i, false);
                            HandleCharacter(i, false);
                            continue;
                        }
                    }
                    else
                    {
                        if (char.IsLetter(c))
                        {
                            CurrentStatement = null;
                            ParseState = PreprocessorParseState.Statement;
                            HandleCharacter(i, true);
                            continue;
                        }
                        else
                        {
                            HandleCharacter(i, true);
                            CurrentStatement = Input[SymbolStart..i];
                            if (c is '\r' or '\n')
                            {
                                Evaluate(CurrentStatement, null);
                                ParseState = PreprocessorParseState.NewLine;
                            }
                            else
                            {
                                ParseState = PreprocessorParseState.Expression;
                            }
                            SymbolStart = -1;
                            continue;
                        }
                    }
                case PreprocessorParseState.Expression:
                    if (char.IsLetterOrDigit(c))
                    {
                        if (SymbolStart == -1)
                        {
                            SymbolStart = i;
                        }
                        ParseState = PreprocessorParseState.Expression;
                        HandleCharacter(i, true);
                        continue;
                    }
                    else if (c is '\r' or '\n')
                    {
                        if (SymbolStart != -1)
                        {
                            if (string.IsNullOrWhiteSpace(CurrentStatement))
                            { throw new UnreachableException(); }
                            HandleCharacter(i, true);
                            Evaluate(CurrentStatement, Input[SymbolStart..i]);
                            CurrentStatement = null;
                            SymbolStart = -1;
                            ParseState = PreprocessorParseState.NewLine;
                        }
                        else
                        {
                            HandleCharacter(i, true);
                            CurrentStatement = null;
                            SymbolStart = -1;
                            ParseState = PreprocessorParseState.NewLine;
                        }
                        continue;
                    }
                    else
                    {
                        HandleCharacter(i, true);
                        CurrentStatement = null;
                        SymbolStart = -1;
                        ParseState = PreprocessorParseState.None;
                        continue;
                    }
            }
        }
    }

    void HandleCharacter(int i, bool skip)
    {
        char c = Input[i];

        if (skip || EvaluationState.Any(v => v is PreprocessorEvaluationState.ConditionFalse or PreprocessorEvaluationState.Fallthrough))
        {
            if (TrimmingRange.HasValue)
            {
                TrimmingRange = new TrimmedRange(TrimmingRange.Value.Index, i - TrimmingRange.Value.Index + 1, TrimmingRange.Value.Lines + (c is '\n' ? 1 : 0));
            }
            else
            {
                TrimmingRange = new TrimmedRange(i, 1, 0);
            }
        }
        else
        {
            if (TrimmingRange.HasValue)
            {
                TrimmedRanges.Add(TrimmingRange.Value);
            }
            TrimmingRange = null;
            Result.Append(c);
        }
    }

    void Evaluate(string statement, string? expression)
    {
        switch (statement)
        {
            case "if":
            {
                if (expression is null)
                {
                    Diagnostics?.Add(Diagnostic.Error($"Preprocessor statement `#{statement}` needs an expression"));
                    EvaluationState.Push(PreprocessorEvaluationState.Fallthrough);
                    break;
                }

                EvaluationState.Push(Variables.Contains(expression) ? PreprocessorEvaluationState.ConditionTrue : PreprocessorEvaluationState.ConditionFalse);
                break;
            }
            case "elseif":
            {
                PreprocessorEvaluationState prev = EvaluationState.Pop();

                if (expression is null)
                {
                    Diagnostics?.Add(Diagnostic.Error($"Preprocessor statement `#{statement}` needs an expression"));
                    EvaluationState.Push(PreprocessorEvaluationState.Fallthrough);
                    break;
                }

                if (prev == PreprocessorEvaluationState.ConditionTrue)
                {
                    EvaluationState.Push(PreprocessorEvaluationState.Fallthrough);
                }
                else if (prev == PreprocessorEvaluationState.ConditionFalse)
                {
                    EvaluationState.Push(Variables.Contains(expression) ? PreprocessorEvaluationState.ConditionTrue : PreprocessorEvaluationState.ConditionFalse);
                }
                else if (prev == PreprocessorEvaluationState.Fallthrough)
                {
                    EvaluationState.Push(PreprocessorEvaluationState.Fallthrough);
                }
                break;
            }
            case "else":
            {
                if (expression is not null)
                {
                    Diagnostics?.Add(Diagnostic.Error($"Preprocessor statement `#{statement}` should not have an expression"));
                }

                PreprocessorEvaluationState prev = EvaluationState.Pop();
                if (prev == PreprocessorEvaluationState.ConditionTrue)
                {
                    EvaluationState.Push(PreprocessorEvaluationState.ConditionFalse);
                }
                else if (prev == PreprocessorEvaluationState.ConditionFalse)
                {
                    EvaluationState.Push(PreprocessorEvaluationState.ConditionTrue);
                }
                else if (prev == PreprocessorEvaluationState.Fallthrough)
                {
                    EvaluationState.Push(PreprocessorEvaluationState.Fallthrough);
                }
                break;
            }
            case "endif":
            {
                if (expression is not null)
                {
                    Diagnostics?.Add(Diagnostic.Error($"Preprocessor statement `#{statement}` should not have an expression"));
                }

                EvaluationState.Pop();
                break;
            }
            default:
            {
                Diagnostics?.Add(Diagnostic.Error($"Invalid preprocessor statement `#{statement}`"));
                break;
            }
        }
    }
}

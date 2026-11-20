namespace LanguageCore;

public class PreprocessorResult
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

public readonly struct TrimmedLine : IPositioned
{
    public readonly string Text;
    public readonly Position Position;
    Position IPositioned.Position => Position;

    public TrimmedLine(string text, Position position)
    {
        Text = text;
        Position = position;
    }
}

public readonly struct TrimmedRange : IPositioned
{
    public readonly ImmutableArray<TrimmedLine> Lines;
    public Position Position => new(Lines.Select(v => v as IPositioned));

    public TrimmedRange(ImmutableArray<TrimmedLine> lines)
    {
        Lines = lines;
    }
}

class TrimmingLine
{
    public readonly StringBuilder Text;
    public Position Position;

    public TrimmingLine()
    {
        Text = new StringBuilder();
    }

    public TrimmedLine Compile() => new(Text.ToString(), Position);
}

class TrimmingRange
{
    public readonly ImmutableArray<TrimmedLine>.Builder Lines;
    public TrimmingLine? TrimmingLine;

    public TrimmingRange()
    {
        Lines = ImmutableArray.CreateBuilder<TrimmedLine>();
    }

    public TrimmedRange Compile() => new(Lines.ToImmutable());
}

public class Preprocessor
{
    readonly string Input;
    readonly ImmutableHashSet<string> Variables;
    readonly DiagnosticsCollection? Diagnostics;
    readonly StringBuilder Result;

    PreprocessorParseState ParseState;
    int SymbolStart;
    string? CurrentStatement;
    readonly ImmutableArray<TrimmedRange>.Builder TrimmingRanges;
    TrimmingRange? TrimmingRange;
    SinglePosition Position;

    readonly Stack<PreprocessorEvaluationState> EvaluationState;

    Preprocessor(string input, ImmutableHashSet<string> variables, DiagnosticsCollection? diagnostics)
    {
        Input = input;
        Variables = variables;
        Diagnostics = diagnostics;
        Result = new StringBuilder();
        TrimmingRanges = ImmutableArray.CreateBuilder<TrimmedRange>();

        ParseState = PreprocessorParseState.NewLine;
        SymbolStart = -1;
        CurrentStatement = null;
        EvaluationState = new();
        EvaluationState.Push(PreprocessorEvaluationState.None);
    }

    void UpdatePosition(char c)
    {
        if (c == '\n')
        {
            Position.Line++;
            Position.Character = 0;
        }
        else
        {
            Position.Character++;
        }
    }

    public static PreprocessorResult Process(string input, ImmutableHashSet<string> variables, DiagnosticsCollection? diagnostics)
    {
        Preprocessor preprocessor = new(input, variables, diagnostics);
        preprocessor.ProcessImpl();
        return new PreprocessorResult(preprocessor.Result.ToString(), preprocessor.TrimmingRanges.ToImmutable());
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
                        break;
                    }
                    else
                    {
                        CurrentStatement = null;
                        ParseState = PreprocessorParseState.None;
                        HandleCharacter(i, false);
                        break;
                    }
                case PreprocessorParseState.NewLine:
                    if (c is '#')
                    {
                        CurrentStatement = null;
                        ParseState = PreprocessorParseState.Statement;
                        break;
                    }
                    else if (c is '\r' or '\n')
                    {
                        CurrentStatement = null;
                        ParseState = PreprocessorParseState.NewLine;
                        HandleCharacter(i, false);
                        break;
                    }
                    else
                    {
                        CurrentStatement = null;
                        ParseState = PreprocessorParseState.None;
                        HandleCharacter(i, false);
                        break;
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
                            break;
                        }
                        else if (c is '\r' or '\n')
                        {
                            CurrentStatement = null;
                            ParseState = PreprocessorParseState.NewLine;
                            SymbolStart = -1;
                            HandleCharacter(i - i, false);
                            HandleCharacter(i, false);
                            break;
                        }
                        else
                        {
                            CurrentStatement = null;
                            ParseState = PreprocessorParseState.None;
                            SymbolStart = -1;
                            HandleCharacter(i - i, false);
                            HandleCharacter(i, false);
                            break;
                        }
                    }
                    else
                    {
                        if (char.IsLetter(c))
                        {
                            CurrentStatement = null;
                            ParseState = PreprocessorParseState.Statement;
                            HandleCharacter(i, true);
                            break;
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
                            break;
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
                        break;
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
                        break;
                    }
                    else
                    {
                        HandleCharacter(i, true);
                        CurrentStatement = null;
                        SymbolStart = -1;
                        ParseState = PreprocessorParseState.None;
                        break;
                    }
            }
        }
    }

    void HandleCharacter(int i, bool skip)
    {
        char c = Input[i];

        if (skip || EvaluationState.Any(v => v is PreprocessorEvaluationState.ConditionFalse or PreprocessorEvaluationState.Fallthrough))
        {
            TrimmingRange ??= new TrimmingRange();

            if (c == '\n')
            {
                if (TrimmingRange.TrimmingLine is not null)
                {
                    TrimmingRange.TrimmingLine.Text.Append(c);
                    TrimmingRange.Lines.Add(TrimmingRange.TrimmingLine.Compile());
                }

                UpdatePosition(c);
                TrimmingRange.TrimmingLine = new TrimmingLine
                {
                    Position = new Position(new Range<SinglePosition>(Position), new Range<int>(i))
                };
            }
            else
            {
                TrimmingRange.TrimmingLine ??= new TrimmingLine()
                {
                    Position = new Position(new Range<SinglePosition>(Position), new Range<int>(i)),
                };
                UpdatePosition(c);
                TrimmingRange.TrimmingLine.Position = new Position(new Range<SinglePosition>(TrimmingRange.TrimmingLine.Position.Range.Start, Position), new Range<int>(TrimmingRange.TrimmingLine.Position.AbsoluteRange.Start, i));
                TrimmingRange.TrimmingLine.Text.Append(c);
            }
        }
        else
        {
            UpdatePosition(c);

            if (TrimmingRange is not null)
            {
                if (TrimmingRange.TrimmingLine is not null)
                {
                    TrimmingRange.Lines.Add(TrimmingRange.TrimmingLine.Compile());
                }
                TrimmingRanges.Add(TrimmingRange.Compile());
                TrimmingRange = null;
            }

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

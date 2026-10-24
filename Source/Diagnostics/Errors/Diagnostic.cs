namespace LanguageCore;

[ExcludeFromCodeCoverage]
public class Diagnostic : IEquatable<Diagnostic>
{
    public DiagnosticsLevel Level { get; }
    public string Message { get; }
    public ImmutableArray<DiagnosticRelatedInformation> RelatedInformation { get; }
    public ImmutableArray<Diagnostic> SubErrors { get; }
    public bool IgnoreOnPartialSource { get; }
    public bool IsTellingNothing { get; }

#if DEBUG && !UNITY
    bool IsDebugged;
#endif

    protected Diagnostic(DiagnosticsLevel level, string message, bool @break, bool ignoreOnPartialSource, bool isTellingNothing, ImmutableArray<Diagnostic> suberrors, ImmutableArray<DiagnosticRelatedInformation> relatedInformation)
    {
        Level = level;
        Message = message;
        SubErrors = suberrors;
        RelatedInformation = relatedInformation;
        IgnoreOnPartialSource = ignoreOnPartialSource;
        IsTellingNothing = isTellingNothing;

        if (@break)
        { Break(); }
    }

    public Diagnostic(DiagnosticsLevel level, string message, ImmutableArray<Diagnostic> suberrors, ImmutableArray<DiagnosticRelatedInformation> relatedInformation)
        : this(level, message, level == DiagnosticsLevel.Error, false, false, suberrors, relatedInformation) { }

    public virtual Diagnostic WithSuberrors(Diagnostic? suberror) => suberror is null ? this : new(Level, Message, false, IgnoreOnPartialSource, IsTellingNothing, ImmutableArray.Create(suberror), RelatedInformation);
    public virtual Diagnostic WithSuberrors(params Diagnostic?[] suberrors) => WithSuberrors(suberrors.Where(v => v is not null).ToImmutableArray()!);
    public virtual Diagnostic WithSuberrors(IEnumerable<Diagnostic?> suberrors) => WithSuberrors(suberrors.Where(v => v is not null).ToImmutableArray()!);
    public virtual Diagnostic WithSuberrors(ImmutableArray<Diagnostic> suberrors) => suberrors.IsDefaultOrEmpty ? this : new(Level, Message, false, IgnoreOnPartialSource, IsTellingNothing, SubErrors.AddRange(suberrors), RelatedInformation);

    public virtual Diagnostic WithRelatedInfo(DiagnosticRelatedInformation? relatedInfo) => relatedInfo is null ? this : new(Level, Message, false, IgnoreOnPartialSource, IsTellingNothing, SubErrors, ImmutableArray.Create(relatedInfo));
    public virtual Diagnostic WithRelatedInfo(params DiagnosticRelatedInformation?[] relatedInfo) => WithRelatedInfo(relatedInfo.Where(v => v is not null).ToImmutableArray()!);
    public virtual Diagnostic WithRelatedInfo(IEnumerable<DiagnosticRelatedInformation?> relatedInfo) => WithRelatedInfo(relatedInfo.Where(v => v is not null).ToImmutableArray()!);
    public virtual Diagnostic WithRelatedInfo(ImmutableArray<DiagnosticRelatedInformation> relatedInfo) => relatedInfo.IsDefaultOrEmpty ? this : new(Level, Message, false, IgnoreOnPartialSource, IsTellingNothing, SubErrors, RelatedInformation.AddRange(relatedInfo));

    [DoesNotReturn]
    public virtual void Throw() => throw ToException();

    public virtual LanguageException ToException() => new(Message, SubErrors.ToImmutableArray(v => v.ToException() as Exception));

    public static Diagnostic Internal(string message, bool @break = true)
        => new(DiagnosticsLevel.Error, message, @break, false, false, ImmutableArray<Diagnostic>.Empty, ImmutableArray<DiagnosticRelatedInformation>.Empty);

    public static Diagnostic Error(string message, bool @break = true, bool ignoreOnPartialSource = false, bool isTellingNothing = false)
        => new(DiagnosticsLevel.Error, message, @break, ignoreOnPartialSource, isTellingNothing, ImmutableArray<Diagnostic>.Empty, ImmutableArray<DiagnosticRelatedInformation>.Empty);

    public static Diagnostic Warning(string message, bool @break = true, bool ignoreOnPartialSource = false, bool isTellingNothing = false)
        => new(DiagnosticsLevel.Warning, message, @break, ignoreOnPartialSource, isTellingNothing, ImmutableArray<Diagnostic>.Empty, ImmutableArray<DiagnosticRelatedInformation>.Empty);

    public virtual Diagnostic Break()
    {
#if DEBUG && !UNITY
        if (!IsDebugged)
        {
            Debugger.Break();
        }
        IsDebugged = true;
#endif

#if TESTING
        //Throw();
#endif

        return this;
    }

    public override string ToString() => Message;

    public bool Equals([NotNullWhen(true)] Diagnostic? other)
    {
        if (other is null) return false;
        if (Message != other.Message) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is Diagnostic other && Equals(other);

    public override int GetHashCode() => Message.GetHashCode();

    public static IEnumerable<Diagnostic> EnumerateAll(Diagnostic diagnostic)
    {
        yield return diagnostic;
        foreach (Diagnostic sub in diagnostic.SubErrors.SelectMany(EnumerateAll))
        {
            yield return sub;
        }
    }

    public static Diagnostic FromException(Exception exception) => exception switch
    {
        LanguageExceptionAt ex => ex.ToDiagnostic(),
        LanguageException ex => ex.ToDiagnostic(),
        AggregateException ex => new Diagnostic(DiagnosticsLevel.Error, ex.Message, false, false, false, ex.InnerExceptions is null ? ImmutableArray<Diagnostic>.Empty : ex.InnerExceptions.ToImmutableArray(FromException), ImmutableArray<DiagnosticRelatedInformation>.Empty),
        _ => new Diagnostic(DiagnosticsLevel.Error, exception.Message, false, false, false, exception.InnerException is null ? ImmutableArray<Diagnostic>.Empty : ImmutableArray.Create(FromException(exception.InnerException)), ImmutableArray<DiagnosticRelatedInformation>.Empty)
    };

    void ReduceNonTellingInternal(List<Diagnostic> result)
    {
        if (IsTellingNothing)
        {
            foreach (Diagnostic subError in SubErrors)
            {
                subError.ReduceNonTellingInternal(result);
            }
        }
        else
        {
            result.Add(this);
        }
    }

    public IEnumerable<Diagnostic> ReduceNonTelling()
    {
        List<Diagnostic> result = new();

        if (IsTellingNothing)
        {
            foreach (Diagnostic subError in SubErrors)
            {
                subError.ReduceNonTellingInternal(result);
            }
        }
        else
        {
            result.Add(this);
        }

        if (result.Count == 0)
        {
            result.Add(this);
        }

        return result;
    }
}

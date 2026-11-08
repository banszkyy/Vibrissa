namespace LanguageCore.Compiler;

public class TemplateInstance<T> where T : notnull
{
    public readonly T Template;
    public readonly ImmutableDictionary<string, CompiledTemplateArgument>? TemplateArguments;

    public TemplateInstance(T template, ImmutableDictionary<string, CompiledTemplateArgument>? templateArguments)
    {
        Template = template;
        TemplateArguments = templateArguments;
    }

    public override string ToString() => Template?.ToString() ?? "null";

    [SuppressMessage("Quality", "MY003")]
    public TemplateInstance<V> UnsafeTo<V>() where V : notnull => new((V)(object)Template, TemplateArguments);
}

public static class TemplateInstance
{
    [return: NotNullIfNotNull(nameof(template))]
    public static TemplateInstance<T>? New<T>(T? template, ImmutableDictionary<string, CompiledTemplateArgument>? templateArguments)
        where T : notnull
        => template is null ? null : new TemplateInstance<T>(template, templateArguments);

    [return: NotNullIfNotNull(nameof(template))]
    public static TemplateInstance<T>? New<T>(StatementCompiler.FunctionQueryResult<T>? template)
        where T : notnull
        => template is null ? null : new TemplateInstance<T>(template.Function, template.TemplateArguments);
}

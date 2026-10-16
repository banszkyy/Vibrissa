namespace LanguageCore.Compiler;

public class CompiledHeapAllocation : CompiledExpression, IHaveAllocator
{
    public required TemplateInstance<CompiledFunctionDefinition> Allocator { get; init; }
    public required CompiledTypeExpression TypeExpression { get; init; }

    CompiledExpression? IHaveAllocator.Allocator => this;

    public override string ToString() => $"new {TypeExpression}";
}

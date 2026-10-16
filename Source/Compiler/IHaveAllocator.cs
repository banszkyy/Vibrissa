namespace LanguageCore.Compiler;

public interface IHaveAllocator
{
    CompiledExpression? Allocator { get; }
}

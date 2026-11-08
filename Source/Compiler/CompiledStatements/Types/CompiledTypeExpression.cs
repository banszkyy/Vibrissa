using LanguageCore.Parser;
using LanguageCore.Tokenizing;

namespace LanguageCore.Compiler;

public abstract class CompiledTypeExpression : CompiledStatement,
    IEquatable<CompiledTypeExpression>,
    IEquatable<TypeInstance>,
    IEquatable<BasicType>,
    IEquatable<RuntimeType>,
    ILocated,
    IPositioned,
    IInFile
{
    public virtual CompiledTypeExpression FinalValue => this;

    [SetsRequiredMembers]
    public CompiledTypeExpression(Location location)
    {
        Location = location;
    }

    public static CompiledTypeExpression CreateAnonymous(GeneralType type, ILocated location) => type switch
    {
        AliasType v => CompiledAliasTypeExpression.CreateAnonymous(v, location),
        EnumType v => CompiledEnumTypeExpression.CreateAnonymous(v, location),
        ArrayType v => CompiledArrayTypeExpression.CreateAnonymous(v, location),
        BuiltinType v => CompiledBuiltinTypeExpression.CreateAnonymous(v, location),
        FunctionType v => CompiledFunctionTypeExpression.CreateAnonymous(v, location),
        UnresolvedTemplateType v => CompiledUnresolvedTemplateTypeExpression.CreateAnonymous(v, location),
        PointerType v => CompiledPointerTypeExpression.CreateAnonymous(v, location),
        ReferenceType v => CompiledReferenceTypeExpression.CreateAnonymous(v, location),
        StructType v => CompiledStructTypeExpression.CreateAnonymous(v, location),
        TupleType => throw new NotImplementedException(),
        _ => throw new UnreachableException(),
    };

    [Obsolete]
    public static bool operator ==(CompiledTypeExpression? a, CompiledTypeExpression? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        return a.Equals(b);
    }
    [Obsolete]
    public static bool operator !=(CompiledTypeExpression? a, CompiledTypeExpression? b) => !(a == b);

    [Obsolete]
    public static bool operator ==([NotNullWhen(true)] CompiledTypeExpression? a, RuntimeType b) => a is not null && a.Equals(b);
    [Obsolete]
    public static bool operator !=(CompiledTypeExpression? a, RuntimeType b) => !(a == b);

    [Obsolete]
    public static bool operator ==([NotNullWhen(true)] CompiledTypeExpression? a, BasicType b) => a is not null && a.Equals(b);
    [Obsolete]
    public static bool operator !=(CompiledTypeExpression? a, BasicType b) => !(a == b);

    public bool SameAs([NotNullWhen(true)] CompiledTypeExpression? other)
    {
        if (other is null) return false;
        return FinalValue.Equals(other.FinalValue);
    }
    public bool SameAs(BasicType other) => FinalValue.Equals(other);

    public bool Is<T>([NotNullWhen(true)] out T? value) where T : CompiledTypeExpression
    {
        if (FinalValue is T _value)
        {
            value = _value;
            return true;
        }
        else
        {
            value = null;
            return false;
        }
    }

    public abstract override bool Equals(object? obj);
    public abstract bool Equals(CompiledTypeExpression? other);
    public abstract bool Equals(TypeInstance? other);
    public abstract override int GetHashCode();
    public abstract override string ToString();
    public bool Equals(BasicType other) => FinalValue is CompiledBuiltinTypeExpression builtinType && builtinType.Type == other;
    public bool Equals(RuntimeType other) => FinalValue switch
    {
        CompiledBuiltinTypeExpression builtinType => builtinType.RuntimeType == other,
        CompiledPointerTypeExpression => other == RuntimeType.I32,
        _ => false,
    };

    public static bool TryGetTemplateParameters(CompiledTypeExpression defined, CompiledTypeExpression passed, Dictionary<string, CompiledTemplateArgumentExpression> templateParameters)
    {
        if (passed.Is(out CompiledUnresolvedTemplateTypeExpression? passedGenericType))
        {
            if (templateParameters.ContainsKey(passedGenericType.Identifier))
            { return false; }
            throw new NotImplementedException($"This should not be a template parameter");
        }

        if (defined.Is(out CompiledUnresolvedTemplateTypeExpression? definedGenericType))
        {
            if (templateParameters.TryGetValue(definedGenericType.Identifier, out CompiledTemplateArgumentExpression? addedTemplateParameter))
            {
                if (addedTemplateParameter is not CompiledTemplateArgumentTypeExpression w) return false;
                if (!w.Value.SameAs(passed)) return false;
            }
            else
            {
                templateParameters.Add(definedGenericType.Identifier, new CompiledTemplateArgumentTypeExpression()
                {
                    Value = passed,
                });
            }

            return true;
        }

        if (defined.Is(out CompiledPointerTypeExpression? definedPointerType) && passed.Is(out CompiledPointerTypeExpression? passedPointerType))
        {
            if (definedPointerType.To is CompiledArrayTypeExpression definedArrayType && passedPointerType.To is CompiledArrayTypeExpression passedArrayType)
            {
                if (definedArrayType.ComputedLength.HasValue)
                {
                    if (!passedArrayType.ComputedLength.HasValue) return false;
                    if (definedArrayType.ComputedLength.Value != passedArrayType.ComputedLength.Value) return false;
                }

                return TryGetTemplateParameters(definedArrayType.Of, passedArrayType.Of, templateParameters);
            }

            return TryGetTemplateParameters(definedPointerType.To, passedPointerType.To, templateParameters);
        }

        if (defined.Is(out CompiledReferenceTypeExpression? definedReferenceType) && passed.Is(out CompiledReferenceTypeExpression? passedReferenceType))
        {
            return TryGetTemplateParameters(definedReferenceType.To, passedReferenceType.To, templateParameters);
        }

        if (defined.Is(out CompiledFunctionTypeExpression? definedFunctionType) && passed.Is(out CompiledFunctionTypeExpression? passedFunctionType))
        {
            if (definedFunctionType.Parameters.Length != passedFunctionType.Parameters.Length) return false;
            for (int i = 0; i < definedFunctionType.Parameters.Length; i++)
            {
                if (!TryGetTemplateParameters(definedFunctionType.Parameters[i], passedFunctionType.Parameters[i], templateParameters)) return false;
            }
            if (!TryGetTemplateParameters(definedFunctionType.ReturnType, passedFunctionType.ReturnType, templateParameters)) return false;
            return true;
        }

        if (defined.Is(out CompiledStructTypeExpression? definedStructType) && passed.Is(out CompiledStructTypeExpression? passedStructType))
        {
            if (definedStructType.Struct.Identifier != passedStructType.Struct.Identifier) return false;
            if (definedStructType.Struct.Definition.Template is not null && passedStructType.Struct.Definition.Template is not null)
            {
                if (definedStructType.Struct.Definition.Template.Parameters.Length != passedStructType.TemplateArguments.Count)
                { throw new NotImplementedException(); }

                for (int i = 0; i < definedStructType.Struct.Definition.Template.Parameters.Length; i++)
                {
                    string typeParamName = definedStructType.Struct.Definition.Template.Parameters[i].Content;
                    CompiledTemplateArgumentExpression typeParamValue = passedStructType.TemplateArguments[typeParamName];

                    if (templateParameters.TryGetValue(typeParamName, out CompiledTemplateArgumentExpression? addedTemplateParameter))
                    { if (!addedTemplateParameter.SameAs(typeParamValue)) return false; }
                    else
                    { templateParameters.Add(typeParamName, typeParamValue); }
                }

                return true;
            }
        }

        if (!defined.SameAs(passed)) return false;

        return true;
    }

    public bool AllGenericsDefined()
    {
        switch (FinalValue)
        {
            case CompiledUnresolvedTemplateTypeExpression: return false;
            case CompiledBuiltinTypeExpression: return true;
            case CompiledPointerTypeExpression pointerType: return pointerType.To.AllGenericsDefined();
            case CompiledReferenceTypeExpression referenceType: return referenceType.To.AllGenericsDefined();
            case CompiledArrayTypeExpression arrayType: return arrayType.Of.AllGenericsDefined();

            case CompiledFunctionTypeExpression functionType:
            {
                for (int i = 0; i < functionType.Parameters.Length; i++)
                {
                    if (!functionType.Parameters[i].AllGenericsDefined())
                    { return false; }
                }
                return functionType.ReturnType.AllGenericsDefined();
            }

            case CompiledStructTypeExpression structType:
            {
                if (structType.Struct.Definition.Template is null) return true;
                return structType.TemplateArguments is not null;
            }

            default: throw new NotImplementedException();
        }
    }

    [return: NotNullIfNotNull(nameof(templateArguments))]
    public static CompiledTypeExpression? InsertTemplateParameters(CompiledTypeExpression type, IReadOnlyDictionary<string, CompiledTemplateArgumentExpression>? templateArguments)
    {
        if (templateArguments is null) return null;

        switch (type)
        {
            case CompiledUnresolvedTemplateTypeExpression templateType:
            {
                if (!templateArguments.TryGetValue(templateType.Identifier, out CompiledTemplateArgumentExpression? passedTemplateArgument))
                { throw new InternalExceptionWithoutContext(); }
                if (passedTemplateArgument is not CompiledTemplateArgumentTypeExpression w) throw new NotImplementedException();
                return w.Value;
            }

            case CompiledPointerTypeExpression pointerType:
            {
                CompiledTypeExpression pointerTo = InsertTemplateParameters(pointerType.To, templateArguments);
                return new CompiledPointerTypeExpression(pointerTo, type.Location);
            }

            case CompiledReferenceTypeExpression pointerType:
            {
                CompiledTypeExpression pointerTo = InsertTemplateParameters(pointerType.To, templateArguments);
                return new CompiledReferenceTypeExpression(pointerTo, type.Location);
            }

            case CompiledArrayTypeExpression arrayType:
            {
                CompiledTypeExpression stackArrayOf = InsertTemplateParameters(arrayType.Of, templateArguments);
                return new CompiledArrayTypeExpression(stackArrayOf, arrayType.Length, type.Location);
            }

            case CompiledStructTypeExpression structType:
            {
                if (structType.Struct.Definition.Template is not null)
                {
                    ImmutableDictionary<string, CompiledTemplateArgumentExpression>.Builder structTemplateArguments = ImmutableDictionary.CreateBuilder<string, CompiledTemplateArgumentExpression>();
                    foreach (Token templateParameter in structType.Struct.Definition.Template.Parameters)
                    {
                        if (!templateArguments.TryGetValue(templateParameter.Content, out CompiledTemplateArgumentExpression? v))
                        {
                            return type;
                        }
                        structTemplateArguments.Add(templateParameter.Content, v);
                    }

                    return new CompiledStructTypeExpression(structType.Struct, structType.File, structTemplateArguments.ToImmutable(), type.Location);
                }

                return type;
            }

            case CompiledBuiltinTypeExpression:
                return type;

            case CompiledAliasTypeExpression: // todo
                return type;

            case CompiledEnumTypeExpression:
                return type;

            case CompiledFunctionTypeExpression functionType:
            {
                CompiledTypeExpression returnType = InsertTemplateParameters(functionType.ReturnType, templateArguments);
                ImmutableArray<CompiledTypeExpression> parameters = InsertTemplateParameters(functionType.Parameters, templateArguments);
                return new CompiledFunctionTypeExpression(returnType, parameters, functionType.HasClosure, type.Location);
            }

            default:
                throw new NotImplementedException();
        }
    }

    public static ImmutableArray<CompiledTypeExpression> InsertTemplateParameters(ImmutableArray<CompiledTypeExpression> types, IReadOnlyDictionary<string, CompiledTemplateArgumentExpression> templateArguments)
    {
        ImmutableArray<CompiledTypeExpression>.Builder result = ImmutableArray.CreateBuilder<CompiledTypeExpression>(types.Length);
        foreach (CompiledTypeExpression type in types)
        {
            result.Add(InsertTemplateParameters(type, templateArguments) ?? type);
        }
        return result.MoveToImmutable();
    }
}

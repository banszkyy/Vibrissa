using LanguageCore.Parser;

namespace LanguageCore.Compiler;

public abstract class GeneralType :
    IEquatable<GeneralType>,
    IEquatable<TypeInstance>,
    IEquatable<BasicType>,
    IEquatable<RuntimeType>
{
    public virtual GeneralType FinalValue => this;

    [Obsolete]
    public static bool operator ==(GeneralType? a, GeneralType? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        return a.Equals(b);
    }
    [Obsolete]
    public static bool operator !=(GeneralType? a, GeneralType? b) => !(a == b);

    [Obsolete]
    public static bool operator ==([NotNullWhen(true)] GeneralType? a, RuntimeType b) => a is not null && a.Equals(b);
    [Obsolete]
    public static bool operator !=(GeneralType? a, RuntimeType b) => !(a == b);

    [Obsolete]
    public static bool operator ==([NotNullWhen(true)] GeneralType? a, BasicType b) => a is not null && a.Equals(b);
    [Obsolete]
    public static bool operator !=(GeneralType? a, BasicType b) => !(a == b);

    public bool SameAs([NotNullWhen(true)] GeneralType? other)
    {
        if (other is null) return false;
        return FinalValue.Equals(other.FinalValue);
    }
    public bool SameAs(RuntimeType other) => FinalValue.Equals(other);
    public bool SameAs(BasicType other) => FinalValue.Equals(other);

    public bool Is<T>() => FinalValue is T;
    public bool Is<T>([NotNullWhen(true)] out T? value)
    {
        if (FinalValue is T _value)
        {
            value = _value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public abstract override bool Equals(object? obj);
    public abstract bool Equals(GeneralType? other);
    public abstract bool Equals(TypeInstance? other);
    public abstract override int GetHashCode();
    public abstract override string ToString();
    public bool Equals(BasicType other) => FinalValue is BuiltinType builtinType && builtinType.Type == other;
    public bool Equals(RuntimeType other) => FinalValue switch
    {
        BuiltinType builtinType => builtinType.TryGetRuntimeType(out RuntimeType runtimeType) && runtimeType == other,
        PointerType => other == RuntimeType.I32,
        _ => false,
    };

    public static bool TryGetTemplateParameters(GeneralType defined, GeneralType passed, Dictionary<string, CompiledTemplateArgument> templateArguments)
    {
        if (passed.Is(out UnresolvedTemplateType? passedGenericType))
        {
            if (templateArguments.ContainsKey(passedGenericType.Identifier))
            { return false; }
            throw new NotImplementedException($"This should be a template parameter");
        }

        if (defined.Is(out UnresolvedTemplateType? definedGenericType))
        {
            if (templateArguments.TryGetValue(definedGenericType.Identifier, out CompiledTemplateArgument? addedTemplateParameter))
            {
                if (addedTemplateParameter is not CompiledTemplateArgumentType w) return false;
                if (!w.Value.SameAs(passed)) return false;
            }
            else
            {
                templateArguments.Add(definedGenericType.Identifier, new CompiledTemplateArgumentType()
                {
                    Value = passed,
                    Definition = definedGenericType.Definition,
                });
            }

            return true;
        }

        if (defined.Is(out IReferenceType? definedPointerType) && passed.Is(out IReferenceType? passedPointerType))
        {
            if (definedPointerType.To is ArrayType definedArrayType && passedPointerType.To is ArrayType passedArrayType)
            {
                if (definedArrayType.Length is not null)
                {
                    if (passedArrayType.Length is null) return false;
                    if (!definedArrayType.Length.SameAs(passedArrayType.Length)) return false;
                }

                return TryGetTemplateParameters(definedArrayType.Of, passedArrayType.Of, templateArguments);
            }

            return TryGetTemplateParameters(definedPointerType.To, passedPointerType.To, templateArguments);
        }

        if (defined.Is(out ReferenceType? definedReferenceType) && passed.Is(out ReferenceType? passedReferenceType))
        {
            return TryGetTemplateParameters(definedReferenceType.To, passedReferenceType.To, templateArguments);
        }

        if (defined.Is(out FunctionType? definedFunctionType) && passed.Is(out FunctionType? passedFunctionType))
        {
            if (definedFunctionType.Parameters.Length != passedFunctionType.Parameters.Length) return false;
            for (int i = 0; i < definedFunctionType.Parameters.Length; i++)
            {
                if (!TryGetTemplateParameters(definedFunctionType.Parameters[i], passedFunctionType.Parameters[i], templateArguments)) return false;
            }
            if (!TryGetTemplateParameters(definedFunctionType.ReturnType, passedFunctionType.ReturnType, templateArguments)) return false;
            return true;
        }

        if (defined.Is(out StructType? definedStructType) && passed.Is(out StructType? passedStructType))
        {
            if (definedStructType.Struct.Identifier != passedStructType.Struct.Identifier) return false;
            if (definedStructType.Struct.Definition.Template is not null && passedStructType.Struct.Definition.Template is not null)
            {
                if (definedStructType.Struct.Definition.Template.Parameters.Length != passedStructType.TemplateArguments.Count)
                { throw new NotImplementedException(); }

                for (int i = 0; i < definedStructType.Struct.Definition.Template.Parameters.Length; i++)
                {
                    string typeParamName = definedStructType.Struct.Definition.Template.Parameters[i].Content;
                    CompiledTemplateArgument typeParamValue = passedStructType.TemplateArguments[typeParamName];

                    if (templateArguments.TryGetValue(typeParamName, out CompiledTemplateArgument? addedTemplateParameter))
                    { if (!addedTemplateParameter.SameAs(typeParamValue)) return false; }
                    else
                    { templateArguments.Add(typeParamName, typeParamValue); }
                }

                return true;
            }
        }

        if (passed.Is(out ReferenceType? passedReferenceType2))
        {
            if (TryGetTemplateParameters(defined, passedReferenceType2.To, templateArguments))
            {
                return true;
            }
        }

        if (defined.Is(out TupleType? definedTupleType) && passed.Is(out TupleType? passedTupleType))
        {
            if (definedTupleType.Items.Length != passedTupleType.Items.Length) return false;
            for (int i = 0; i < definedTupleType.Items.Length; i++)
            {
                if (!TryGetTemplateParameters(definedTupleType.Items[i], passedTupleType.Items[i], templateArguments)) return false;
            }
            return true;
        }

        if (!defined.SameAs(passed)) return false;

        return true;
    }

    public bool AllGenericsDefined()
    {
        switch (FinalValue)
        {
            case UnresolvedTemplateType: return false;
            case BuiltinType: return true;
            case PointerType pointerType: return pointerType.To.AllGenericsDefined();
            case ReferenceType pointerType: return pointerType.To.AllGenericsDefined();
            case ArrayType arrayType: return arrayType.Of.AllGenericsDefined();

            case FunctionType functionType:
            {
                for (int i = 0; i < functionType.Parameters.Length; i++)
                {
                    if (!functionType.Parameters[i].AllGenericsDefined())
                    { return false; }
                }
                return functionType.ReturnType.AllGenericsDefined();
            }

            case StructType structType:
            {
                if (structType.Struct.Definition.Template is null) return true;
                return structType.TemplateArguments is not null;
            }

            case TupleType tupleType:
            {
                for (int i = 0; i < tupleType.Items.Length; i++)
                {
                    if (!tupleType.Items[i].AllGenericsDefined())
                    { return false; }
                }
                return true;
            }

            default: throw new NotImplementedException();
        }
    }

    public static GeneralType TryInsertTemplateParameters(GeneralType type, IReadOnlyDictionary<string, CompiledTemplateArgument>? templateArguments)
        => InsertTemplateParameters(type, templateArguments) ?? type;

    [return: NotNullIfNotNull(nameof(templateArguments))]
    public static GeneralType? InsertTemplateParameters(GeneralType type, IReadOnlyDictionary<string, CompiledTemplateArgument>? templateArguments)
    {
        if (templateArguments is null) return null;

        switch (type)
        {
            case UnresolvedTemplateType templateType:
            {
                if (!templateArguments.TryGetValue(templateType.Identifier, out CompiledTemplateArgument? passedTemplateArgument))
                { return type; }
                if (passedTemplateArgument is not CompiledTemplateArgumentType w) throw new NotImplementedException();
                return w.Value;
            }

            case PointerType pointerType:
                return new PointerType(InsertTemplateParameters(pointerType.To, templateArguments));

            case ReferenceType referenceType:
                return new ReferenceType(InsertTemplateParameters(referenceType.To, templateArguments));

            case ArrayType arrayType:
            {
                GeneralType of = InsertTemplateParameters(arrayType.Of, templateArguments);
                if (arrayType.Length is CompiledTemplateParameterAccess compiledTemplateParameterAccess
                    && templateArguments.TryGetValue(compiledTemplateParameterAccess.Parameter.Content, out CompiledTemplateArgument? v))
                {
                    if (v is not CompiledTemplateArgumentValue w) throw new LanguageExceptionAt($"Template argument `{compiledTemplateParameterAccess.Parameter}` is not a value", compiledTemplateParameterAccess.Location.Position, compiledTemplateParameterAccess.Location.File);
                    return new ArrayType(of, new CompiledConstantValue()
                    {
                        Value = w.Value,
                        Location = arrayType.Length.Location,
                        SaveValue = true,
                        Type = BuiltinType.Create(w.Value.Type),
                    });
                }
                else
                {
                    return new ArrayType(of, arrayType.Length);
                }
            }

            case StructType structType:
            {
                if (structType.Struct.Definition.Template is not null)
                {
                    CompiledTemplateArgument[] structTemplateParameterValues = new CompiledTemplateArgument[structType.Struct.Definition.Template.Parameters.Length];

                    foreach (KeyValuePair<string, CompiledTemplateArgument> item in templateArguments)
                    {
                        if (structType.Struct.Definition.Template.TryGetTemplateArgumentIndex(item.Key, out int i))
                        { structTemplateParameterValues[i] = item.Value; }
                    }

                    for (int i = 0; i < structTemplateParameterValues.Length; i++)
                    {
                        if (structTemplateParameterValues[i] is null) // TODO or GenericType
                        { return type; }
                    }

                    return new StructType(structType.Struct, structType.File, structTemplateParameterValues);
                }

                return type;
            }

            case BuiltinType:
                return type;

            case AliasType: // t
                return type;

            case EnumType:
                return type;

            case FunctionType functionType:
                return new FunctionType(InsertTemplateParameters(functionType.ReturnType, templateArguments), InsertTemplateParameters(functionType.Parameters, templateArguments), functionType.HasClosure);

            case TupleType tupleType:
                return new TupleType(InsertTemplateParameters(tupleType.Items, templateArguments));

            default:
                throw new NotImplementedException();
        }
    }

    public static GeneralType TryInsertConstants(GeneralType type, GeneralType source)
        => InsertConstants(type, source) ?? type;

    public static GeneralType? InsertConstants(GeneralType type, GeneralType source)
    {
        switch (type, source.FinalValue)
        {
            case (ArrayType a, ArrayType b):
            {
                if (a.Length is null && b.Length is not null) return new ArrayType(a.Of, b.Length);
                break;
            }
            case (PointerType a, PointerType b):
            {
                GeneralType? to = InsertConstants(a.To, b.To);
                if (to is not null) return new PointerType(to);
                break;
            }
            case (ReferenceType a, ReferenceType b):
            {
                GeneralType? to = InsertConstants(a.To, b.To);
                if (to is not null) return new ReferenceType(to);
                break;
            }
            default:
                break;
        }
        return null;
    }

    public static ImmutableArray<GeneralType> InsertTemplateParameters(ImmutableArray<GeneralType> types, IReadOnlyDictionary<string, CompiledTemplateArgument> templateArguments)
    {
        ImmutableArray<GeneralType>.Builder result = ImmutableArray.CreateBuilder<GeneralType>(types.Length);
        foreach (GeneralType type in types)
        {
            result.Add(TryInsertTemplateParameters(type, templateArguments));
        }
        return result.MoveToImmutable();
    }
}

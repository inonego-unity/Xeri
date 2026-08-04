/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ManagedReferenceTypeCatalog.cs
수정일 : 2026-08-05

# 설명
SerializeReference 필드에 생성할 구현 타입과 generic argument 타입 후보를 제공한다.
Unity Player assembly의 직렬화 가능한 타입과 후보 목록을 domain reload 단위로 캐싱한다.

# 특이사항
선언 타입으로 확정 가능한 generic argument는 후보를 반환하기 전에 닫고,
남은 argument만 생성 UI가 구성한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 선언 타입에 할당할 수 있는 managed-reference 생성 후보를 표현한다.
    /// </summary>
    // ============================================================
    internal sealed class ManagedReferenceCreationCandidate
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 즉시 생성할 closed type 또는 추가 구성이 필요한 generic definition.
        /// </summary>
        // ------------------------------------------------------------
        public Type Type { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 선언 타입과의 대입 관계에서 이미 확정된 generic argument이다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyDictionary<Type, Type> FixedArguments => fixedArguments;
        private readonly IReadOnlyDictionary<Type, Type> fixedArguments = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Picker에 표시할 선언 타입 계약을 반영한 이름이다.
        /// </summary>
        // ------------------------------------------------------------
        public string DisplayName => ManagedReferenceTypeCatalog.GetDisplayName(Type, fixedArguments);

        // ------------------------------------------------------------
        /// <summary>
        /// 남은 generic argument 구성이 필요한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool RequiresGenericTypeCreation => Type.IsGenericTypeDefinition;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 생성 후보와 선언 타입에서 확정된 argument를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public ManagedReferenceCreationCandidate
        (
            Type type,
            IReadOnlyDictionary<Type, Type> fixedArguments = null
        )
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            this.fixedArguments = fixedArguments == null
                ? new Dictionary<Type, Type>()
                : new Dictionary<Type, Type>(fixedArguments);
        }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// managed-reference 생성 및 generic argument 후보를 제공한다.
    /// </summary>
    // ============================================================
    internal static class ManagedReferenceTypeCatalog
    {
    #region 내부 데이터

        private static readonly IReadOnlyDictionary<Type, string> builtInTypeNames =
            new Dictionary<Type, string>
            {
                { typeof(bool), "bool" },
                { typeof(char), "char" },
                { typeof(sbyte), "sbyte" },
                { typeof(byte), "byte" },
                { typeof(short), "short" },
                { typeof(ushort), "ushort" },
                { typeof(int), "int" },
                { typeof(uint), "uint" },
                { typeof(long), "long" },
                { typeof(ulong), "ulong" },
                { typeof(float), "float" },
                { typeof(double), "double" },
                { typeof(string), "string" },
            };

        private static readonly Type[] builtInSerializableTypes = builtInTypeNames.Keys
            .Concat
            (
                new[]
                {
                    typeof(Color),
                    typeof(Color32),
                    typeof(Vector2),
                    typeof(Vector3),
                    typeof(Vector4),
                    typeof(Quaternion),
                    typeof(Ray),
                    typeof(Ray2D),
                }
            )
            .ToArray();

        private static readonly Type[] supportedContainerDefinitions =
        {
            typeof(List<>),
        };

        private static readonly Dictionary<string, Type> declaredTypeCache = new();
        private static readonly Dictionary<Type, IReadOnlyList<ManagedReferenceCreationCandidate>> creationCandidateCache = new();
        private static readonly Dictionary<(Type GenericParameter, bool MakeArray), IReadOnlyList<Type>> genericArgumentCandidateCache = new();
        private static IReadOnlyList<Type> playerTypes = null;
        private static IReadOnlyList<Type> genericArgumentTypes = null;

    #endregion

    #region 메서드

        // ----------------------------------------------------------------------
        /// <summary>
        /// SerializedProperty가 보고하는 managed-reference 선언 타입을 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static Type GetDeclaredReferenceType(SerializedProperty property)
        {
            if (property == null || property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return null;
            }

            var serializedTypeName = property.managedReferenceFieldTypename;
            if (string.IsNullOrEmpty(serializedTypeName))
            {
                return null;
            }

            if (declaredTypeCache.TryGetValue(serializedTypeName, out var cachedType))
            {
                return cachedType;
            }

            // Unity 형식인 "AssemblyName Full.TypeName"을 현재 domain의 assembly에서 해석한다.
            var separatorIndex = serializedTypeName.IndexOf(' ');
            if (separatorIndex < 0)
            {
                cachedType = Type.GetType(serializedTypeName, throwOnError: false);
            }
            else
            {
                var assemblyName = serializedTypeName[..separatorIndex];
                var typeName     = serializedTypeName[(separatorIndex + 1)..];
                if (assemblyName == "Assembly")
                {
                    assemblyName = "Assembly-CSharp";
                }

                cachedType = Type.GetType($"{typeName}, {assemblyName}", throwOnError: false);
            }

            declaredTypeCache[serializedTypeName] = cachedType;
            return cachedType;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 선언 타입에 대입할 수 있는 생성 후보를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static IReadOnlyList<ManagedReferenceCreationCandidate> GetCreationCandidates(Type declaredType)
        {
            if (declaredType == null)
            {
                return Array.Empty<ManagedReferenceCreationCandidate>();
            }

            if (creationCandidateCache.TryGetValue(declaredType, out var cachedCandidates))
            {
                return cachedCandidates;
            }

            var candidates = GetPlayerTypes()
                .Select(type => TryCreateCandidate(type, declaredType, out var candidate) ? candidate : null)
                .Where(candidate => candidate != null)
                .OrderBy(candidate => candidate.DisplayName, StringComparer.Ordinal)
                .ToArray();
            creationCandidateCache[declaredType] = candidates;

            return candidates;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// generic parameter에 사용할 수 있는 Unity 직렬화 타입 후보를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static IReadOnlyList<Type> GetGenericArgumentCandidates
        (
            Type genericParameter,
            bool makeArray
        )
        {
            if (genericParameter == null || !genericParameter.IsGenericParameter)
            {
                return Array.Empty<Type>();
            }

            var cacheKey = (genericParameter, makeArray);
            if (genericArgumentCandidateCache.TryGetValue(cacheKey, out var cachedCandidates))
            {
                return cachedCandidates;
            }

            var candidates = GetGenericArgumentTypes()
                .Where(type => IsPotentialGenericArgument(genericParameter, type, makeArray))
                .OrderBy(GetDisplayName, StringComparer.Ordinal)
                .ToArray();
            genericArgumentCandidateCache[cacheKey] = candidates;

            return candidates;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 생성자로 새 managed-reference instance를 만들 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool CanCreate(Type type)
        {
            return type != null
                   && type.IsClass
                   && !type.IsAbstract
                   && !type.IsInterface
                   && !type.ContainsGenericParameters
                   && !typeof(UnityEngine.Object).IsAssignableFrom(type)
                   && type.IsSerializable
                   && type.GetConstructor(Type.EmptyTypes) != null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// picker와 inspector에 표시할 type 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static string GetDisplayName(Type type)
        {
            return GetDisplayName(type, null);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정된 generic parameter 치환을 반영한 type 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static string GetDisplayName
        (
            Type type,
            IReadOnlyDictionary<Type, Type> substitutions
        )
        {
            if (type == null)
            {
                return "None";
            }

            if (builtInTypeNames.TryGetValue(type, out var builtInName))
            {
                return builtInName;
            }

            if (type.IsGenericParameter)
            {
                return substitutions != null && substitutions.TryGetValue(type, out var substitution)
                    ? GetDisplayName(substitution, substitutions)
                    : type.Name;
            }

            if (type.IsArray)
            {
                return $"{GetDisplayName(type.GetElementType(), substitutions)}[]";
            }

            var typeName = GetTypeNameWithoutArity(type);
            if (!type.IsGenericType)
            {
                return typeName;
            }

            var arguments = string.Join(", ", type.GetGenericArguments().Select(argument => GetDisplayName(argument, substitutions)));
            return $"{typeName}<{arguments}>";
        }

        // ------------------------------------------------------------
        /// <summary>
        /// CLR generic parameter 제약을 최종 타입이 만족하는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsGenericArgumentCompatible(Type genericParameter, Type argument)
        {
            if (genericParameter == null
                || !genericParameter.IsGenericParameter
                || argument == null
                || argument.ContainsGenericParameters
                || argument.IsPointer
                || argument.IsByRef)
            {
                return false;
            }

            var attributes         = genericParameter.GenericParameterAttributes;
            var specialConstraints = attributes & GenericParameterAttributes.SpecialConstraintMask;

            if ((specialConstraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0
                && argument.IsValueType)
            {
                return false;
            }

            if ((specialConstraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0
                && (!argument.IsValueType || Nullable.GetUnderlyingType(argument) != null))
            {
                return false;
            }

            if ((specialConstraints & GenericParameterAttributes.DefaultConstructorConstraint) != 0
                && !argument.IsValueType
                && argument.GetConstructor(Type.EmptyTypes) == null)
            {
                return false;
            }

            // 다른 generic parameter를 포함한 관계형 제약은 모든 인자가 정해진 MakeGenericType에서 확정한다.
            return genericParameter.GetGenericParameterConstraints()
                .Where(constraint => !constraint.ContainsGenericParameters)
                .All(constraint => constraint.IsAssignableFrom(argument));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// type과 선언 타입의 계약을 해석해 실제 생성 후보를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool TryCreateCandidate
        (
            Type type,
            Type declaredType,
            out ManagedReferenceCreationCandidate candidate
        )
        {
            candidate = null;
            if (type == null || type.IsAbstract || type.IsInterface)
            {
                return false;
            }

            if (!type.IsGenericTypeDefinition)
            {
                if (!declaredType.IsAssignableFrom(type) || !CanCreate(type))
                {
                    return false;
                }

                candidate = new ManagedReferenceCreationCandidate(type);
                return true;
            }

            if (!IsOpenManagedReferenceType(type)
                || !TryGetGenericArgumentBindings(type, declaredType, out var fixedArguments))
            {
                return false;
            }

            var parameters = type.GetGenericArguments();
            if (parameters.All(fixedArguments.ContainsKey))
            {
                if (!TryCloseGenericType(type, parameters, fixedArguments, out var closedType)
                    || !declaredType.IsAssignableFrom(closedType)
                    || !CanCreate(closedType))
                {
                    return false;
                }

                candidate = new ManagedReferenceCreationCandidate(closedType);
                return true;
            }

            candidate = new ManagedReferenceCreationCandidate(type, fixedArguments);
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열린 generic class가 managed-reference instance로 닫힐 수 있는 형태인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsOpenManagedReferenceType(Type type)
        {
            return type.IsGenericTypeDefinition
                   && type.IsClass
                   && !type.IsAbstract
                   && !typeof(UnityEngine.Object).IsAssignableFrom(type)
                   && type.IsSerializable
                   && type.GetConstructor(Type.EmptyTypes) != null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// generic definition과 선언 타입의 계약을 비교해 확정된 argument를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool TryGetGenericArgumentBindings
        (
            Type genericDefinition,
            Type declaredType,
            out IReadOnlyDictionary<Type, Type> fixedArguments
        )
        {
            fixedArguments = null;
            if (genericDefinition == null
                || !genericDefinition.IsGenericTypeDefinition
                || declaredType == null)
            {
                return false;
            }

            foreach (var contract in GetTypeContracts(genericDefinition))
            {
                var arguments = new Dictionary<Type, Type>();
                if (TryMatchContract(contract, declaredType, arguments))
                {
                    fixedArguments = arguments;
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// generic definition을 확정된 argument로 닫고 CLR 제약을 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool TryCloseGenericType
        (
            Type genericDefinition,
            IReadOnlyList<Type> parameters,
            IReadOnlyDictionary<Type, Type> fixedArguments,
            out Type closedType
        )
        {
            closedType = null;

            try
            {
                var arguments = parameters.Select(parameter => fixedArguments[parameter]).ToArray();
                closedType = genericDefinition.MakeGenericType(arguments);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (TypeLoadException)
            {
                return false;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// candidate contract의 generic parameter를 선언 타입의 실제 argument에 대응시킨다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool TryMatchContract
        (
            Type contract,
            Type declaredType,
            IDictionary<Type, Type> fixedArguments
        )
        {
            if (contract == declaredType)
            {
                return true;
            }

            if (!declaredType.IsGenericType)
            {
                return declaredType.IsAssignableFrom(contract);
            }

            if (!contract.IsGenericType
                || contract.GetGenericTypeDefinition() != declaredType.GetGenericTypeDefinition())
            {
                return false;
            }

            var contractArguments = contract.GetGenericArguments();
            var declaredArguments = declaredType.GetGenericArguments();
            for (var i = 0; i < contractArguments.Length; i++)
            {
                if (!TryMatchTypeArgument(contractArguments[i], declaredArguments[i], fixedArguments))
                {
                    return false;
                }
            }

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// contract argument의 generic parameter와 실제 선언 argument를 재귀적으로 대응시킨다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool TryMatchTypeArgument
        (
            Type contractArgument,
            Type declaredArgument,
            IDictionary<Type, Type> fixedArguments
        )
        {
            if (contractArgument.IsGenericParameter)
            {
                if (fixedArguments.TryGetValue(contractArgument, out var fixedArgument))
                {
                    return fixedArgument == declaredArgument;
                }

                fixedArguments.Add(contractArgument, declaredArgument);
                return true;
            }

            if (contractArgument.IsArray)
            {
                return declaredArgument.IsArray
                       && contractArgument.GetArrayRank() == declaredArgument.GetArrayRank()
                       && TryMatchTypeArgument
                       (
                           contractArgument.GetElementType(),
                           declaredArgument.GetElementType(),
                           fixedArguments
                       );
            }

            if (!contractArgument.IsGenericType
                || !declaredArgument.IsGenericType
                || contractArgument.GetGenericTypeDefinition() != declaredArgument.GetGenericTypeDefinition())
            {
                return contractArgument == declaredArgument;
            }

            var contractArguments = contractArgument.GetGenericArguments();
            var declaredArguments = declaredArgument.GetGenericArguments();
            for (var i = 0; i < contractArguments.Length; i++)
            {
                if (!TryMatchTypeArgument(contractArguments[i], declaredArguments[i], fixedArguments))
                {
                    return false;
                }
            }

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 후보 타입이 선택 시점에 확인 가능한 generic parameter 제약을 만족하는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsPotentialGenericArgument
        (
            Type genericParameter,
            Type candidate,
            bool makeArray
        )
        {
            Type argument;
            try
            {
                argument = makeArray ? candidate.MakeArrayType() : candidate;
            }
            catch (TypeLoadException)
            {
                return false;
            }

            if (!argument.ContainsGenericParameters)
            {
                return IsGenericArgumentCompatible(genericParameter, argument);
            }

            var attributes         = genericParameter.GenericParameterAttributes;
            var specialConstraints = attributes & GenericParameterAttributes.SpecialConstraintMask;

            if ((specialConstraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0
                && argument.IsValueType)
            {
                return false;
            }

            if ((specialConstraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0
                && !argument.IsValueType)
            {
                return false;
            }

            if ((specialConstraints & GenericParameterAttributes.DefaultConstructorConstraint) != 0
                && !argument.IsValueType
                && argument.GetConstructor(Type.EmptyTypes) == null)
            {
                return false;
            }

            return genericParameter.GetGenericParameterConstraints()
                .Where(constraint => !constraint.ContainsGenericParameters)
                .All(constraint => CanPotentiallySatisfyConstraint(argument, constraint));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열린 후보 타입이 닫힌 뒤 명시 constraint에 대입될 가능성이 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool CanPotentiallySatisfyConstraint(Type candidate, Type constraint)
        {
            if (constraint.IsAssignableFrom(candidate))
            {
                return true;
            }

            foreach (var contract in GetTypeContracts(candidate))
            {
                if (contract == constraint)
                {
                    return true;
                }

                if (constraint.IsGenericType
                    && contract.IsGenericType
                    && constraint.GetGenericTypeDefinition() == contract.GetGenericTypeDefinition())
                {
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// generic argument Picker에 노출할 타입인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsGenericArgumentType(Type type)
        {
            if (type == null
                || type.IsAbstract
                || type.IsInterface
                || type.IsPointer
                || type.IsByRef
                || type.IsGenericParameter
                || typeof(Delegate).IsAssignableFrom(type))
            {
                return false;
            }

            if (type.IsGenericTypeDefinition)
            {
                return type.IsSerializable;
            }

            if (type.ContainsGenericParameters)
            {
                return false;
            }

            return type.IsEnum
                   || builtInSerializableTypes.Contains(type)
                   || typeof(UnityEngine.Object).IsAssignableFrom(type)
                   || type.IsSerializable;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// generic argument Picker의 캐시된 타입 목록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static IReadOnlyList<Type> GetGenericArgumentTypes()
        {
            if (genericArgumentTypes != null)
            {
                return genericArgumentTypes;
            }

            genericArgumentTypes = builtInSerializableTypes
                .Concat(supportedContainerDefinitions)
                .Concat(GetPlayerTypes())
                .Where(IsGenericArgumentType)
                .Distinct()
                .ToArray();

            return genericArgumentTypes;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unity Player assembly에 속한 로드 가능한 타입을 캐싱해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static IReadOnlyList<Type> GetPlayerTypes()
        {
            if (playerTypes != null)
            {
                return playerTypes;
            }

            var playerAssemblyNames = CompilationPipeline.GetAssemblies()
                .Where
                (
                    assembly => (assembly.flags & UnityEditor.Compilation.AssemblyFlags.EditorAssembly) == 0
                )
                .Select(assembly => assembly.name)
                .ToHashSet(StringComparer.Ordinal);

            // Unity의 domain reload 수명과 같은 TypeCache를 사용해 unloaded assembly 참조를 보관하지 않는다.
            playerTypes = TypeCache.GetTypesDerivedFrom<object>()
                .Where
                (
                    type => playerAssemblyNames.Contains(type.Assembly.GetName().Name)
                            || type.Assembly.GetName().Name.StartsWith(nameof(UnityEngine), StringComparison.Ordinal)
                )
                .Distinct()
                .ToArray();
            return playerTypes;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// type 자신과 구현 interface 및 base type을 순회한다.
        /// </summary>
        // ------------------------------------------------------------
        private static IEnumerable<Type> GetTypeContracts(Type type)
        {
            yield return type;

            foreach (var interfaceType in type.GetInterfaces())
            {
                yield return interfaceType;
            }

            for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                yield return baseType;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// namespace와 nested type 경로를 유지하면서 generic arity를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private static string GetTypeNameWithoutArity(Type type)
        {
            var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            var typeName   = (definition.FullName ?? definition.Name).Replace('+', '.');
            var arityIndex = typeName.IndexOf('`');

            while (arityIndex >= 0)
            {
                var endIndex = arityIndex + 1;
                while (endIndex < typeName.Length && char.IsDigit(typeName[endIndex]))
                {
                    endIndex++;
                }

                typeName   = typeName.Remove(arityIndex, endIndex - arityIndex);
                arityIndex = typeName.IndexOf('`');
            }

            return typeName;
        }

    #endregion
    }
}

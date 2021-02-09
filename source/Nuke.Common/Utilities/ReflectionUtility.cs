// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Common.Utilities
{
    public static partial class ReflectionUtility
    {
        public const BindingFlags All = Static | Instance | BindingFlags.FlattenHierarchy;

        public const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static bool IsNullableType(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null ||
                   type == typeof(string) ||
                   type.IsClass ||
                   type.IsArray;
        }

        public static Type GetNullableType(this Type type)
        {
            return type.IsNullableType() ? type : typeof(Nullable<>).MakeGenericType(type);
        }

        public static bool IsCollectionLike(this Type type)
        {
            return type != typeof(string) &&
                   new[] { type }.Concat(type.GetInterfaces())
                       .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        public static bool HasCustomAttribute<T>(this MemberInfo member)
            where T : Attribute
        {
            return member.GetCustomAttribute<T>() != null;
        }

        public static bool HasCustomAttribute<T>(this ParameterInfo parameter)
            where T : Attribute
        {
            return parameter.GetCustomAttribute<T>() != null;
        }

        public static Type GetMemberType(this MemberInfo member)
        {
            return member switch
            {
                FieldInfo fieldInfo => fieldInfo.FieldType,
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                MethodInfo methodInfo => methodInfo.ReturnType,
                _ => throw new NotSupportedException(member.ToString())
            };
        }

        public static Type GetScalarType(this Type type)
        {
            return Nullable.GetUnderlyingType(type) is { } underlyingType
                ? underlyingType
                : type.IsArray
                    ? type.GetElementType()
                    : type.IsCollectionLike()
                        ? type.GetGenericArguments().Single()
                        : type;
        }

        public static bool IsPublic(this MemberInfo member)
        {
            return member switch
            {
                Type type => type.IsPublic,
                ConstructorInfo constructor => constructor.IsPublic,
                FieldInfo field => field.IsPublic,
                MethodInfo method => method.IsPublic,
                PropertyInfo property => property.GetMethod.IsPublic,
                _ => throw new NotSupportedException(member.ToString())
            };
        }

        public static bool IsFamily(this MemberInfo member)
        {
            return member switch
            {
                ConstructorInfo constructor => constructor.IsFamily,
                FieldInfo field => field.IsFamily,
                MethodInfo method => method.IsFamily,
                PropertyInfo property => property.GetMethod.IsFamily,
                _ => throw new NotSupportedException(member.ToString())
            };
        }

        public static bool IsAssembly(this MemberInfo member)
        {
            return member switch
            {
                ConstructorInfo constructor => constructor.IsAssembly,
                FieldInfo field => field.IsAssembly,
                MethodInfo method => method.IsAssembly,
                PropertyInfo property => property.GetMethod.IsAssembly,
                _ => throw new NotSupportedException(member.ToString())
            };
        }

        public static bool IsStatic(this MemberInfo member)
        {
            return member switch
            {
                ConstructorInfo constructor => constructor.IsStatic,
                FieldInfo field => field.IsStatic,
                MethodInfo method => method.IsStatic,
                PropertyInfo property => property.GetMethod.IsStatic,
                _ => throw new NotSupportedException(member.ToString())
            };
        }

        public static bool IsExtensionParameter(this ParameterInfo parameter)
        {
            return parameter.Member is MethodInfo method &&
                   method.HasCustomAttribute<ExtensionAttribute>() &&
                   method.GetParameters().First() == parameter;
        }

        public static string GetPossibleExplicitName(this MemberInfo member)
        {
            return $"{member.DeclaringType.NotNull().FullName.NotNull().Replace("+", ".")}.{member.Name}";
        }

        public static MemberInfo GetImplementedOrInterfaceMember(this MemberInfo member, Type classType)
        {
            ControlFlow.Assert(classType.IsClass, "classType.IsClass");
            return member.DeclaringType != classType
                ? classType.GetMember(member.Name).SingleOrDefault() ??
                  classType.GetMember(member.GetPossibleExplicitName(), Instance).SingleOrDefault() ??
                  member
                : member;
        }
    }
}

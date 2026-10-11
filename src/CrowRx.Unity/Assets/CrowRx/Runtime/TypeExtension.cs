using System;
using System.Text;

namespace CrowRx
{
    public static class TypeExtension
    {
        public static bool InheritsFrom(this Type self, Type target, bool isIncludeSelf)
        {
            if (self is null || target is null)
            {
                return false;
            }

            return self == target ? isIncludeSelf : target.IsAssignableFrom(self);
        }

        public static bool InheritsFrom(this Type self, Type target) => self.InheritsFrom(target, true);

        public static string GetRealTypeName(this Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.HasElementType && GetArity(type.Name) == 0)
            {
                return type.Name;
            }

            StringBuilder builder = new();
            AppendType(builder, type, false);

            return builder.ToString();
        }

        /// <summary>네임스페이스와 선언 타입을 포함한 표시명입니다. 어셈블리를 구분하는 식별자는 AssemblyQualifiedName을 사용합니다.</summary>
        public static string GetRealTypeFullName(this Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.HasElementType && !type.IsGenericType)
            {
                return (type.FullName ?? type.Name).Replace('+', '.');
            }

            StringBuilder builder = new();
            AppendType(builder, type, true);

            return builder.ToString();
        }

        private static void AppendType(StringBuilder builder, Type type, bool fullName)
        {
            if (type.HasElementType)
            {
                AppendType(builder, type.GetElementType(), fullName);
                if (type.IsArray)
                {
                    builder.Append('[');

                    int rank = type.GetArrayRank();
                    if (rank == 1 && !type.IsSZArray)
                    {
                        builder.Append('*');
                    }
                    else
                    {
                        builder.Append(',', rank - 1);
                    }

                    builder.Append(']');
                }
                else
                {
                    builder.Append(type.IsByRef ? '&' : '*');
                }

                return;
            }

            Type[] arguments = type.IsGenericType ? type.GetGenericArguments() : Type.EmptyTypes;
            int argumentIndex = fullName ? 0 : arguments.Length - GetArity(type.Name);
            AppendNamedType(builder, type, fullName, arguments, ref argumentIndex);
        }

        private static void AppendNamedType(StringBuilder builder, Type type, bool fullName, Type[] arguments, ref int argumentIndex)
        {
            switch (fullName)
            {
                case true when type.DeclaringType != null && !type.IsGenericParameter:
                    AppendNamedType(builder, type.DeclaringType, true, arguments, ref argumentIndex);
                    builder.Append('.');
                    break;
                case true when !type.IsGenericParameter && !string.IsNullOrEmpty(type.Namespace):
                    builder.Append(type.Namespace).Append('.');
                    break;
            }

            string name = type.Name;
            int marker = name.IndexOf('`');
            builder.Append(name, 0, marker < 0 ? name.Length : marker);

            int arity = GetArity(name);
            if (arity == 0)
            {
                return;
            }

            builder.Append('<');

            for (int i = 0; i < arity; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendType(builder, arguments[argumentIndex++], fullName);
            }

            builder.Append('>');
        }

        private static int GetArity(string name)
        {
            int arity = 0;

            int marker = name.IndexOf('`');
            if (marker >= 0)
            {
                for (int i = marker + 1; i < name.Length; i++)
                {
                    arity = arity * 10 + name[i] - '0';
                }
            }

            return arity;
        }
    }
}
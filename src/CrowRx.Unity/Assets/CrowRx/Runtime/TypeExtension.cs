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

            if (isIncludeSelf && self == target)
            {
                return true;
            }

            return target.IsAssignableFrom(self);
        }

        public static bool InheritsFrom(this Type self, Type target) => self.InheritsFrom(target, true);

        public static string GetRealTypeName(this Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name.Replace("+", ".");
            }

            StringBuilder sb = new();

            sb.Append(type.Name[..type.Name.IndexOf('`')]);
            sb.Append('<');

            bool isAppendComma = false;

            foreach (Type arg in type.GetGenericArguments())
            {
                if (isAppendComma)
                {
                    sb.Append(',');
                }

                sb.Append(arg.GetRealTypeName());

                isAppendComma = true;
            }

            sb.Append('>');

            return sb.ToString().Replace("+", ".");
        }

        public static string GetRealTypeFullName(this Type type)
        {
            if (!type.IsGenericType)
            {
                return type.ToString().Replace("+", ".");
            }

            StringBuilder sb = new();

            string fullName = type.ToString();

            sb.Append(fullName[..fullName.IndexOf('`')]);
            sb.Append('<');

            bool isAppendComma = false;

            foreach (Type arg in type.GetGenericArguments())
            {
                if (isAppendComma)
                {
                    sb.Append(',');
                }

                sb.Append(arg.GetRealTypeFullName());

                isAppendComma = true;
            }

            sb.Append('>');

            return sb.ToString().Replace("+", ".");
        }
    }
}
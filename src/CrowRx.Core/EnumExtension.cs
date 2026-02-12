using System;
using System.Reflection;


namespace CrowRx
{
    public static class EnumExtension
    {
        private struct Shell<TEnum>
            where TEnum : Enum
        {
            // WARNING : field 선언 순서 바뀌면 안됨!
            public int IntValue;
            public TEnum Enum;
        }


        public static int ToInt<TEnum>(this TEnum self)
            where TEnum : Enum
        {
            Shell<TEnum> shell;
            shell.Enum = self;

            unsafe
            {
                int* pointerInt = &shell.IntValue;

                pointerInt += 1;

                return *pointerInt;
            }
        }

        public static bool EqualsEnum<TEnum1, TEnum2>(this TEnum1 enum1, TEnum2 enum2)
            where TEnum1 : Enum
            where TEnum2 : Enum
            => enum1.ToInt() == enum2.ToInt();

        public static T? GetAttributeOfType<T>(this Enum enumVal)
            where T : Attribute
        {
            MemberInfo[] enumMembers = enumVal.GetType().GetMember(enumVal.ToString());
            if (enumMembers.Length == 0)
            {
                return null;
            }
            
            object[] attributes = enumMembers[0].GetCustomAttributes(typeof(T), false);

            return attributes.Length > 0 ? (T)attributes[0] : null;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CrowRx
{
    public static class EnumAttributeExtension
    {
        /// <summary>
        /// enum 값을 boxing하지 않고 상수 선언 필드의 첫 attribute를 캐시에서 반환합니다.
        /// </summary>
        /// <typeparam name="TEnum">조회할 enum 형식입니다.</typeparam>
        /// <typeparam name="TAttribute">조회할 attribute 형식입니다. 파생 형식도 포함합니다.</typeparam>
        /// <param name="enumVal">attribute를 조회할 enum 값입니다.</param>
        /// <returns>조회된 첫 attribute이며, 대응하는 필드나 attribute가 없으면 null입니다.</returns>
        /// <remarks>
        /// enum 형식과 attribute 형식의 조합별 첫 사용 시 선언 필드를 준비하고, 값별 첫 조회 시 attribute를 생성합니다.
        /// 이후에는 캐시를 조회하며, 반환되는 attribute 인스턴스는 호출자 간 공유됩니다.
        /// 반환 객체를 수정하면 이후 조회에도 영향을 줍니다. null 결과도 캐시합니다.
        /// Flags 조합의 개별 상수에 붙은 attribute를 합치지 않습니다. 명명된 복합 상수는 그 필드만 조회합니다.
        /// 같은 숫자에 여러 이름이 있으면 ToString이 선택한 이름을 사용하므로 원래 선언 이름을 구분하지 못합니다.
        /// 여러 attribute가 일치하면 reflection 결과의 첫 항목을 반환하며 선택 순서는 보장하지 않습니다.
        /// 이름 없는 조합과 미정의 값은 캐시에 추가하지 않습니다.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TAttribute? GetAttributeOfType<TEnum, TAttribute>(this TEnum enumVal)
            where TEnum : unmanaged, Enum
            where TAttribute : Attribute
            => AttributeCache<TEnum, TAttribute>.Lookup.Get(EnumValueExtension.GetNumericValue(enumVal).Bits);

        private static class AttributeCache<TEnum, TAttribute>
            where TEnum : unmanaged, Enum
            where TAttribute : Attribute
        {
            public static readonly AttributeLookup<TAttribute> Lookup = new(typeof(TEnum));
        }

        private sealed class AttributeLookup<TAttribute>
            where TAttribute : Attribute
        {
            private readonly Dictionary<ulong, AttributeEntry<TAttribute>> byBits = new();

            public AttributeLookup(Type enumType)
            {
                foreach (Enum value in Enum.GetValues(enumType))
                {
                    // Boxing을 수반할 수 있는 변환과 reflection은 선언 필드를 준비할 때만 수행합니다.
                    IConvertible? convertible = value;
                    TypeCode code = convertible.GetTypeCode();
                    ulong bits =
                        code == TypeCode.SByte || code == TypeCode.Int16 || code == TypeCode.Int32 || code == TypeCode.Int64
                            ? unchecked((ulong)convertible.ToInt64(CultureInfo.InvariantCulture))
                            : convertible.ToUInt64(CultureInfo.InvariantCulture);

                    if (byBits.ContainsKey(bits))
                    {
                        continue;
                    }

                    FieldInfo? field = enumType.GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
                    if (field is null)
                    {
                        continue;
                    }

                    AttributeEntry<TAttribute> entry = new(field);
                    byBits.Add(bits, entry);
                }
            }

            public TAttribute? Get(ulong bits) => byBits.TryGetValue(bits, out AttributeEntry<TAttribute>? entry) ? entry.Get() : null;
        }

        private sealed class AttributeEntry<TAttribute>
            where TAttribute : Attribute
        {
            private readonly object gate = new();
            private readonly FieldInfo field;
            private bool initialized;
            private TAttribute? attribute;

            public AttributeEntry(FieldInfo field) => this.field = field;

            public TAttribute? Get()
            {
                lock (gate)
                {
                    if (!initialized)
                    {
                        object[] attributes = field.GetCustomAttributes(typeof(TAttribute), false);
                        attribute = attributes.Length > 0 ? (TAttribute)attributes[0] : null;
                        initialized = true;
                    }

                    return attribute;
                }
            }
        }
    }
}

using System;
using System.Runtime.CompilerServices;

namespace CrowRx
{
    public static class EnumValueExtension
    {
        /// <summary>
        /// enum의 실제 기저형 값을 boxing 없이 추출하고 성공 여부를 반환합니다.
        /// </summary>
        /// <typeparam name="TEnum">값을 추출할 enum 형식입니다.</typeparam>
        /// <typeparam name="TValue">enum의 실제 기저형과 정확히 일치해야 하는 결과 형식입니다.</typeparam>
        /// <param name="value">기저형 값을 추출할 enum 값입니다.</param>
        /// <param name="result">성공하면 기저형 값이며, 실패하면 기본값입니다.</param>
        /// <returns>결과 형식이 실제 기저형과 같으면 true, 다르면 false입니다.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryToValue<TEnum, TValue>(this TEnum value, out TValue result)
            where TEnum : unmanaged, Enum
            where TValue : unmanaged
        {
            if (!UnderlyingType<TEnum, TValue>.Matches)
            {
                result = default;
                return false;
            }

            result = *(TValue*)&value;
            return true;
        }

        /// <summary>
        /// enum의 실제 기저형 값을 boxing 없이 반환합니다.
        /// </summary>
        /// <typeparam name="TEnum">값을 추출할 enum 형식입니다.</typeparam>
        /// <typeparam name="TValue">enum의 실제 기저형과 정확히 일치해야 하는 결과 형식입니다.</typeparam>
        /// <param name="value">기저형 값을 추출할 enum 값입니다.</param>
        /// <returns>enum의 실제 기저형 값입니다.</returns>
        /// <exception cref="InvalidOperationException">결과 형식이 enum의 실제 기저형과 다릅니다.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue ToValue<TEnum, TValue>(this TEnum value)
            where TEnum : unmanaged, Enum
            where TValue : unmanaged
        {
            if (value.TryToValue(out TValue result))
            {
                return result;
            }

            throw new InvalidOperationException($"{typeof(TEnum).FullName}의 실제 기저형과 {typeof(TValue).FullName}이 일치하지 않습니다.");
        }

        /// <summary>
        /// enum 형식과 관계없이 부호와 전체 정수 범위를 보존해 숫자 값을 boxing 없이 비교합니다.
        /// </summary>
        /// <typeparam name="TEnum1">첫 번째 enum 형식입니다.</typeparam>
        /// <typeparam name="TEnum2">두 번째 enum 형식입니다.</typeparam>
        /// <param name="enum1">비교할 첫 번째 enum 값입니다.</param>
        /// <param name="enum2">비교할 두 번째 enum 값입니다.</param>
        /// <returns>두 값이 같은 정수를 나타내면 true입니다. 음수와 부호 없는 값은 같지 않습니다.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsValue<TEnum1, TEnum2>(this TEnum1 enum1, TEnum2 enum2)
            where TEnum1 : unmanaged, Enum
            where TEnum2 : unmanaged, Enum
        {
            NumericValue first = GetNumericValue(enum1);
            NumericValue second = GetNumericValue(enum2);

            return first.IsNegative == second.IsNegative && first.Bits == second.Bits;
        }

        internal static class UnderlyingType<TEnum, TValue>
            where TEnum : unmanaged, Enum
            where TValue : unmanaged
        {
            public static readonly bool Matches = Enum.GetUnderlyingType(typeof(TEnum)) == typeof(TValue);
        }

        internal static class EnumInfo<TEnum>
            where TEnum : unmanaged, Enum
        {
            public static readonly TypeCode Code = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(TEnum)));
        }

        internal readonly struct NumericValue
        {
            public readonly ulong Bits;
            public readonly bool IsNegative;

            public NumericValue(ulong bits, bool isNegative)
            {
                Bits = bits;
                IsNegative = isNegative;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static NumericValue FromSigned(long value) => new(unchecked((ulong)value), value < 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static NumericValue GetNumericValue<TEnum>(TEnum value) where TEnum : unmanaged, Enum =>
            EnumInfo<TEnum>.Code switch
            {
                TypeCode.SByte => FromSigned(value.ToValue<TEnum, sbyte>()),
                TypeCode.Int16 => FromSigned(value.ToValue<TEnum, short>()),
                TypeCode.Int32 => FromSigned(value.ToValue<TEnum, int>()),
                TypeCode.Int64 => FromSigned(value.ToValue<TEnum, long>()),
                TypeCode.Byte => new NumericValue(value.ToValue<TEnum, byte>(), false),
                TypeCode.UInt16 => new NumericValue(value.ToValue<TEnum, ushort>(), false),
                TypeCode.UInt32 => new NumericValue(value.ToValue<TEnum, uint>(), false),
                TypeCode.UInt64 => new NumericValue(value.ToValue<TEnum, ulong>(), false),
                _ => throw new InvalidOperationException("지원하지 않는 enum 기저형입니다."),
            };

        #region obsolete

        [Obsolete]
        private struct Shell<TEnum> where TEnum : Enum
        {
            // WARNING : field 선언 순서 바뀌면 안됨!
            public int IntValue;
            public TEnum Enum;
        }

        [Obsolete(nameof(ToValue) + "<TEnum, TValue> 또는 " + nameof(TryToValue) + "<TEnum, TValue>를 사용하세요. TValue는 enum의 실제 기저형과 일치해야 합니다.")]
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

        [Obsolete(nameof(EqualsValue) + "를 사용하세요. enum의 부호와 전체 정수 범위를 보존해 비교합니다.")]
        public static bool EqualsEnum<TEnum1, TEnum2>(this TEnum1 enum1, TEnum2 enum2)
            where TEnum1 : Enum
            where TEnum2 : Enum
            => enum1.ToInt() == enum2.ToInt();

        #endregion
    }
}

using System;
using System.ComponentModel;
using System.Globalization;

namespace CrowRx
{
    public static class ObjectExtension
    {
        /// <summary>값을 InvariantCulture로 변환하며 실패하면 기본값을 반환하고 진단 로그를 전달합니다.</summary>
        /// <remarks>성공 여부가 필요하면 TryConvertTo를 사용하세요. null은 진단 없이 기본값을 반환합니다.</remarks>
        public static T? ConvertTo<T>(this object? value)
        {
            if (value.TryConvertTo(out T? result))
            {
                return result;
            }

            if (value is not null)
            {
                Log.ReportError($"CrowRx.ObjectExtension.{nameof(ConvertTo)}<{typeof(T).FullName}>() 변환에 실패했습니다.");
            }

            return default;
        }

        /// <summary>문자열을 InvariantCulture로 변환하며 실패하면 기본값을 반환하고 진단 로그를 전달합니다.</summary>
        /// <remarks>성공 여부가 필요하면 TryParseTo를 사용하세요. null과 빈 문자열은 진단 없이 기본값을 반환합니다.</remarks>
        public static T? ParseTo<T>(this string? value)
        {
            if (value.TryParseTo(out T? result))
            {
                return result;
            }

            if (!string.IsNullOrEmpty(value))
            {
                Log.ReportError($"CrowRx.ObjectExtension.{nameof(ParseTo)}<{typeof(T).FullName}>() 변환에 실패했습니다.");
            }

            return default;
        }

        /// <summary>값을 InvariantCulture로 변환하고 성공 여부를 반환합니다. 일반적인 변환 실패는 로그를 남기지 않습니다.</summary>
        /// <typeparam name="T">결과 형식입니다. nullable과 enum을 지원합니다.</typeparam>
        /// <param name="value">변환할 값입니다. null은 실패로 처리합니다.</param>
        /// <param name="result">성공하면 변환 결과이며, 실패하면 기본값입니다.</param>
        /// <returns>변환에 성공하면 true입니다.</returns>
        /// <remarks>예상되는 형식·범위 오류만 실패로 처리하며 사용자 변환기의 예상 밖 예외는 전달합니다.</remarks>
        public static bool TryConvertTo<T>(this object? value, out T? result)
        {
            result = default;

            if (value is null)
            {
                return false;
            }

            if (value is T typed)
            {
                result = typed;
                return true;
            }

            if (value is string text)
            {
                return text.TryParseTo(out result);
            }

            try
            {
                object? converted;

                Type target = ConversionType<T>.EffectiveType;
                if (target.IsEnum)
                {
                    if (value is not IConvertible)
                    {
                        return false;
                    }

                    object number = Convert.ChangeType(value, Enum.GetUnderlyingType(target), CultureInfo.InvariantCulture);
                    converted = Enum.ToObject(target, number);
                }
                else if (ConversionType<T>.IsNullable && Type.GetTypeCode(target) == TypeCode.Object)
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                    if (!converter.CanConvertFrom(value.GetType()))
                    {
                        return false;
                    }

                    converted = converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
                }
                else
                {
                    if (value is not IConvertible || Type.GetTypeCode(target) == TypeCode.Object)
                    {
                        return false;
                    }

                    converted = Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
                }

                result = (T?)converted;

                return true;
            }
            catch (Exception exception) when (IsConversionFailure(exception))
            {
                return false;
            }
        }

        /// <summary>문자열을 InvariantCulture로 변환하고 성공 여부를 반환합니다. 일반적인 입력 실패는 로그를 남기지 않습니다.</summary>
        /// <typeparam name="T">결과 형식입니다. nullable과 enum을 지원합니다.</typeparam>
        /// <param name="value">변환할 문자열입니다.</param>
        /// <param name="result">성공하면 변환 결과이며, 실패하면 기본값입니다.</param>
        /// <returns>변환에 성공하면 true입니다. null은 실패이며 빈 문자열은 string 등 그대로 받을 수 있는 형식에만 성공합니다.</returns>
        /// <remarks>예상되는 형식·범위 오류만 실패로 처리하며 사용자 변환기의 예상 밖 예외는 전달합니다.</remarks>
        public static bool TryParseTo<T>(this string? value, out T? result)
        {
            result = default;

            if (value is T typed)
            {
                result = typed;
                return true;
            }

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            object? parsed;

            Type target = ConversionType<T>.EffectiveType;
            if (target.IsEnum)
            {
                if (!Enum.TryParse(target, value, true, out parsed))
                {
                    return false;
                }
            }
            else if (Type.GetTypeCode(target) != TypeCode.Object)
            {
                if (!TryParsePrimitive(value, Type.GetTypeCode(target), ConversionType<T>.IsNullable, out parsed))
                {
                    return false;
                }
            }
            else
            {
                if (!ConversionType<T>.IsNullable)
                {
                    return false;
                }

                try
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                    if (!converter.CanConvertFrom(typeof(string)))
                    {
                        return false;
                    }

                    result = (T?)converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);

                    return true;
                }
                catch (Exception exception) when (IsConversionFailure(exception))
                {
                    return false;
                }
            }

            result = (T?)parsed;

            return true;
        }

        private static class ConversionType<T>
        {
            public static readonly Type? UnderlyingType = Nullable.GetUnderlyingType(typeof(T));
            public static readonly Type EffectiveType = UnderlyingType ?? typeof(T);
            public static readonly bool IsNullable = UnderlyingType is not null;
        }

        private static bool IsConversionFailure(Exception exception)
            => exception is FormatException || exception is InvalidCastException || exception is OverflowException
                || exception is NotSupportedException
                || exception is ArgumentException && (exception.InnerException is null || IsConversionFailure(exception.InnerException));

        private static bool TryParsePrimitive(string value, TypeCode code, bool nullable, out object? result)
        {
            result = null;
            ReadOnlySpan<char> text = value.AsSpan();
            NumberStyles integerStyle = NumberStyles.Integer;
            if (nullable)
            {
                if (code != TypeCode.Char)
                {
                    text = text.Trim();
                }
                if (code >= TypeCode.SByte && code <= TypeCode.UInt64)
                {
                    if (text.Length > 0 && text[0] == '#')
                    {
                        text = text.Slice(1);
                        integerStyle = NumberStyles.AllowHexSpecifier;
                    }
                    else if (text.Length >= 2 && ((text[0] == '0' && (text[1] == 'x' || text[1] == 'X'))
                        || (text[0] == '&' && (text[1] == 'h' || text[1] == 'H'))))
                    {
                        text = text.Slice(2);
                        integerStyle = NumberStyles.AllowHexSpecifier;
                    }
                }
            }

            NumberStyles floatingStyle = nullable ? NumberStyles.Float : NumberStyles.Float | NumberStyles.AllowThousands;
            CultureInfo culture = CultureInfo.InvariantCulture;
            switch (code)
            {
                case TypeCode.SByte: if (sbyte.TryParse(text, integerStyle, culture, out sbyte i8)) { result = i8; return true; } break;
                case TypeCode.Byte: if (byte.TryParse(text, integerStyle, culture, out byte u8)) { result = u8; return true; } break;
                case TypeCode.Int16: if (short.TryParse(text, integerStyle, culture, out short i16)) { result = i16; return true; } break;
                case TypeCode.UInt16: if (ushort.TryParse(text, integerStyle, culture, out ushort u16)) { result = u16; return true; } break;
                case TypeCode.Int32: if (int.TryParse(text, integerStyle, culture, out int i32)) { result = i32; return true; } break;
                case TypeCode.UInt32: if (uint.TryParse(text, integerStyle, culture, out uint u32)) { result = u32; return true; } break;
                case TypeCode.Int64: if (long.TryParse(text, integerStyle, culture, out long i64)) { result = i64; return true; } break;
                case TypeCode.UInt64: if (ulong.TryParse(text, integerStyle, culture, out ulong u64)) { result = u64; return true; } break;
                case TypeCode.Single: if (float.TryParse(text, floatingStyle, culture, out float single)) { result = single; return true; } break;
                case TypeCode.Double: if (double.TryParse(text, floatingStyle, culture, out double number)) { result = number; return true; } break;
                case TypeCode.Decimal: if (decimal.TryParse(text, nullable ? NumberStyles.Float : NumberStyles.Number, culture, out decimal amount)) { result = amount; return true; } break;
                case TypeCode.Boolean: if (bool.TryParse(text, out bool boolean)) { result = boolean; return true; } break;
                case TypeCode.Char:
                    if (nullable && text.Length > 1) { text = text.Trim(); }
                    if (nullable && text.IsEmpty) { result = '\0'; return true; }
                    if (text.Length == 1) { result = text[0]; return true; }
                    break;
                case TypeCode.DateTime:
                    if (nullable && text.IsEmpty) { result = DateTime.MinValue; return true; }
                    if (DateTime.TryParse(text, culture, DateTimeStyles.None, out DateTime date)) { result = date; return true; }
                    break;
            }

            return false;
        }
    }
}

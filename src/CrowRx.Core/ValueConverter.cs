using System;
using System.Globalization;

namespace CrowRx
{
    internal interface IParsedValue
    {
        string? Source { get; }
        IParsedValue? Next { get; set; }

        void Set(string? source);
    }

    internal class ParsedValue<T> : IParsedValue
    {
        private T? _value;
        private bool _shouldConvert;
        private bool _succeeded;
        private bool _failureReported;

        public string? Source { get; private set; }
        public IParsedValue? Next { get; set; }

        public T? Value
        {
            get
            {
                if (!TryGetValue(out T? result) && !string.IsNullOrEmpty(Source) && !_failureReported)
                {
                    _failureReported = true;
                    Log.ReportError($"CrowRx.ValueConverter.{nameof(ValueConverter.GatValue)}<{typeof(T).FullName}>() 변환에 실패했습니다.");
                }

                return result;
            }
        }

        public bool TryGetValue(out T? result)
        {
            if (_shouldConvert)
            {
                _succeeded = Source.TryParseTo(out _value);
                _shouldConvert = false;
            }

            result = _value;
            return _succeeded;
        }

        public ParsedValue(string? source)
        {
            _shouldConvert = true;

            Source = source;
        }

        public void Set(string? source)
        {
            if (string.Equals(source, Source, StringComparison.Ordinal))
            {
                return;
            }

            Source = source;
            _value = default;
            _shouldConvert = true;
            _succeeded = false;
            _failureReported = false;
        }

        public override string ToString() => Source ?? string.Empty;
    }

    /// <summary>
    /// 문자열 배열을 보관하고 각 요소를 지정한 형식으로 변환한 결과를 캐시합니다.
    /// </summary>
    /// <remarks>
    /// 빈 슬롯과 null 문자열을 허용하며, 슬롯마다 요청한 형식별 결과와 성공 여부를 캐시합니다.
    /// 형식별 캐시는 해당 슬롯이 제거되거나 인스턴스가 해제될 때까지 유지됩니다.
    /// 인스턴스는 스레드 안전하지 않습니다. 여러 스레드에서 공유할 경우 호출자가 동기화해야 합니다.
    /// </remarks>
    public class ValueConverter
    {
        private string?[] _sources;
        private IParsedValue?[] _values;
        private int _valueCount;

        /// <summary>입력 배열을 복사해 변환기를 생성합니다. 각 요소는 null일 수 있습니다.</summary>
        /// <exception cref="ArgumentNullException">sources 배열이 null입니다.</exception>
        public ValueConverter(string?[] sources)
        {
            if (sources is null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            _valueCount = sources.Length;

            _sources = new string?[_valueCount];
            _values = new IParsedValue?[_valueCount];

            if (_valueCount > 0)
            {
                Array.Copy(sources, _sources, _valueCount);
            }
        }

        /// <summary>지정한 슬롯의 변환 결과를 반환합니다. 범위 밖 인덱스와 빈 슬롯은 기본값을 반환합니다.</summary>
        /// <remarks>변환 실패는 기본값을 반환하며, 비어 있지 않은 입력의 실패는 슬롯의 형식과 입력별로 한 번 진단합니다.</remarks>
        public T? GatValue<T>(int index)
        {
            if ((uint)index >= (uint)_valueCount)
            {
                return default;
            }

            return GetParsedValue<T>(index).Value;
        }

        /// <summary>지정한 슬롯의 변환 성공 여부와 결과를 반환합니다. 실패는 로그를 남기지 않습니다.</summary>
        /// <param name="index">읽을 슬롯의 인덱스입니다.</param>
        /// <param name="result">성공한 변환 결과입니다. 실패하면 기본값입니다.</param>
        /// <returns>인덱스가 유효하고 입력이 지정한 형식으로 변환되면 true입니다. 성공한 0과 변환 실패를 구분합니다.</returns>
        public bool TryGetValue<T>(int index, out T? result)
        {
            if ((uint)index >= (uint)_valueCount)
            {
                result = default;
                return false;
            }

            return GetParsedValue<T>(index).TryGetValue(out result);
        }

        /// <summary>값을 문자열로 저장하며 IFormattable에는 InvariantCulture를 적용합니다. null 값은 유효한 인덱스에서 아무 작업도 하지 않습니다.</summary>
        /// <exception cref="ArgumentOutOfRangeException">index가 음수이거나 int.MaxValue입니다.</exception>
        public void SetValue<T>(int index, T? value)
        {
            ValidateWriteIndex(index);

            if (value is null)
            {
                return;
            }

            SetValue(index, value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : value.ToString());
        }

        /// <summary>지정한 슬롯에 문자열을 저장합니다. 필요하면 확장하며 중간 슬롯과 null 문자열은 빈 값으로 취급합니다.</summary>
        /// <exception cref="ArgumentOutOfRangeException">index가 음수이거나 int.MaxValue입니다.</exception>
        public void SetValue(int index, string? value)
        {
            ValidateWriteIndex(index);

            int needArrayLength = index + 1;
            EnsureCapacity(needArrayLength);
            _sources[index] = value;
            SetCachedSources(index, value);
            _valueCount = Math.Max(_valueCount, needArrayLength);
        }

        /// <summary>입력 배열로 전체 값을 교체하며, 제거된 슬롯의 문자열과 변환 캐시 참조를 해제합니다.</summary>
        /// <exception cref="ArgumentNullException">sources 배열이 null입니다.</exception>
        public void SetValues(string?[] sources)
        {
            if (sources is null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            int newCount = sources.Length;
            EnsureCapacity(newCount);
            Array.Copy(sources, _sources, newCount);

            for (int i = 0; i < Math.Min(_valueCount, newCount); i++)
            {
                SetCachedSources(i, _sources[i]);
            }

            if (newCount < _valueCount)
            {
                Array.Clear(_sources, newCount, _valueCount - newCount);
                Array.Clear(_values, newCount, _valueCount - newCount);
            }

            _valueCount = newCount;
        }

        private ParsedValue<T> GetParsedValue<T>(int index)
        {
            for (IParsedValue? cached = _values[index]; cached is not null; cached = cached.Next)
            {
                if (cached is ParsedValue<T> parsedValue)
                {
                    return parsedValue;
                }
            }

            var created = new ParsedValue<T>(_sources[index]) { Next = _values[index] };
            _values[index] = created;
            return created;
        }

        private void SetCachedSources(int index, string? source)
        {
            for (IParsedValue? cached = _values[index]; cached is not null; cached = cached.Next)
            {
                cached.Set(source);
            }
        }

        private static void ValidateWriteIndex(int index)
        {
            if (index < 0 || index == int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "인덱스는 0 이상이며 int.MaxValue보다 작아야 합니다.");
            }
        }

        private void EnsureCapacity(int minimum)
        {
            if (_sources.Length >= minimum)
            {
                return;
            }

            int capacity = _sources.Length == 0 ? 4 : _sources.Length;
            while (capacity < minimum)
            {
                capacity = capacity > int.MaxValue / 2 ? minimum : capacity * 2;
            }

            var sources = new string?[capacity];
            var values = new IParsedValue?[capacity];
            Array.Copy(_sources, sources, _valueCount);
            Array.Copy(_values, values, _valueCount);
            _sources = sources;
            _values = values;
        }
    }
}

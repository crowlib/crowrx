using System;


namespace CrowRx
{
    internal interface IParsedValue
    {
        string Source { get; }

        void Set(string source);
    }


    internal class ParsedValue<T> : IParsedValue
    {
        private bool _shouldConvert;

        private T? _value;

        public string Source { get; private set; }

        public T? Value
        {
            get
            {
                if (!_shouldConvert)
                {
                    return _value;
                }

                _value = Source.ParseTo<T>();

                _shouldConvert = false;

                return _value;
            }
        }


        public ParsedValue(string source)
        {
            _shouldConvert = true;

            Source = source;
        }


        public void Set(string source)
        {
            if (!_shouldConvert)
            {
                _shouldConvert = !source.Equals(Source);
            }

            if (_shouldConvert)
            {
                Source = source;
            }
        }

        public override string ToString() => Source;
    }

    public class ValueConverter
    {
        private string[] _sources;
        private IParsedValue[] _values;
        private int _valueCount;


        public ValueConverter(string[] sources)
        {
            _valueCount = sources?.Length ?? 0;

            _sources = new string[_valueCount];
            _values = new IParsedValue[_valueCount];

            if (_valueCount > 0)
            {
                Array.Copy(sources, _sources, _valueCount);
            }
        }


        public T? GatValue<T>(int index)
        {
            if (_valueCount <= index)
            {
                return default;
            }

            if (_values[index] is ParsedValue<T> parsedValue)
            {
                parsedValue.Set(_sources[index]);
            }
            else
            {
                parsedValue = new ParsedValue<T>(_sources[index]);

                _values[index] = parsedValue;
            }

            return parsedValue.Value;
        }

        public void SetValue<T>(int index, T? value)
        {
            if (value is null)
            {
                return;
            }

            SetValue(index, value.ToString());
        }

        public void SetValue(int index, string value)
        {
            int needArrayLength = index + 1;
            if (_sources.Length < needArrayLength)
            {
                string[] oldSources = new string[needArrayLength];

                if (_valueCount > 0)
                {
                    Array.Copy(_sources, oldSources, Math.Min(_valueCount, _sources.Length));
                }

                oldSources[index] = value;

                SetValues(oldSources);
            }
            else
            {
                if (_valueCount < needArrayLength)
                {
                    _valueCount = needArrayLength;
                }

                if (_values.Length < _valueCount)
                {
                    _values = new IParsedValue[_valueCount];
                }

                _sources[index] = value;
            }
        }

        public void SetValues(string[] sources)
        {
            _valueCount = sources?.Length ?? 0;
            if (_sources.Length < _valueCount)
            {
                _sources = new string[_valueCount];
                _values = new IParsedValue[_valueCount];
            }

            Array.Copy(sources, _sources, _valueCount);
        }
    }
}
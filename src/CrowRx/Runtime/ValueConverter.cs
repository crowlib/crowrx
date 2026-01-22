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
        protected string _source;
        protected bool _shoudConvert;

        private T _value;


        public T Value
        {
            get
            {
                if (!_shoudConvert)
                {
                    return _value;
                }

                _value = _source.ParseTo<T>();

                _shoudConvert = false;

                return _value;
            }
        }

        public string Source => _source;


        public ParsedValue(string source)
        {
            _shoudConvert = true;

            _source = source;
        }


        public void Set(string source)
        {
            if (!_shoudConvert)
            {
                _shoudConvert = !source.Equals(_source);
            }

            if (_shoudConvert)
            {
                _source = source;
            }
        }

        public override string ToString() => _source;
    }

    public class ValueConverter
    {
        private string[] _sources;
        private IParsedValue[] _values;
        private int _valueCount;


        public ValueConverter()
        {
        }

        public ValueConverter(string[] sources)
        {
            SetValues(sources);
        }


        public T GatValue<T>(int index)
        {
            if (_sources is null || _valueCount <= index)
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

        public void SetValue<T>(int index, T value) => SetValue(index, value.ToString());

        public void SetValue(int index, string value)
        {
            int needArrayLength = index + 1;

            if (_sources is null)
            {
                _valueCount = needArrayLength;

                _sources = new string[_valueCount];
                _values = new IParsedValue[_valueCount];

                _sources[index] = value;
            }
            else
            {
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
        }

        public void SetValues(string[] sources)
        {
            if (sources is null || sources.Length == 0)
            {
                _valueCount = 0;

                return;
            }

            _valueCount = sources.Length;

            if (_sources is null || _sources.Length < _valueCount)
            {
                _sources = new string[_valueCount];
                _values = new IParsedValue[_valueCount];
            }

            Array.Copy(sources, _sources, _valueCount);
        }
    }
}
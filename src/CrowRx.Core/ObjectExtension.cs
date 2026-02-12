using System;
using System.ComponentModel;


namespace CrowRx
{
    public static class ObjectExtension
    {
        public static T? ConvertTo<T>(this object value)
        {
            switch (value)
            {
                case null:
                    return default;
                case T tValue:
                    return tValue;
                default:
                    try
                    {
                        //Handling Nullable types i.e, int?, double?, bool? .. etc
                        if (Nullable.GetUnderlyingType(typeof(T)) is not null)
                        {
                            return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(value);
                        }

                        return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch (Exception exception)
                    {
                        Log.Error($"CrowRx.ObjectExtension.ConvertTo()\nMessage - {exception.Message}\nStack - {exception.StackTrace}");

                        return default;
                    }
            }
        }

        public static T? ParseTo<T>(this string value)
        {
            if (value is T tValue)
            {
                return tValue;
            }

            if (string.IsNullOrEmpty(value))
            {
                return default;
            }

            try
            {
                //Handling Nullable types i.e, int?, double?, bool? .. etc
                if (Nullable.GetUnderlyingType(typeof(T)) is not null)
                {
                    return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(value);
                }

                if (typeof(T).IsEnum)
                {
                    return (T)Enum.Parse(typeof(T), value, true);
                }

                return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception exception)
            {
                Log.Error($"CrowRx.ObjectExtension.ConvertTo()\nMessage - {exception.Message}\nStack - {exception.StackTrace}");

                return default;
            }
        }
    }
}
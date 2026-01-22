using System;
#if USING_ZSTRING
using Cysharp.Text;

#else
using System.Text;
#endif


namespace CrowRx
{
    public static class StringExtension
    {
        /// <summary>
        /// 시작 문자열과 끝 문자열 사이의 문자열을 반환한다.
        /// </summary>
        /// <param name="start">시작 문자열</param>
        /// <param name="end">끝 문자열</param>
        /// <returns>시작과 끝 문자열 둘 중의 하나라도 포함되어 있지 않으면 빈 문자열을 반환한다.</returns>
        public static string SubstringBetween(this string self, string start, string end)
        {
            if (string.IsNullOrEmpty(self))
            {
                return string.Empty;
            }

            // 시작 위치
            int startIndex = self.IndexOf(start, StringComparison.Ordinal);

            if (startIndex <= -1)
            {
                return string.Empty;
            }

            startIndex += start.Length;

            // 끝 위치
            int endIndex = self.IndexOf(end, startIndex, StringComparison.Ordinal);

            return endIndex > -1 ? self[startIndex..endIndex] : string.Empty;
        }

        /// <summary>
        /// 첫번째 글자만 대문자로 변환한 문자열을 반환한다.
        /// </summary>
        public static string ToUpperFirst(this string self)
        {
            if (string.IsNullOrEmpty(self))
            {
                return string.Empty;
            }
#if USING_ZSTRING
            using Utf16ValueStringBuilder stringBuilder = ZString.CreateStringBuilder();
#else
            StringBuilder stringBuilder = new();
#endif
            stringBuilder.Append(char.ToUpperInvariant(self[0]));
            stringBuilder.Append(self[1..]);

            return stringBuilder.ToString();
        }
    }
}
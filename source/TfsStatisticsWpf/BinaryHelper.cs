using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TfsStatisticsWpf
{
    internal static class BinaryHelper
    {
        // http://msdn.microsoft.com/en-us/library/windows/desktop/dd317756%28v=vs.85%29.aspx
        private const uint Utf8Codepage = 65001;

        private readonly static HashSet<string> binaryExtensions = new HashSet<string>
            {
                ".pdf",
                ".dll",
                ".exe",
                ".sdf",
                ".doc",
                ".docx",
                ".png",
                ".jpeg",
                ".bmp",
                ".tiff",
                ".tif",
                ".xls",
                ".xlsx",
                ".dot",
                ".dotx",
                ".accdb",
                ".mdb",
                ".zip",
                ".xvf",
                ".ovf",
                ".eop",
                ".acb",
            };

        public static bool IsProbablyBinary(string fileName, Lazy<byte[]> content)
        {
            if (IsKnownBinaryExtension(Path.GetExtension(fileName)))
                return true;

            byte[] data = content.Value;
            Encoding encoding = GuessEncodingForBytes(data);

            return IsFileBinaryHeuristic(encoding.GetString(data))
                && IsFileBinaryHeuristic(Encoding.UTF8.GetString(data));
        }

        public static bool IsKnownBinaryExtension(string extension)
        {
            return binaryExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsFileBinaryHeuristic(string content)
        {
            var ranges = new[] 
            { 
                new { Start = 0x0, End = 0x1f }, 
                new { Start = 0x80, End = 0x9f }, 
                new { Start = 0x200e, End = 0x200f }, 
                new { Start = 0x202a, End = 0x202e }
            };
            
            int[] allowed = new int[] { 0x9, 0xa, 0xd, 0x85 };
            
            int indicators = 0;
            int neutral = 0;
            int length = ranges.Length;

            int sizeChecked = Math.Min(2048, content.Length);
            
            for (int i = 0; i < sizeChecked; i++)
            {
                bool found = false;

                for (int j = 0; j < length; j++)
                {
                    int item = content[i];
                    
                    if (item >= ranges[j].Start 
                        && item <= ranges[j].End)
                    {
                        found = !allowed.Contains(item);
                        break;
                    }
                }

                if (found)
                {
                    indicators++;
                }
                else
                {
                    neutral++;
                }
            }

            double result = (double)indicators / (double)neutral;
            
            return result > 0.005;
        }

        public static string GetXml(byte[] data)
        {
            Encoding encoding = GuessEncodingForBytes(data);

            string value = encoding.GetString(data);

            if (value.Length == 0 || value[0] == '<')
                return value;

            // Trim non visible characters.
            return new string(value.SkipWhile(c => c != '<').ToArray());
        }

        /// <remarks>Algorithm seen in GitHub Windows client.</remarks>
        public static Encoding GuessEncodingForBytes(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (buffer.Length < 8)
                return Encoding.UTF8;

            var source = new[]
            {
                new Definition { Pattern = new byte[] { 239, 187, 191 }, Encoding = Encoding.UTF8 }, 
                new Definition { Pattern = new byte[] { 254, 255 }, Encoding = Encoding.BigEndianUnicode }, 
                new Definition { Pattern = new byte[] { 255, 255, 0, 0 }, Encoding = Encoding.UTF32 }, 
                new Definition { Pattern = new byte[] { 255, 254 }, Encoding = Encoding.Unicode }, 
                new Definition { Pattern = new byte[] { 43, 47, 118, 56 }, Encoding = Encoding.UTF7 }, 
                new Definition { Pattern = new byte[] { 43, 47, 118, 57 }, Encoding = Encoding.UTF7 }, 
                new Definition { Pattern = new byte[] { 43, 47, 118, 43 }, Encoding = Encoding.UTF7 }, 
                new Definition { Pattern = new byte[] { 43, 47, 118, 47 }, Encoding = Encoding.UTF7 }
            };

            Definition type = source.FirstOrDefault(bom => bom.Pattern.Zip<byte, byte, bool>(buffer, (l, r) => (l == r)).All<bool>(x => x));

            if (type != null)
                return type.Encoding;

            var flags = NativeMethods.IsTextUnicodeFlags.IS_TEXT_UNICODE_NOT_UNICODE_MASK;

            if (NativeMethods.IsTextUnicode(buffer, buffer.Length, ref flags))
                return Encoding.GetEncoding(Thread.CurrentThread.CurrentCulture.TextInfo.ANSICodePage);

            int numberOfZeros = buffer.Count(b => b == 0);

            double ratioOfZeros = (double)numberOfZeros / (double)buffer.Length;

            if (ratioOfZeros > 0.6)
                return Encoding.UTF32;

            if (ratioOfZeros > 0.3)
            {
                if (buffer[0] != 0)
                    return Encoding.Unicode;

                return Encoding.BigEndianUnicode;
            }

            if (NativeMethods.MultiByteToWideChar(Utf8Codepage, NativeMethods.MbwcFlags.MBErrInvalidChars, buffer, buffer.Length, null, 0) == 0)
                return Encoding.Unicode;

            return Encoding.UTF8;
        }

        private static class NativeMethods
        {
            public const int CPAcp = 0;

            public const int CPMaccp = 2;

            public const int CPOemcp = 1;

            public const int CPSymbol = 42;

            public const int CPThreadAcp = 3;

            public const int CPUtf7 = 65000;

            public const int CPUtf8 = 65001;

            public const int ISTextUnicodeNotAsciiMask = 61440;

            public const int IsTextUnicodeNotUnicodeMask = 3840;

            public const int IsTextUnicodeReverseMask = 240;

            public const int IsTextUnicodeUnicodeMask = 15;

            [Flags]
            internal enum IsTextUnicodeFlags : int
            {
                IS_TEXT_UNICODE_ASCII16 = 0x0001,

                IS_TEXT_UNICODE_REVERSE_ASCII16 = 0x0010,

                IS_TEXT_UNICODE_STATISTICS = 0x0002,

                IS_TEXT_UNICODE_REVERSE_STATISTICS = 0x0020,

                IS_TEXT_UNICODE_CONTROLS = 0x0004,

                IS_TEXT_UNICODE_REVERSE_CONTROLS = 0x0040,

                IS_TEXT_UNICODE_SIGNATURE = 0x0008,

                IS_TEXT_UNICODE_REVERSE_SIGNATURE = 0x0080,

                IS_TEXT_UNICODE_ILLEGAL_CHARS = 0x0100,

                IS_TEXT_UNICODE_ODD_LENGTH = 0x0200,

                IS_TEXT_UNICODE_DBCS_LEADBYTE = 0x0400,

                IS_TEXT_UNICODE_NULL_BYTES = 0x1000,

                IS_TEXT_UNICODE_UNICODE_MASK = 0x000F,

                IS_TEXT_UNICODE_REVERSE_MASK = 0x00F0,

                IS_TEXT_UNICODE_NOT_UNICODE_MASK = 0x0F00,

                IS_TEXT_UNICODE_NOT_ASCII_MASK = 0xF000
            }

            [Flags]
            internal enum MbwcFlags
            {
                MBComposite = 2,

                MBErrInvalidChars = 8,

                MBPrecomposed = 1,

                MBUseglyphchars = 4
            }

            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("advapi32", SetLastError = true)]
            public static extern bool IsTextUnicode(
                [MarshalAs(UnmanagedType.LPArray)] byte[] buffer,
                int cb,
                ref IsTextUnicodeFlags flags);

            /// <summary>
            /// Maps a character string to a UTF-16 (wide character) string. The character string is not necessarily from a multi-byte character set.
            /// </summary>
            /// <param name="codePage"></param>
            /// <param name="dwFlags"></param>
            /// <param name="lpMultiByteStr"></param>
            /// <param name="cbMultiByte"></param>
            /// <param name="lpWideCharStr"></param>
            /// <param name="cchWideChar"></param>
            /// <returns></returns>
            [DllImport("kernel32", SetLastError = true)]
            public static extern int MultiByteToWideChar(
                uint codePage,
                MbwcFlags dwFlags,
                [MarshalAs(UnmanagedType.LPArray)] byte[] lpMultiByteStr,
                int cbMultiByte,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpWideCharStr,
                int cchWideChar);
        }

        private class Definition
        {
            public byte[] Pattern { get; set; }

            public Encoding Encoding { get; set; }
        }
    }
}

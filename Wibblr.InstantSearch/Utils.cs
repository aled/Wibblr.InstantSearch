using System;
using System.Collections.Generic;
using System.Text;

namespace Wibblr.InstantSearch
{

    public static class Utils
    {
        public static ushort CompressTrigram(byte[] ascii, int start)
        {
            int sum = 0;
            for (int i = 2; i >= 0; i--)
            {
                sum *= 36;

                var b = ascii[start + i];

                
            }

            return (ushort)sum;
        }

        public static string UnicodeToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public static byte[] NormalizeString(string s)
        {
            var b = new List<byte>();

            var normalized = Encoding.UTF32.GetBytes(s.Normalize(NormalizationForm.FormKD));

            //Console.WriteLine(UnicodeToHex(normalized));

            for (int i = 0; i < normalized.Length; i += 4)
                if (normalized[i+1] == 0 && normalized[i+2] == 0 && normalized[i+3] == 0)
                {
                    if (normalized[i] >= (byte)'0' && normalized[i] <= (byte)'9')
                        b.Add(normalized[i]);

                    else if (normalized[i] >= (byte)'a' && normalized[i] <= (byte)'z')
                        b.Add(normalized[i]);

                    else if (normalized[i] >= (byte)'A' && normalized[i] <= (byte)'Z')
                        b.Add((byte)(normalized[i] - (byte)'A' + (byte)'a'));

                }
                return b.ToArray();
        }

        public static void AddTrigrams(byte[] ascii, HashSet<Trigram> x)
        {
            DateTime startTime = DateTime.UtcNow;
            int start = 0;
            while (start + 3 <= ascii.Length)
            {
                //ushort ct = CompressTrigram(ascii, start);
                var ct = new Trigram(ascii[start], ascii[start + 1], ascii[start + 2]);
                x.Add(ct);
                start++;
            }
           // Console.WriteLine($"AddCompressedTrigrams: {DateTime.UtcNow.Subtract(startTime).TotalMilliseconds}ms");
        }
    }
}

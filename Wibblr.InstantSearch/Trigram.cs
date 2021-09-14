using System;

namespace Wibblr.InstantSearch
{
    /// <summary>
    /// A trigram is a set of 3 characters taken from a word.
    /// Only the characters 0-9 and a-z are included, so there is
    /// a choice of 36 characters at each position.
    /// 
    /// This means there are 36^3 = 46656 possible combinations. This is
    /// small enough to fit in a ushort, which takes 2 bytes of memory.
    /// </summary>
    public struct Trigram
    {
        public static readonly Trigram Invalid = new Trigram(ushort.MaxValue);

        // This must be between 0 and 46655.
        public ushort Ordinal { get; private set; }

        /// <summary>
        /// Convert an ASCII byte (0-9a-z) into a base-36 number
        /// </summary>
        private static int AsciiToBase36Digit(byte b)
        {
            if (b >= (byte)'0' && b <= (byte)'9')
                return b - '0';
            else if (b >= (byte)'a' && b <= (byte)'z')
                return b - 'a' + 10;
            else
                throw new Exception("Invalid char in base-36 string");
        }

        /// <summary>
        /// Convert a base-36 digit to an ascii char (0-9a-z)
        /// </summary>
        /// <param name="digit"></param>
        /// <returns></returns>
        private static char Base36DigitToAscii(int digit)
        {
            if (digit < 10)
                return (char)('0' + digit);
            else if (digit < 36)
                return (char)('a' + digit - 10);
            else
                throw new Exception("Invalid ordinal");
        }

        private Trigram(ushort ordinal)
        {
            if (ordinal > 46656 && ordinal != ushort.MaxValue)
                throw new Exception($"Invalid trigram ordinal {ordinal}");

            Ordinal = ordinal;
        }

        public Trigram(byte b0, byte b1, byte b2)
        {
            Ordinal = (ushort)(AsciiToBase36Digit(b0) * 36 * 36
                            + AsciiToBase36Digit(b1) * 36
                            + AsciiToBase36Digit(b2));
        }

        public override string ToString()
        {
            var char2 = Base36DigitToAscii(Ordinal % 36);
            var char1 = Base36DigitToAscii(Ordinal / 36 % 36);
            var char0 = Base36DigitToAscii(Ordinal / (36 * 36) % 36);

            return new string(new[] { char0, char1, char2 });
        }
    }
}

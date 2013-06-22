/// <summary>
/// Base32.
/// </summary> maybe more efficient implementation @ http://www.koders.com/csharp/fidA42CD3A8B38A142CBBF9B1B9BC1110428EE26A48.aspx?s=cdef%3Afile
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Collections;


namespace ThexCS
{
	/// <summary>
	/// This class converts byte arrays to base32 strings and vice versa.
	/// Base32 strings, unlike Base64 ones, are safe for filenames, even for
	/// case-insensitive file system like Windows. Base32 Encoding is described in 
	/// RFC 3548 - The Base16, Base32, and Base64 Data Encodings.
	/// </summary>
    public class Base32
    {
        private static Char[] Base32Chars = {
												'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 
												'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
												'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 
												'Y', 'Z', '2', '3', '4', '5', '6', '7'};

        private Base32() { }

        /// <summary>
        /// This method converts a byte array into a Base32-encoded string. The resulting
        /// string can be used safely for Windows file or directory names.
        /// </summary>
        /// <param name="inArray">The byte array to encode.</param>
        /// <returns>A base32-encoded string representation of the byte array.</returns>
        public static String ToBase32String(Byte[] inArray)
        {
            if (inArray == null) return null;
            int len = inArray.Length;
            // divide the input into 40-bit groups, so let's see, 
            // how many groups of 5 bytes can we get out of it?
            int numberOfGroups = len / 5;
            // and how many remaining bytes are there?
            int numberOfRemainingBytes = len - 5 * numberOfGroups;

            // after this, we're gonna split it into eight 5 bit
            // values. 
            StringBuilder sb = new StringBuilder();
            //int resultLen = 4*((len + 2)/3);
            //StringBuffer result = new StringBuffer(resultLen);

            // Translate all full groups from byte array elements to Base64
            int byteIndexer = 0;
            for (int i = 0; i < numberOfGroups; i++)
            {
                byte b0 = inArray[byteIndexer++];
                byte b1 = inArray[byteIndexer++];
                byte b2 = inArray[byteIndexer++];
                byte b3 = inArray[byteIndexer++];
                byte b4 = inArray[byteIndexer++];

                // first 5 bits from byte 0
                sb.Append(Base32Chars[b0 >> 3]);
                // the remaining 3, plus 2 from the next one
                sb.Append(Base32Chars[(b0 << 2) & 0x1F | (b1 >> 6)]);
                // get bit 3, 4, 5, 6, 7 from byte 1
                sb.Append(Base32Chars[(b1 >> 1) & 0x1F]);
                // then 1 bit from byte 1, and 4 from byte 2
                sb.Append(Base32Chars[(b1 << 4) & 0x1F | (b2 >> 4)]);
                // 4 bits from byte 2, 1 from byte3
                sb.Append(Base32Chars[(b2 << 1) & 0x1F | (b3 >> 7)]);
                // get bit 2, 3, 4, 5, 6 from byte 3
                sb.Append(Base32Chars[(b3 >> 2) & 0x1F]);
                // 2 last bits from byte 3, 3 from byte 4
                sb.Append(Base32Chars[(b3 << 3) & 0x1F | (b4 >> 5)]);
                // the last 5 bits
                sb.Append(Base32Chars[b4 & 0x1F]);
            }

            // Now, is there any remaining bytes?
            if (numberOfRemainingBytes > 0)
            {
                byte b0 = inArray[byteIndexer++];
                // as usual, get the first 5 bits
                sb.Append(Base32Chars[b0 >> 3]);
                // now let's see, depending on the 
                // number of remaining bytes, we do different
                // things
                switch (numberOfRemainingBytes)
                {
                    case 1:
                        // use the remaining 3 bits, padded with five 0 bits
                        sb.Append(Base32Chars[(b0 << 2) & 0x1F]);
                        //						sb.Append("======");
                        break;
                    case 2:
                        byte b1 = inArray[byteIndexer++];
                        sb.Append(Base32Chars[(b0 << 2) & 0x1F | (b1 >> 6)]);
                        sb.Append(Base32Chars[(b1 >> 1) & 0x1F]);
                        sb.Append(Base32Chars[(b1 << 4) & 0x1F]);
                        //						sb.Append("====");
                        break;
                    case 3:
                        b1 = inArray[byteIndexer++];
                        byte b2 = inArray[byteIndexer++];
                        sb.Append(Base32Chars[(b0 << 2) & 0x1F | (b1 >> 6)]);
                        sb.Append(Base32Chars[(b1 >> 1) & 0x1F]);
                        sb.Append(Base32Chars[(b1 << 4) & 0x1F | (b2 >> 4)]);
                        sb.Append(Base32Chars[(b2 << 1) & 0x1F]);
                        //						sb.Append("===");
                        break;
                    case 4:
                        b1 = inArray[byteIndexer++];
                        b2 = inArray[byteIndexer++];
                        byte b3 = inArray[byteIndexer++];
                        sb.Append(Base32Chars[(b0 << 2) & 0x1F | (b1 >> 6)]);
                        sb.Append(Base32Chars[(b1 >> 1) & 0x1F]);
                        sb.Append(Base32Chars[(b1 << 4) & 0x1F | (b2 >> 4)]);
                        sb.Append(Base32Chars[(b2 << 1) & 0x1F | (b3 >> 7)]);
                        sb.Append(Base32Chars[(b3 >> 2) & 0x1F]);
                        sb.Append(Base32Chars[(b3 << 3) & 0x1F]);
                        //						sb.Append("=");
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// This is a utility method. Given a string, this method computes the SHA1 hash of 
        /// the string, and then Base32-encode the hash and return it.
        /// </summary>
        /// <param name="str">The string to hash.</param>
        /// <returns>The Base32-encoded hash of the string.</returns>
        public static String GetBase32Hash(String str)
        {
            byte[] byteStr = Encoding.UTF8.GetBytes(str);
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] byteResult = sha.ComputeHash(byteStr);
            return ToBase32String(byteResult);
        }
    }
}

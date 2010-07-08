using System;  // Required for everything.
using System.IO; // for BinaryReader

namespace Yekyaa.FFXIEncoding
{
    /// <summary>
    /// Represents a UTF-16 Encoding of FFXI Characters.
    /// </summary>
    public class FFXIEncoding : System.Text.Encoding
    {
        #region FFXIEncoding Variables
        /// <summary>
        /// Character map for converting from UTF-16 to FFXI-Encoding.
        /// </summary>
        private BinaryReader encoding_br = new BinaryReader(new MemoryStream(FFXIEncodingResources.encoding));

        /// <summary>
        /// Character map for converting from FFXI-Encoding to UTF-16.
        /// </summary>
        private BinaryReader decoding_br = new BinaryReader(new MemoryStream(FFXIEncodingResources.decoding));

        //F     B           A       S       T       W       L       D       G       R
        //Fire  Blizzard    Aero    Stone   Thunder Water   Light   Dark    Green   Red
        private readonly char[] _0xEFmap = { 'F', 'B', 'A', 'S', 'T', 'W', 'L', 'D', 'G', 'R' };

        /// <summary>
        /// FFXI Encoding Marker to indicate start of an auto-translate phrase or special character.
        /// </summary>
        public static readonly char StartMarker = '\uFF1C';     // <

        /// <summary>
        /// FFXI Encoding Marker to indicate the middle of an auto-translate phrase.
        /// </summary>
        public static readonly char MiddleMarker = '\uFF5C';    // |

        /// <summary>
        /// FFXI Encoding Marker to indicate end of an auto-translate phrase or special character.
        /// </summary>
        public static readonly char EndMarker = '\uFF1E';       // >
        #endregion

        #region FFXIEncoding Properties
        public override bool IsSingleByte
        {
            get
            {
                return false;
            }
        }

        public override string BodyName
        {
            get
            {
                return String.Empty;
            }
        }

        public override int CodePage
        {
            get
            {
                return 0;
            }
        }

        public override string EncodingName
        {
            get { return "Yekyaa's FFXI Encoding"; }
        }
        #endregion

        #region FFXIEncoding Methods
        #region Other Byte/Character functions used heavily in this class.
        /// <summary>
        /// I've yet to figure out how I came up with this. Basically if byte given is 0x80 >= b >= 0x9F, returns true.
        /// </summary>
        /// <param name="b">Byte to evaluate.</param>
        /// <returns>true if 0x80 >= b >= 0x9F, false otherwise.</returns>
        private bool IsSurrogate(byte b)
        {
            return ((b >= 0x80) && (b <= 0x9F));
        }

        /// <summary>
        /// Converts char given to it's 1 or 2-byte equivalent.
        /// </summary>
        /// <param name="c">The character to convert to a byte(s).</param>
        /// <returns>Byte array of length 1 or 2 depending on if it's a surrogate character or not.</returns>
        private byte[] ConvertToByte(char c)
        {
            byte b1 = (byte)((c & 0xFF00) >> 8); // high byte
            byte b2 = (byte)(c & 0x00FF); // low byte
            byte[] ReturnValue = new byte[2];
            if ((b1 == 0xFF) && (b2 == 0xFF))
            {
                Array.Resize(ref ReturnValue, 1);
                ReturnValue[0] = (byte)'?';
            }
            else if (b1 == 0x00)
            {
                Array.Resize(ref ReturnValue, 1);
                ReturnValue[0] = b2;
            }
            else
            {
                ReturnValue[0] = b1;
                ReturnValue[1] = b2;
            }
            return ReturnValue;
        }
        #endregion

        #region ////// Conversion from UTF-16 to FFXI-Encoding and back. //////
        /// <summary>Convert FFXI Encoding (single-byte with surrogates) character to UTF16 (double-byte) character.</summary>
        /// <param name="convertChar">The character in FFXI Encoding format.</param>
        /// <returns>UTF-16 converted character.</returns>
        private char FFXIToUTF16(char convertChar)
        {
            Byte b1, b2;
            // Decoding file in program (Resource)
            // is a 2-byte per character lookup table
            // Lookup the address * 2, read 2 bytes,
            // That's the UTF-16 version.
            decoding_br.BaseStream.Position = (long)((UInt32)convertChar * 2);
            b1 = decoding_br.ReadByte();
            b2 = decoding_br.ReadByte();
            return ((char)((uint)((b1 << 8) + b2)));
        }

        /// <summary>Convert UTF16 (double-byte) char to FFXI Encoding (single-byte with surrogates) character.</summary>
        /// <param name="convertChar">The character in UTF16 format.</param>
        /// <returns>Converted character in FFXI Encoding.</returns>
        private char UTF16ToFFXI(char convertChar)
        {
            //UInt32 value = (UInt32)x;
            Byte b1 = 0x00, b2 = 0x00;
            encoding_br.BaseStream.Position = (long)((UInt32)((UInt32)convertChar * 2));
            b1 = encoding_br.ReadByte();
            b2 = encoding_br.ReadByte();
            return ((char)((uint)((b1 << 8) + b2))); // encoding_br.ReadChar();
        }
        #endregion

        #region Get????Count() overloads
        #region GetByteCount() overloads
        /// <summary>
        /// Calculates the number of bytes produced by encoding all the characters in the specified character array.
        /// </summary>
        /// <param name="chars">The character array containing the characters to encode.</param>
        /// <returns>The number of bytes produced by encoding all the characters in the specified character array.</returns>
        /// <exception cref="ArgumentNullException">Thrown if chars is null.</exception>
        public override int GetByteCount(char[] chars)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", "chars is null.");

            if (chars.Length <= 0)
                return 0;

            int ReturnValue = 0;

            try
            {
                ReturnValue = GetByteCount(chars, 0, chars.Length);
            }
            catch
            {
            }
            return ReturnValue;
        }

        /// <summary>
        /// Calculates the number of bytes produced by encoding the characters in the specified String.
        /// </summary>
        /// <param name="s">The String containing the set of characters to encode.</param>
        /// <returns>The number of bytes produced by encoding the specified characters.</returns>
        /// <exception cref="ArgumentNullException">Thrown if s is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if The resulting number of bytes is greater than the maximum number that can be returned as an integer.</exception>
        public override int GetByteCount(string s)
        {
            if (s == null)
                throw new ArgumentNullException("s", "s is null.");

            if (s == String.Empty)
                return 0;

            int ReturnValue = 0;

            try
            {
                ReturnValue = this.GetByteCount(s.ToCharArray());
            }
            catch (ArgumentOutOfRangeException err)
            {
                throw err;
            }
            catch (ArgumentException err)
            {
                throw err;
            }
            return ReturnValue;
        }

        /// <summary>
        /// Calculates the number of bytes produced by encoding a set of characters from the specified character array.
        /// </summary>
        /// <param name="chars">The character array containing the set of characters to encode.</param>
        /// <param name="index">The index of the first character to encode.</param>
        /// <param name="count">The number of characters to encode.</param>
        /// <returns>The number of bytes produced by encoding the specified characters.</returns>
        /// <exception cref="ArgumentNullException">Thrown when chars is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when index or count is less than zero or index and count do not denote a valid range in chars or The resulting number of bytes is greater than the maximum number that can be returned as an integer.</exception>
        /// <exception cref="ArgumentException">Error detection is enabled, and chars contains an invalid sequence of characters. </exception>
        public override int GetByteCount(char[] chars, int index, int count)
        {
            if (chars == null)
                throw new ArgumentNullException("chars");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            int maxCharCount = index + count;

            if (maxCharCount > chars.Length)
                throw new ArgumentOutOfRangeException("index and count", "index and count does not denote a valid range in chars.");

            long ReturnValue = 0;

            // Starting at charIndex (min 0), and going to charIndex + charCount (max chars.Length)
            for (; index < maxCharCount; index++)
            {
                // if it's null, skip it until end of line,
                // we only want the bytes, not the 0x00's
                // so skip it.
                if (chars[index] == '\u0000')
                    continue;
                // Assuming this char BEGINS the Auto-Translate Phrase.
                else if (chars[index] == StartMarker)
                {
                    // if it's <?> (3 bytes) 
                    // (ie <F> for fire element or <G> or <R> for ATPhrase arrows
                    if (((index + 2) < chars.Length) && ((chars[index + 2]) == EndMarker))
                    {
                        #region if it's 3 bytes <F> Fire, <G> Green ATPhrase arrow, etc
                        // Go to the F, G, R, etc to access the character
                        index++;

                        // locate said character in the MapIndex
                        int charMapIndex = Array.IndexOf(_0xEFmap, chars[index]);

                        // If unable to find in the 0xEF mapIndex
                        if (charMapIndex == -1)
                        {
                            // Copy it char for char <Z> would be <Z> in-game
                            byte[] start = ConvertToByte(UTF16ToFFXI(StartMarker));
                            byte[] mid = ConvertToByte(UTF16ToFFXI(chars[index]));
                            byte[] end = ConvertToByte(UTF16ToFFXI(EndMarker));

                            ReturnValue += start.Length;
                            ReturnValue += mid.Length;
                            ReturnValue += end.Length;
                        }
                        else  // else, convert it to the 0xEF byte for Elemental and Green/Red Arrows.
                        {
                            ReturnValue += 2; // 0xEF 0x1F etc
                        }
                        // Skip the EndMarker by going to it in the index
                        index++;
                        #endregion
                    }
                    // if it's an unknown UTF-16 character <####> (<0000> - <FFFF>)
                    else if (((index + 5) < chars.Length) && ((chars[index + 5]) == EndMarker))
                    {
                        #region If it's an unknown character (Undecodable)
                        ReturnValue += 2;
                        index += 5;
                        #endregion
                    }
                    // if it's <########> (10 bytes total) or <########|blahblah>
                    else if (((index + 9) < chars.Length) && (((chars[index + 9]) == EndMarker) || (chars[index + 9] == MiddleMarker)))
                    {
                        #region If it's an unknown byte group (Undecodable) or an AT Phrase
                        ReturnValue += 6;
                        index += 9;

                        // skip the rest, AT Phrases can be an unknown length
                        for (; ((chars[index] != EndMarker) && (index < maxCharCount)); index++) ;
                        #endregion
                    }
                    else  // if it's none of the above, just copy it, char for char.
                    {
                        ReturnValue += ConvertToByte(UTF16ToFFXI(chars[index])).Length;
                    }
                }
                else if ((chars[index] == '\u000a') || (chars[index] == '\u000d')) // shouldn't have \r\n's
                {
                    continue;
                }
                else
                {
                    ReturnValue += ConvertToByte(UTF16ToFFXI(chars[index])).Length;
                }
            }
            if (ReturnValue > Int32.MaxValue)
                throw new ArgumentOutOfRangeException("return value", "The resulting number of bytes is greater than the maximum number that can be returned as an integer.");
            return (int)(ReturnValue);
        }
        #endregion

        #region GetCharCount() overloads
        /// <summary>
        /// Calculates the number of characters produced by decoding a sequence of bytes.
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <returns>The number of characters produced by decoding the specified sequence of bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if bytes is null.</exception>
        public override int GetCharCount(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", "bytes is null.");

            if (bytes.Length <= 0)
                return 0;

            int ReturnValue = 0;

            try
            {
                ReturnValue = this.GetCharCount(bytes, 0, bytes.Length);
            }
            catch
            {
            }
            return ReturnValue;
        }

        /// <summary>
        /// Calculates the number of characters produced by decoding a sequence of bytes.
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <param name="index">The index of the first byte to decode.</param>
        /// <param name="count">The number of bytes to decode.</param>
        /// <returns>The number of characters produced by decoding the specified sequence of bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if bytes is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if index or count is less than zero or index and count do not denote a valid range in bytes.</exception>
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", "Parameter to FFXIEncoding.GetString cannot be null.");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", "index is less than zero.");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "count is less than zero.");

            int maxByteCount = index + count;

            if (maxByteCount > bytes.Length)
                throw new ArgumentOutOfRangeException("index and count", "index and count do not denote a valid range in bytes.");

            if (bytes.Length <= 0)
                return 0;

            int ReturnValue = 0;

            string s = String.Empty;
            char nextChar = '\uFFFF';

            #region "Loop Through bytes[] Array, Translating 0x85 codes or Shift-JIS codes"
            for ( ; index < maxByteCount; index++)
            {
                if (bytes[index] == 0x00) break;
                switch (bytes[index])
                {
                    case 0xEF:
                        #region If it's a Weather or such Special Character
                        index++; // Skip this byte and the next for now.
                        if (index >= maxByteCount)
                            break;
                        else if ((bytes[index] >= 0x1F) && (bytes[index] <= 0x28))
                            ReturnValue += 3;
                            //s += String.Format("{0}{1}{2}", StartMarker, _0xEFmap[(uint)(bytes[index] - 0x1F)], EndMarker);
                        #endregion
                        break;
                    case 0xFD:
                        #region If it's an Auto-Translate Phrase Marker
                        index++;  // Skip this char.
                        // Must have enough bytes to support Phrase otherwise skip the char
                        if (index >= maxByteCount)
                            break;
                        else if ((index + 4 + 1) >= maxByteCount)
                            break;
                        // Must continue the phrase, if not, skip the whole attempt... FFXI would ;;
                        else if (bytes[index + 4] != 0xFD)
                        {
                            // item_counter will be incremented the fifth time upon returning to the loop
                            index += 4;
                            break;
                        }
                        else
                        {
                            ReturnValue += 2;
                            for (int i = 0; i < 4; i++, index++)
                            //for ( ; bytes[index] != 0xFD; index++)
                            {
                                //s += String.Format("{0:X2}", (uint)bytes[index]);
                                ReturnValue += 2;
                            }
                            if (bytes[index] != 0xFD)
                                { /* ERROR ?!?!?!?! */ }
                        }
                        #endregion
                        break;
                    default:
                        #region If it's any regular character
                        if (this.IsSurrogate(bytes[index]))
                        {
                            nextChar = FFXIToUTF16((char)(((UInt32)(bytes[index] << 8)) + bytes[index + 1]));
                            if (nextChar == 0xFFFF)
                                ReturnValue += 6;//String.Format("{0}{1:X2}{2:X2}{3}", StartMarker,
                            //(UInt16)bytes[index], (UInt16)bytes[index + 1], EndMarker).Length;
                            else ReturnValue += 1; // String.Format("{0}", nextChar).Length;     // Else copy char
                            index++;
                        }
                        else
                        {
                            nextChar = FFXIToUTF16((char)(bytes[index]));
                            if (nextChar == 0xFFFF)
                                ReturnValue += 6; //String.Format("{0}00{1:X2}{2}", StartMarker,
                                    //(UInt16)bytes[index], EndMarker).Length;
                            else ReturnValue += 1; // String.Format("{0}", nextChar).Length;     // Else copy char
                        }
                        #endregion
                        break;
                }
            }
            #endregion

            return (ReturnValue);
        }
        #endregion
        #endregion

        #region GetMax????Count() overloads
        /// <summary>
        /// Calculates the maximum number of bytes produced by encoding the specified number of characters.
        /// </summary>
        /// <param name="charCount">The number of characters to encode.</param>
        /// <returns>The maximum number of bytes produced by encoding the specified number of characters.</returns>
        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException("charCount", "charCount is less than zero.");

            long ret = charCount * 6;
            if (ret > Int32.MaxValue)
                throw new ArgumentOutOfRangeException("return value", "The resulting number of bytes is greater than the maximum number that can be returned as an integer.");

            return (int)ret;
        }

        /// <summary>
        /// Calculates the maximum number of characters produced by decoding the specified number of bytes.
        /// </summary>
        /// <param name="byteCount">The number of bytes to decode.</param>
        /// <returns>The maximum number of characters produced by decoding the specified number of bytes.</returns>
        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("byteCount", "byteCount is less than zero.");
            long ret = byteCount * 6;
            if (ret > Int32.MaxValue)
                throw new ArgumentOutOfRangeException("return value", "The resulting number of bytes is greater than the maximum number that can be returned as an integer.");
            return (int)ret;
        }
        #endregion

        #region GetBytes() overloads
        /// <summary>
        /// Encodes all the characters in the specified character array into a sequence of bytes.
        /// </summary>
        /// <param name="chars">The character array containing the characters to encode. </param>
        /// <returns>A byte array containing the results of encoding the specified set of characters.</returns>
        /// <exception cref="ArgumentNullException">If chars is null, throws ArgumentNullException.</exception>
        public override byte[] GetBytes(char[] chars)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", "chars is null.");

            byte[] returnValue = new byte[0];

            if (chars.Length > 0)
            {
                try
                {
                    returnValue = GetBytes(chars, 0, chars.Length);
                }
                catch
                {
                    // ignore all.
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Encodes all the characters in the specified String into a sequence of bytes.
        /// </summary>
        /// <param name="s">The string containing the set of characters to encode.</param>
        /// <returns>A byte array containing the results of encoding the specified set of characters.</returns>
        /// <exception cref="ArgumentNullException">If s is null, throws ArgumentNullException.</exception>
        public override byte[] GetBytes(string s)
        {
            if (s == null)
                throw new ArgumentNullException("s", "s is null.");

            byte[] returnBytes = new byte[0];

            if (s != String.Empty)
            {
                try
                {
                    char[] charArray = s.ToCharArray();
                    returnBytes = this.GetBytes(charArray, 0, charArray.Length);
                }
                catch
                {
                    // ignore all others.
                }
            }
            return returnBytes;
        }

        /// <summary>
        /// Encodes a set of characters from the specified character array into a sequence of bytes.
        /// </summary>
        /// <param name="chars">The character array containing the set of characters to encode.</param>
        /// <param name="index">The index of the first character to encode.</param>
        /// <param name="count">The number of characters to encode.</param>
        /// <returns>A byte array containing the results of encoding the specified set of characters.</returns>
        /// <exception cref="ArgumentNullException">If chars is null, throws ArgumentNullException.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If index or count is less than zero, or if index and count does not denote a valid range in chars, throws ArgumentOutOfRangeException.</exception>
        public override byte[] GetBytes(char[] chars, int index, int count)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", "chars is null.");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", "index is less than zero.");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "count is less than zero.");
            if ((index + count) > chars.Length)
                throw new ArgumentOutOfRangeException("index + count", "index + count does not denote a valid range in chars.");

            byte[] bytes = new byte[GetByteCount(chars, index, count)];
            int newSize = -1;

            if (chars.Length > 0)
            {
                try
                {
                    newSize = GetBytes(chars, index, count, bytes, 0);
                }
                catch
                {

                }
                if (newSize < 0) // if GetBytes fail, send back a non-null bytes var
                    Array.Resize(ref bytes, 0);
                else if (newSize < bytes.Length) // else if < estimated Length, send back newSize
                    Array.Resize(ref bytes, newSize);
            }
            return bytes;
        }

        /// <summary>
        /// Encodes a set of characters from the specified character array into the specified byte array.
        /// </summary>
        /// <param name="chars">The character array containing the set of characters to encode.</param>
        /// <param name="charIndex">The index of the first character to encode.</param>
        /// <param name="charCount">The number of characters to encode.</param>
        /// <param name="bytes">The byte array to contain the resulting sequence of bytes.</param>
        /// <param name="byteIndex">The index at which to start writing the resulting sequence of bytes.</param>
        /// <returns>The actual number of bytes written into bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown when bytes or chars is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when charIndex or charCount do not denote a valid range in chars, or when byteIndex does not denote a valid range in bytes.</exception>
        /// <exception cref="ArgumentException">Thrown when bytes does not have enough capacity from byteIndex to the end of the array to accommodate the resulting bytes.</exception>
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if ((chars == null) || (bytes == null))
                throw new ArgumentNullException("bytes or chars");
            if ((charIndex < 0) || (byteIndex < 0) || (charCount < 0))
                throw new ArgumentOutOfRangeException("charIndex, byteIndex, or charCount", "value is less than zero.");
            if (byteIndex > (bytes.Length - 1))
                throw new ArgumentOutOfRangeException("byteIndex", "byteIndex is not a valid index in bytes.");

            if (charIndex > (chars.Length - 1)) // index can't be equal to the length
                throw new ArgumentOutOfRangeException("charIndex", "charIndex does not denote a valid range in chars.");
            if (charCount > chars.Length)
                throw new ArgumentOutOfRangeException("charCount", "charCount does not denote a valid range in chars.");

            int maxCharCount = charIndex + charCount;

            if (maxCharCount > chars.Length)
                throw new ArgumentOutOfRangeException("charIndex + charCount", "charIndex + charCount does not denote a valid range in chars.");

            int ReturnValue = 0;

            byte[] convertedBytes;

            // Starting at charIndex (min 0), and going to charIndex + charCount (max chars.Length)
            for (; charIndex < maxCharCount; charIndex++)
            {
                // if it's null, skip it until end of line,
                // we only want the bytes, not the 0x00's
                // so skip it.
                if (chars[charIndex] == '\u0000')
                    continue;
                // Assuming this char BEGINS the Auto-Translate Phrase.
                else if (chars[charIndex] == StartMarker)
                {
                    // if it's <?> (3 bytes) 
                    // (ie <F> for fire element or <G> or <R> for ATPhrase arrows
                    if (((charIndex + 2) < chars.Length) && ((chars[charIndex + 2]) == EndMarker))
                    {
                        #region if it's 3 bytes <F> Fire, <G> Green ATPhrase arrow, etc
                        // Go to the F, G, R, etc to access the character
                        charIndex++;

                        // locate said character in the MapIndex
                        int charMapIndex = Array.IndexOf(_0xEFmap, chars[charIndex]);

                        // If unable to find in the 0xEF mapIndex
                        if (charMapIndex == -1)
                        {
                            // Copy it char for char <Z> would be <Z> in-game
                            byte[] start = ConvertToByte(UTF16ToFFXI(StartMarker));
                            byte[] mid = ConvertToByte(UTF16ToFFXI(chars[charIndex]));
                            byte[] end = ConvertToByte(UTF16ToFFXI(EndMarker));

                            convertedBytes = new byte[start.Length + mid.Length + end.Length];

                            int convertedBytesIndex = 0;

                            // Copy the byte value for the StartMarker
                            convertedBytes[convertedBytesIndex++] = start[0];
                            if (start.Length > 1)
                                convertedBytes[convertedBytesIndex++] = start[1];
                            // Copy the byte value for the Middle character
                            convertedBytes[convertedBytesIndex++] = mid[0];
                            if (mid.Length > 1)
                                convertedBytes[convertedBytesIndex++] = mid[1];
                            // Copy the byte value for the EndMarker
                            convertedBytes[convertedBytesIndex++] = end[0];
                            if (end.Length > 1)
                                convertedBytes[convertedBytesIndex++] = end[1];
                            // If the total characters copied are less than the Length
                            // Resize the array. (We really should never get here)
                            if (convertedBytesIndex < convertedBytes.Length)
                                Array.Resize(ref convertedBytes, convertedBytesIndex);
                        }
                        else  // else, convert it to the 0xEF byte for Elemental and Green/Red Arrows.
                        {
                            convertedBytes = new byte[2];
                            convertedBytes[0] = 0xEF;
                            convertedBytes[1] = (byte)(0x1F + charMapIndex);
                        }
                        // Skip the EndMarker by going to it in the index
                        charIndex++;
                        #endregion
                    }
                    // if it's an unknown UTF-16 character <####> (<0000> - <FFFF>)
                    else if (((charIndex + 5) < chars.Length) && ((chars[charIndex + 5]) == EndMarker))
                    {
                        #region If it's an unknown character (Undecodable)
                        string s;
                        convertedBytes = new byte[2];
                        // it's stored as a hex code <0000> - <FFFF>
                        s = String.Format("0x{0}{1}", chars[charIndex + 1], chars[charIndex + 2]);
                        convertedBytes[0] = System.Convert.ToByte(s, 16);
                        s = String.Format("0x{0}{1}", chars[charIndex + 3], chars[charIndex + 4]);
                        convertedBytes[1] = System.Convert.ToByte(s, 16);
                        charIndex += 5;
                        #endregion
                    }
                    // if it's <########> (10 bytes total) or <########|blahblah>
                    else if (((charIndex + 9) < chars.Length) && (((chars[charIndex + 9]) == EndMarker) || (chars[charIndex + 9] == MiddleMarker)))
                    {
                        #region If it's an unknown byte group (Undecodable) or an AT Phrase
                        string s;
                        convertedBytes = new byte[6];

                        convertedBytes[0] = 0xFD;
                        s = String.Format("0x{0}{1}", chars[charIndex + 1], chars[charIndex + 2]);
                        convertedBytes[1] = System.Convert.ToByte(s, 16);
                        s = String.Format("0x{0}{1}", chars[charIndex + 3], chars[charIndex + 4]);
                        convertedBytes[2] = System.Convert.ToByte(s, 16);
                        s = String.Format("0x{0}{1}", chars[charIndex + 5], chars[charIndex + 6]);
                        convertedBytes[3] = System.Convert.ToByte(s, 16);
                        s = String.Format("0x{0}{1}", chars[charIndex + 7], chars[charIndex + 8]);
                        convertedBytes[4] = System.Convert.ToByte(s, 16);
                        convertedBytes[5] = 0xFD;
                        charIndex += 9;

                        // skip the rest, AT Phrases can be an unknown length
                        for (; ((chars[charIndex] != EndMarker) && (charIndex < maxCharCount)); charIndex++) ;
                        #endregion
                    }
                    else  // if it's none of the above, just copy it, char for char.
                    {
                        convertedBytes = ConvertToByte(UTF16ToFFXI(chars[charIndex]));
                    }
                }
                else if ((chars[charIndex] == '\u000a') || (chars[charIndex] == '\u000d')) // shouldn't have \r\n's
                {
                    continue;
                }
                else
                {
                    convertedBytes = ConvertToByte(UTF16ToFFXI(chars[charIndex]));
                }

                if ((byteIndex + convertedBytes.Length) > bytes.Length)
                    throw new ArgumentException("bytes", "bytes does not have enough capacity from byteIndex to the end of the array to accommodate the resulting bytes.");
                else // if ((byteIndex + convertedBytes.Length) < bytes.Length)
                {
                    foreach (byte b in convertedBytes)
                        bytes[byteIndex++] = b;
                    ReturnValue += convertedBytes.Length;
                }
            }
            return (ReturnValue);
        }

        /// <summary>
        /// Encodes a set of characters from the specified String into the specified byte array.
        /// </summary>
        /// <param name="s">The string containing the set of characters to encode.</param>
        /// <param name="charIndex">The index of the first character to encode.</param>
        /// <param name="charCount">The number of characters to encode.</param>
        /// <param name="bytes">The byte array to contain the resulting sequence of bytes.</param>
        /// <param name="byteIndex">The index at which to start writing the resulting sequence of bytes.</param>
        /// <returns>The actual number of bytes written into bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown when s is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when charIndex or charCount do not denote a valid range in chars, or when byteIndex does not denote a valid range in bytes.</exception>
        /// <exception cref="ArgumentException">Thrown when bytes does not have enough capacity from byteIndex to the end of the array to accommodate the resulting bytes.</exception>
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            int returnValue = 0;

            if (s == null)
                throw new ArgumentNullException("s", "s is null.");

            if (s != String.Empty)
            {
                try
                {
                    returnValue = GetBytes(s.ToCharArray(), charIndex, charCount, bytes, byteIndex);
                }
                catch
                {
                    throw;
                }
            }
            return returnValue;
        }
        #endregion

        #region GetChars() overloads
        /// <summary>
        /// Decodes a sequence of bytes into a set of characters.
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <param name="index">The index in bytes to starting decoding at.</param>
        /// <param name="count">The number of bytes to decode.</param>
        /// <returns>A character array containing the results of decoding the specified sequence of bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if bytes is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if index or count is less than zero or index and count do not denote a valid range in bytes.</exception>
        public override char[] GetChars(byte[] bytes, int index, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", "bytes is null.");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", "index is less than zero.");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "count is less than zero.");
            if ((index + count) > bytes.Length)
                throw new ArgumentOutOfRangeException("index and count", "index and count does not denote a valid range in bytes.");

            string s = String.Empty;

            if ((bytes[0] == 0x00) || (bytes.Length == 0))
                return s.ToCharArray();

            try
            {
                s = this.GetString(bytes, index, count);
            }
            catch
            {
            }

            return (s.ToCharArray());
        }

        /// <summary>
        /// Decodes a sequence of bytes into a set of characters.
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <returns>A character array containing the results of decoding the specified sequence of bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if bytes is null.</exception>
        public override char[] GetChars(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", "bytes is null.");

            string s = String.Empty;

            if ((bytes[0] == 0x00) || (bytes.Length == 0))
                return s.ToCharArray();

            try
            {
                s = this.GetString(bytes, 0, bytes.Length);
            }
            catch
            {
            }

            return (s.ToCharArray());
        }

        /// <summary>
        /// Decodes a sequence of bytes from the specified byte array into the specified character array.
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <param name="byteIndex">The index of the first byte to decode.</param>
        /// <param name="byteCount">The number of bytes to decode.</param>
        /// <param name="chars">The character array to contain the resulting set of characters.</param>
        /// <param name="charIndex">The index at which to start writing the resulting set of characters.</param>
        /// <returns>The actual number of characters written into chars.</returns>
        /// <exception cref="ArgumentNullException">Thrown if chars or bytes is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when byteIndex or byteCount or charIndex is less than zero, byteindex and byteCount do not denote a valid range in bytes, or charIndex is not a valid index in chars.</exception>
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", "chars is null.");
            if (bytes == null)
                throw new ArgumentNullException("bytes", "bytes is null.");
            if (byteIndex < 0)
                throw new ArgumentOutOfRangeException("byteIndex", "byteIndex is less than zero.");
            if (charIndex < 0)
                throw new ArgumentOutOfRangeException("charIndex", "charIndex is less than zero.");
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("byteCount", "byteCount is less than zero.");
            if (charIndex >= chars.Length)
                throw new ArgumentOutOfRangeException("charIndex", "charIndex is not a valid index in chars.");

            int maxByteCount = byteIndex + byteCount;

            if (maxByteCount > bytes.Length)
                throw new ArgumentOutOfRangeException("byteIndex and byteCount", "byteindex and byteCount do not denote a valid range in bytes.");

            string s = String.Empty;

            try
            {
                s = this.GetString(bytes, byteIndex, byteCount);
            }
            catch
            {
            }

            char[] charArray = s.ToCharArray();
            if ((charIndex + charArray.Length) > chars.Length)
                throw new ArgumentException("chars", "chars does not have enough capacity from charIndex to the end of the array to accommodate the resulting characters.");

            int ReturnValue = 0;

            foreach (char c in charArray)
            {
                chars[ReturnValue + charIndex] = c;
                ReturnValue++;
            }
            return ReturnValue;
        }
        #endregion

        #region GetString() overloads
        /// <summary>
        /// Decodes a sequence of bytes into a string.
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <param name="index">The index of the first byte to decode.</param>
        /// <param name="count">The number of bytes to decode.</param>
        /// <returns>A String containing the results of decoding the specified sequence of bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if bytes is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if index or count is less than zero or index and count do not denote a valid range in bytes.</exception>
        public override string GetString(byte[] bytes, int index, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", "Parameter to FFXIEncoding.GetString cannot be null.");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", "index is less than zero.");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "count is less than zero.");

            int maxByteCount = index + count;

            if (maxByteCount > bytes.Length)
                throw new ArgumentOutOfRangeException("index and count", "index and count do not denote a valid range in bytes.");

            string s = String.Empty;

            if (bytes.Length <= 0)
                return s;

            char nextChar = '\uFFFF';

            #region "Loop Through bytes[] Array, Translating 0x85 codes or Shift-JIS codes"
            for (; index < maxByteCount; index++)
            {
                if (bytes[index] == 0x00) break;
                switch (bytes[index])
                {
                    case 0xEF:
                        #region If it's a Weather or such Special Character
                        index++; // Skip this byte and the next for now.
                        if (index >= maxByteCount)
                            break;
                        else if ((bytes[index] >= 0x1F) && (bytes[index] <= 0x28))
                            s += String.Format("{0}{1}{2}", StartMarker, _0xEFmap[(uint)(bytes[index] - 0x1F)], EndMarker);
                        #endregion
                        break;
                    case 0xFD:
                        #region If it's an Auto-Translate Phrase Marker
                        index++;  // Skip this char.
                        // Must have enough bytes to support Phrase otherwise skip the char
                        if (index >= maxByteCount)
                            break;
                        else if ((index + 4 + 1) >= maxByteCount)
                            break;
                        // Must continue the phrase, if not, skip the whole attempt... FFXI would ;;
                        else if (bytes[index + 4] != 0xFD)
                        {
                            // item_counter will be incremented the fifth time upon returning to the loop
                            index += 4;
                            break;
                        }
                        else
                        {
                            //throw InvalidAutoTranslatePhraseException("Auto-Translate Phrase Not Found, Skipping.");
                            s += StartMarker;
                            /*
                            byte language_byte = bytes[index + 1];
                            byte resource_byte = bytes[index];
                            if (bytes[index] == 0x04)
                                bytes[index] = 0x02;
                            FFXIATPhrase atp = this.GetPhraseByID(bytes[index], (byte)this.LanguagePreference, bytes[index + 2], bytes[index + 3]);
                            */
                            for (int i = 0; i < 4; i++, index++)
                            //for ( ; bytes[index] != 0xFD; index++)
                            {
                                //s += String.Format("{0:X2}", (uint)bytes[index]);
                                s += String.Format("{0:X2}", (uint)bytes[index]);
                            }
                            if (bytes[index] != 0xFD)
                            { /* ERROR ?!?!?!?! */ }

                            /*
                            s += MidMarker;
                            if (atp == null) // not found
                            {
                                s += "UNKNOWN"; // Add autotranslate support here.
                            }
                            else
                            {
                                //if ((language_byte == 0x01) && ((atp.shortvalue != String.Empty) &&
                                //    (atp.shortvalue != null)))
                                //    s += atp.shortvalue.Trim('\0');
                                //else
                                s += atp.value.Trim('\0');
                            }*/
                            s += EndMarker;
                        }
                        #endregion
                        break;
                    default:
                        #region If it's any regular character
                        if (this.IsSurrogate(bytes[index]))
                        {
                            nextChar = FFXIToUTF16((char)(((UInt32)(bytes[index] << 8)) + bytes[index + 1]));
                            if (nextChar == 0xFFFF)
                                s += String.Format("{0}{1:X2}{2:X2}{3}", StartMarker,
                                 (UInt16)bytes[index], (UInt16)bytes[index + 1], EndMarker);           // Setup for conversion BACK later
                            else s += nextChar;     // Else copy char
                            // Add 1 to i to skip the second character
                            index++;
                        }
                        else
                        {
                            nextChar = FFXIToUTF16((char)(bytes[index]));
                            if (nextChar == 0xFFFF)
                                s += String.Format("{0}00{1:X2}{2}", StartMarker,
                                    (UInt16)bytes[index], EndMarker);
                            else s += nextChar;     // Else copy char
                        }
                        #endregion
                        break;
                }
            }

            #endregion

            return (s);
        }
        
        /// <summary>
        /// Decodes a sequence of bytes into a string. 
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <returns>A String containing the results of decoding the specified sequence of bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if bytes is null.</exception>
        public override string GetString(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", "Parameter to FFXIEncoding.GetString cannot be null.");

            string s = String.Empty;

            if (bytes.Length <= 0)
                return s;

            try
            {
                s = GetString(bytes, 0, bytes.Length);
            }
            catch
            {
            }

            return (s);
        }
        
        #endregion
        #endregion

        #region FFXIEncoding Constructor
        /// <summary>
        /// Initializes a new instance of the Yekyaa.FFXIEncoding.FFXIEncoding class.
        /// </summary>
        public FFXIEncoding()
        {
        }
        #endregion
    }
}

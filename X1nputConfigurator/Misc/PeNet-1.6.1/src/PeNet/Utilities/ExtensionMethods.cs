using System;
using System.Collections.Generic;
using System.Linq;
using PeNet.Structures;

namespace PeNet.Utilities
{
    /// <summary>
    /// Extensions method to work make the work with buffers 
    /// and addresses easier.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        ///     Convert to bytes to an 16 bit unsigned integer.
        /// </summary>
        /// <param name="b1">High byte.</param>
        /// <param name="b2">Low byte.</param>
        /// <returns>UInt16 of the input bytes.</returns>
        private static ushort BytesToUInt16(byte b1, byte b2)
        {
            return BitConverter.ToUInt16(new[] {b1, b2}, 0);
        }

        /// <summary>
        ///     Convert a two bytes in a byte array to an 16 bit unsigned integer.
        /// </summary>
        /// <param name="buff">Byte buffer.</param>
        /// <param name="offset">Position of the high byte. Low byte is i+1.</param>
        /// <returns>UInt16 of the bytes in the buffer at position i and i+1.</returns>
        public static ushort BytesToUInt16(this byte[] buff, ulong offset)
        {
            return BytesToUInt16(buff[offset], buff[offset + 1]);
        }

        /// <summary>
        ///     Convert 4 bytes to an 32 bit unsigned integer.
        /// </summary>
        /// <param name="b1">Highest byte.</param>
        /// <param name="b2">Second highest byte.</param>
        /// <param name="b3">Second lowest byte.</param>
        /// <param name="b4">Lowest byte.</param>
        /// <returns>UInt32 representation of the input bytes.</returns>
        private static uint BytesToUInt32(byte b1, byte b2, byte b3, byte b4)
        {
            return BitConverter.ToUInt32(new[] {b1, b2, b3, b4}, 0);
        }

        /// <summary>
        ///     Convert 4 consecutive bytes out of a buffer to an 32 bit unsigned integer.
        /// </summary>
        /// <param name="buff">Byte buffer.</param>
        /// <param name="offset">Offset of the highest byte.</param>
        /// <returns>UInt32 of 4 bytes.</returns>
        public static uint BytesToUInt32(this byte[] buff, uint offset)
        {
            return BytesToUInt32(buff[offset], buff[offset + 1], buff[offset + 2], buff[offset + 3]);
        }

        /// <summary>
        ///     Convert an UIn16 to an byte array.
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Two byte array of the input value.</returns>
        private static byte[] UInt16ToBytes(ushort value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        ///     Set an UInt16 value at an offset in an byte array.
        /// </summary>
        /// <param name="buff">Buffer in which the value is set.</param>
        /// <param name="offset">Offset where the value is set.</param>
        /// <param name="value">The value to set.</param>
        public static void SetUInt16(this byte[] buff, ulong offset, ushort value)
        {
            var x = UInt16ToBytes(value);
            buff[offset] = x[0];
            buff[offset + 1] = x[1];
        }

        /// <summary>
        ///     Convert an UInt32 value into an byte array.
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>4 byte array of the value.</returns>
        private static byte[] UInt32ToBytes(uint value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        ///     Sets an UInt32 value at an offset in a buffer.
        /// </summary>
        /// <param name="buff">Buffer to set the value in.</param>
        /// <param name="offset">Offset in the array for the value.</param>
        /// <param name="value">Value to set.</param>
        public static void SetUInt32(this byte[] buff, uint offset, uint value)
        {
            var x = UInt32ToBytes(value);
            buff[offset] = x[0];
            buff[offset + 1] = x[1];
            buff[offset + 2] = x[2];
            buff[offset + 3] = x[3];
        }

        /// <summary>
        ///     Map an relative virtual address to the raw file address.
        /// </summary>
        /// <param name="relativeVirtualAddress">Relative Virtual Address</param>
        /// <param name="sh">Section Headers</param>
        /// <returns>Raw file address.</returns>
        public static ulong RVAtoFileMapping(this ulong relativeVirtualAddress, ICollection<IMAGE_SECTION_HEADER> sh)
        {
            IMAGE_SECTION_HEADER GetSectionForRva(ulong rva)
            {
                var sectionsByRva = sh.OrderBy(s => s.VirtualAddress).ToList();
                var notLastSection = sectionsByRva.FirstOrDefault(s =>
                    rva >= s.VirtualAddress && rva < s.VirtualAddress + s.VirtualSize);

                if (notLastSection != null)
                    return notLastSection;

                var lastSection = sectionsByRva.LastOrDefault(s => 
                        rva >= s.VirtualAddress && rva <= s.VirtualAddress + s.VirtualSize);

                return lastSection;
            }

            var section = GetSectionForRva(relativeVirtualAddress);

            if (section is null)
            {
                throw new Exception("Cannot find corresponding section.");
            }

            return relativeVirtualAddress - section.VirtualAddress + section.PointerToRawData;
        }

        /// <summary>
        ///     Map an relative virtual address to the raw file address.
        /// </summary>
        /// <param name="relativeVirtualAddress">Relative Virtual Address</param>
        /// <param name="sh">Section Headers</param>
        /// <returns>Raw file address.</returns>
        public static uint RVAtoFileMapping(this uint relativeVirtualAddress, ICollection<IMAGE_SECTION_HEADER> sh)
        {
            return (uint) RVAtoFileMapping((ulong) relativeVirtualAddress, sh);
        }

        /// <summary>
        ///     Map an relative virtual address to the raw file address.
        /// </summary>
        /// <param name="RelativeVirtualAddress">Relative Virtual Address</param>
        /// <param name="sh">Section Headers</param>
        /// <returns>Raw address of null if error occurred.</returns>
        public static uint? SafeRVAtoFileMapping(this uint RelativeVirtualAddress, ICollection<IMAGE_SECTION_HEADER> sh)
        {
            try
            {
                return RelativeVirtualAddress.RVAtoFileMapping(sh);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static ushort GetOrdinal(this byte[] buff, uint ordinal)
        {
            return BitConverter.ToUInt16(new[] {buff[ordinal], buff[ordinal + 1]}, 0);
        }

        /// <summary>
        ///     Get a name (C string) at a specific position in a buffer.
        /// </summary>
        /// <param name="buff">Containing buffer.</param>
        /// <param name="stringOffset">Offset of the string.</param>
        /// <returns>The parsed C string.</returns>
        public static string GetCString(this byte[] buff, ulong stringOffset)
        {
            var length = GetCStringLength(buff, stringOffset);
            var tmp = new char[length];
            for (ulong i = 0; i < length; i++)
            {
                tmp[i] = (char) buff[stringOffset + i];
            }

            return new string(tmp);
        }

        /// <summary>
        ///     For a given offset in an byte array, find the next
        ///     null value which terminates a C string.
        /// </summary>
        /// <param name="buff">Buffer which contains the string.</param>
        /// <param name="stringOffset">Offset of the string.</param>
        /// <returns>Length of the string in bytes.</returns>
        public static ulong GetCStringLength(this byte[] buff, ulong stringOffset)
        {
            var offset = stringOffset;
            ulong length = 0;
            while (buff[offset] != 0x00)
            {
                length++;
                offset++;
            }
            return length;
        }
    }
}
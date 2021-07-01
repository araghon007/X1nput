using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    ///     Represents the section header for one section.
    /// </summary>
    public class IMAGE_SECTION_HEADER : AbstractStructure
    {
        /// <summary>
        ///     Create a new IMAGE_SECTION_HEADER object.
        /// </summary>
        /// <param name="imageBaseAddress">Base address of the image from the Optional header.</param>
        /// <param name="buff">A PE file as a byte array.</param>
        /// <param name="offset">Raw offset to the section header.</param>
        public IMAGE_SECTION_HEADER(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        /// <summary>
        ///     Size of the section when loaded into memory. If it's bigger than
        ///     the raw data size, the rest of the section is filled with zeros.
        /// </summary>
        public uint VirtualSize
        {
            get => Buff.BytesToUInt32(Offset + 0x8);
            set => Buff.SetUInt32(Offset + 0x8, value);
        }

        /// <summary>
        ///     RVA of the section start in memory.
        /// </summary>
        public uint VirtualAddress
        {
            get => Buff.BytesToUInt32(Offset + 0xC);
            set => Buff.SetUInt32(Offset + 0xC, value);
        }

        /// <summary>
        ///     Raw address of the section in the file.
        /// </summary>
        public uint PointerToRawData
        {
            get => Buff.BytesToUInt32(Offset + 0x14);
            set => Buff.SetUInt32(Offset + 0x14, value);
        }
    }
}

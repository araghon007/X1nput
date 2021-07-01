using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    ///     The File header contains information about the structure
    ///     and properties of the PE file.
    /// </summary>
    public class IMAGE_FILE_HEADER : AbstractStructure
    {
        /// <summary>
        ///     Create a new IMAGE_FILE_HEADER object.
        /// </summary>
        /// <param name="buff">A PE file as byte array.</param>
        /// <param name="offset">Raw offset to the file header.</param>
        public IMAGE_FILE_HEADER(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        /// <summary>
        ///     The machine (CPU type) the PE file is intended for.
        ///     Can be resolved with Utility.ResolveTargetMachine(machine).
        /// </summary>
        public ushort Machine
        {
            get => Buff.BytesToUInt16(Offset);
            set => Buff.SetUInt16(Offset, value);
        }

        /// <summary>
        ///     The number of sections in the PE file.
        /// </summary>
        public ushort NumberOfSections
        {
            get => Buff.BytesToUInt16(Offset + 0x2);
            set => Buff.SetUInt16(Offset + 0x2, value);
        }

        /// <summary>
        ///     The size of the optional header which follow the file header.
        /// </summary>
        public ushort SizeOfOptionalHeader
        {
            get => Buff.BytesToUInt16(Offset + 0x10);
            set => Buff.SetUInt16(Offset + 0x10, value);
        }
    }
}
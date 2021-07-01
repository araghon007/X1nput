using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    ///     The export directory contains all exported function, symbols etc.
    ///     which can be used by other module.
    /// </summary>
    public class IMAGE_EXPORT_DIRECTORY : AbstractStructure
    {
        /// <summary>
        ///     Create a new IMAGE_EXPORT_DIRECTORY object.
        /// </summary>
        /// <param name="buff">PE file as a byte array.</param>
        /// <param name="offset">Raw offset of the export directory in the PE file.</param>
        public IMAGE_EXPORT_DIRECTORY(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }
        /// <summary>
        ///     Name.
        /// </summary>
        public uint Name
        {
            get => Buff.BytesToUInt32(Offset + 0xC);
            set => Buff.SetUInt32(Offset + 0xC, value);
        }
        
        /// <summary>
        ///     Number of exported functions.
        /// </summary>
        public uint NumberOfFunctions
        {
            get => Buff.BytesToUInt32(Offset + 0x14);
            set => Buff.SetUInt32(Offset + 0x14, value);
        }

        /// <summary>
        ///     Number of exported names.
        /// </summary>
        public uint NumberOfNames
        {
            get => Buff.BytesToUInt32(Offset + 0x18);
            set => Buff.SetUInt32(Offset + 0x18, value);
        }

        /// <summary>
        ///     RVA to the addresses of the functions.
        /// </summary>
        public uint AddressOfFunctions
        {
            get => Buff.BytesToUInt32(Offset + 0x1C);
            set => Buff.SetUInt32(Offset + 0x1C, value);
        }

        /// <summary>
        ///     RVA to the addresses of the names.
        /// </summary>
        public uint AddressOfNames
        {
            get => Buff.BytesToUInt32(Offset + 0x20);
            set => Buff.SetUInt32(Offset + 0x20, value);
        }

        /// <summary>
        ///     RVA to the name ordinals.
        /// </summary>
        public uint AddressOfNameOrdinals
        {
            get => Buff.BytesToUInt32(Offset + 0x24);
            set => Buff.SetUInt32(Offset + 0x24, value);
        }
    }
}
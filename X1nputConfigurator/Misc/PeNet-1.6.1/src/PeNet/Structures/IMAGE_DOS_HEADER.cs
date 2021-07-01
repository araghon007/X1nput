using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    ///     The IMAGE_DOS_HEADER with which every PE file starts.
    /// </summary>
    public class IMAGE_DOS_HEADER : AbstractStructure
    {
        /// <summary>
        ///     Create a new IMAGE_DOS_HEADER object.
        /// </summary>
        /// <param name="buff">Byte buffer containing a PE file.</param>
        /// <param name="offset">Offset in the buffer to the DOS header.</param>
        public IMAGE_DOS_HEADER(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        /// <summary>
        ///     Raw address of the NT header.
        /// </summary>
        public uint e_lfanew
        {
            get => Buff.BytesToUInt32(Offset + 0x3C);
            set => Buff.SetUInt32(Offset + 0x3C, value);
        }
    }
}
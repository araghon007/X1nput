namespace PeNet.Structures
{
    /// <summary>
    ///     Represents the optional header in
    ///     the NT header.
    /// </summary>
    public class IMAGE_OPTIONAL_HEADER : AbstractStructure
    {
        private readonly bool _is64Bit;

        /// <summary>
        ///     The Data Directories.
        /// </summary>
        public readonly IMAGE_DATA_DIRECTORY[] DataDirectory;

        /// <summary>
        ///     Create a new IMAGE_OPTIONAL_HEADER object.
        /// </summary>
        /// <param name="buff">A PE file as a byte array.</param>
        /// <param name="offset">Raw offset to the optional header.</param>
        /// <param name="is64Bit">Set to true, if header is for a x64 application.</param>
        public IMAGE_OPTIONAL_HEADER(byte[] buff, uint offset, bool is64Bit)
            : base(buff, offset)
        {
            _is64Bit = is64Bit;

            DataDirectory = new IMAGE_DATA_DIRECTORY[16];

            for (uint i = 0; i < 16; i++)
            {
                if (!_is64Bit)
                    DataDirectory[i] = new IMAGE_DATA_DIRECTORY(buff, offset + 0x60 + i*0x8);
                else
                    DataDirectory[i] = new IMAGE_DATA_DIRECTORY(buff, offset + 0x70 + i*0x8);
            }
        }
    }
}
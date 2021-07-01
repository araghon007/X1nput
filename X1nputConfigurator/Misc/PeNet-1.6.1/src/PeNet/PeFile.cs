using System.IO;
using PeNet.Structures;

namespace PeNet
{
    /// <summary>
    ///     This class represents a Portable Executable (PE) file and makes the different
    ///     header and properties accessible.
    /// </summary>
    public class PeFile : AbstractStructure
    {
        private readonly DataDirectoryParsers _dataDirectoryParsers;
        private readonly NativeStructureParsers _nativeStructureParsers;

        /// <summary>
        ///     The PE binary as a byte array.
        /// </summary>
        public new byte[] Buff => base.Buff;

        /// <summary>
        ///     Create a new PeFile object.
        /// </summary>
        /// <param name="buff">A PE file a byte array.</param>
        public PeFile(byte[] buff) : base(buff, 0)
        {
            _nativeStructureParsers = new NativeStructureParsers(Buff);

            _dataDirectoryParsers = new DataDirectoryParsers(
                Buff,
                ImageNtHeaders?.OptionalHeader?.DataDirectory,
                ImageSectionHeaders
                );

        }

        /// <summary>
        ///     Create a new PeFile object.
        /// </summary>
        /// <param name="peFile">Path to a PE file.</param>
        public PeFile(string peFile)
            : this(File.ReadAllBytes(peFile))
        {
        }
        
        /// <summary>
        ///     Access the IMAGE_NT_HEADERS of the PE file.
        /// </summary>
        public IMAGE_NT_HEADERS ImageNtHeaders => _nativeStructureParsers.ImageNtHeaders;

        /// <summary>
        ///     Access the IMAGE_SECTION_HEADERS of the PE file.
        /// </summary>
        public IMAGE_SECTION_HEADER[] ImageSectionHeaders => _nativeStructureParsers.ImageSectionHeaders;

        /// <summary>
        ///     Access the exported functions as an array of parsed objects.
        /// </summary>
        public ExportFunction[] ExportedFunctions => _dataDirectoryParsers.ExportFunctions;
    }
}
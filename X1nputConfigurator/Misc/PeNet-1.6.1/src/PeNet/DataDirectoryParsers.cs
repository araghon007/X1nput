using System.Collections.Generic;
using System.Linq;
using PeNet.Parser;
using PeNet.Structures;
using PeNet.Utilities;

namespace PeNet
{
    internal class DataDirectoryParsers
    {
        private readonly byte[] _buff;
        private readonly IMAGE_DATA_DIRECTORY[] _dataDirectories;

        private readonly IMAGE_SECTION_HEADER[] _sectionHeaders;
        private ExportedFunctionsParser _exportedFunctionsParser;
        private ImageExportDirectoriesParser _imageExportDirectoriesParser;

        public DataDirectoryParsers(
            byte[] buff,
            IEnumerable<IMAGE_DATA_DIRECTORY> dataDirectories,
            IEnumerable<IMAGE_SECTION_HEADER> sectionHeaders
            )
        {
            _buff = buff;
            _dataDirectories = dataDirectories.ToArray();
            _sectionHeaders = sectionHeaders.ToArray();

            InitAllParsers();
        }

        public IMAGE_EXPORT_DIRECTORY ImageExportDirectories => _imageExportDirectoriesParser?.GetParserTarget();
        public ExportFunction[] ExportFunctions => _exportedFunctionsParser?.GetParserTarget();

        private void InitAllParsers()
        {
            _imageExportDirectoriesParser = InitImageExportDirectoryParser();
            _exportedFunctionsParser = InitExportFunctionParser();
        }

        private ExportedFunctionsParser InitExportFunctionParser()
        {
            return new ExportedFunctionsParser(
                _buff, 
                ImageExportDirectories, 
                _sectionHeaders);
        }

        private ImageExportDirectoriesParser InitImageExportDirectoryParser()
        {
            var rawAddress =
                _dataDirectories[0].VirtualAddress.SafeRVAtoFileMapping(_sectionHeaders);
            if (rawAddress == null)
                return null;

            return new ImageExportDirectoriesParser(_buff, rawAddress.Value);
        }
    }
}
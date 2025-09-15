using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.Interfaces
{
    public interface IFileProcessorService
    {
        /// <summary>
        /// Generates a copy of the specified file in the given directory with a unique name based on the current
        /// timestamp.
        /// </summary>
        /// <remarks>The copied file will have a name in the format
        /// "Copy_yyyyMMddHHmmss_[OriginalFileName]" to ensure uniqueness.</remarks>
        /// <param name="directoryPath">The path to the directory where the file is located and where the copy will be created.</param>
        /// <param name="fileName">The name of the file to be copied, including its extension.</param>
        /// <returns>The full path of the newly created copy of the file, or an empty string if the specified directory or file
        /// does not exist.</returns>
        string GetCopyPath(string directoryPath, string fileName);
    }
}

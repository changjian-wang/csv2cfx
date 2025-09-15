using CSV2CFX.Interfaces;
using CSV2CFX.Models;
using Microsoft.Extensions.Logging;
using System.IO;

namespace CSV2CFX.Services
{
    public class FileProcessorService : IFileProcessorService
    {
        private readonly ILogger<FileProcessorService> _logger;
        public FileProcessorService(ILogger<FileProcessorService> logger)
        {
            _logger = logger;
        }

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
        public string GetCopyPath(string directoryPath, string fileName)
        {
            if (Directory.Exists(directoryPath) && File.Exists(fileName))
            {
                var filePath = Path.Combine(directoryPath, fileName);
                var copyFilePath = Path.Combine(directoryPath, $"Copy_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{fileName}");
                File.Copy(filePath, copyFilePath);

                _logger.LogInformation($"File copied from {filePath} to {copyFilePath}");

                return copyFilePath;
            }

            return "";
        }
    }
}

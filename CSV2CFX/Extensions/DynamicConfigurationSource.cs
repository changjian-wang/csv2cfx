using Microsoft.Extensions.Configuration;

namespace CSV2CFX.Extensions
{
    public class DynamicConfigurationSource : IConfigurationSource
    {
        private readonly string _filePath;

        public DynamicConfigurationSource(string filePath)
        {
            _filePath = filePath;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new DynamicConfigurationProvider(_filePath);
        }
    }
}
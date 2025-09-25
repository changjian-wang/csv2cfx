namespace CSV2CFX.Interfaces
{
    public interface IDynamicConfigurationService
    {
        Task UpdateConfigurationAsync<T>(T configuration, string sectionName = null);
        Task SaveConfigurationToFileAsync<T>(T configuration, string filePath, string sectionName = null);
        Task LoadConfigurationFromFileAsync(string filePath);
    }
}
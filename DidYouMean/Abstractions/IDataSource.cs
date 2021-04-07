using System.Threading.Tasks;

namespace DidYouMean.Abstractions
{
    /// <summary>
    /// I expect this will be replaced by an interface on your end
    /// </summary>
    public interface IDataSource
    {
        Task<string> GetAllAsync();

        Task<bool> ExistsAsync(string s);
    }
}
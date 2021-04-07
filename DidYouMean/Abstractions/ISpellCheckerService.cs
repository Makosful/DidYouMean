using System.Collections.Generic;
using System.Threading.Tasks;

namespace DidYouMean.Abstractions
{
    public interface ISpellCheckerService
    {
        IDataSource _dataSource { get; set; }

        Task<IEnumerable<string>> GetSimilarWordsAsync(string s, double maxDistance, int maxAmount = 0);
        
        Task<IEnumerable<string>> GetSimilarWordsForceAsync(string s, double maxDistance, int maxAmount = 0);

        double Distance(string a, string b);
    }
}
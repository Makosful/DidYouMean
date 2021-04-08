using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DidYouMean.Abstractions;
using Microsoft.Extensions.Logging;

namespace DidYouMean
{
    public class SpellCheckerService : ISpellCheckerService
    {
        private readonly ILogger _logger;
        
        public SpellCheckerService(IDataSource dataSource, ILogger<SpellCheckerService> logger)
        {
            _dataSource = dataSource;
            _logger = logger;
        }

        // Hint. This format is considered a property.
        // Properties uses the PascalCase.
        // Private fields use the leading _underscore
        public IDataSource _dataSource { get; set; }

        public async Task<IEnumerable<string>> GetSimilarWordsAsync(string s, double maxDistance, int maxAmount = 0)
        {
            // DataSource null check
            if (_dataSource == null) throw new InvalidOperationException($"{nameof(IDataSource)} in {nameof(ISpellCheckerService)} is null");

            // Fetches everything from the data source
            IEnumerable<string> dictionary = await _dataSource.GetAllAsync();
            // Creates a dictionary to store the word and it's distance
            Dictionary<string, double> matches = new();
            //List<string> matches = new();

            // Loops through every word in the dictionary
            foreach (string word in dictionary)
            {
                _logger.Log(LogLevel.Debug, "Trying to compare [{Search}] with [{Dictionary}]", s, word);
                
                // Compute the distance between the dictionary word and the searched word
                double distance = Distance(s, word);
                // If distance is less than 1, return empty list
                if (distance <= 0)
                {
                    _logger.Log(LogLevel.Debug, "Exact match found");
                    return new List<string>();
                }
                // If distance is higher than max distance, discard it, otherwise save it 
                if (distance <= maxDistance)
                {
                    matches.Add(word, distance);
                    _logger.Log(LogLevel.Debug, "Possible match found: [{Match}] - [{Distance}]", word, distance);
                }
                else
                {
                    _logger.Log(LogLevel.Debug, "[{Match}] did not meet the distance threshold [{Distance}]", word, distance);
                }
            }
            
            var result = matches
                // Orders the matches by distance, ascending (closest match first)
                .OrderBy(x => x.Value)
                // Converts the KeyPair value to just a string
                .Select(x => x.Key)
                .ToList();

            // No max amount has been set. Return all
            if (maxAmount <= 0) return result;
            
            // If the amount of results are more than the max amount
            if (maxAmount < result.Count)
            {
                _logger.Log(LogLevel.Debug, "Enforcing maxAmount of: [{Amount}]", maxAmount);
                // Only return the first set
                var take = result.Take(maxAmount);
                _logger.Log(LogLevel.Debug, "The following words have been taken: [{Take}]", take);
                return take;
            }
            // Otherwise, if the count is less than maxAmount, just ignore it
            return result;
        }

        public async Task<IEnumerable<string>> GetSimilarWordsForceAsync(string s, double maxDistance, int maxAmount = 0)
        {
            // DataSource null check
            if (_dataSource == null) throw new InvalidOperationException($"{nameof(IDataSource)} in {nameof(ISpellCheckerService)} is null");

            // Fetches everything from the data source
            IEnumerable<string> dictionary = await _dataSource.GetAllAsync();
            // Creates a dictionary to store the word and it's distance
            Dictionary<string, double> matches = new();
            //List<string> matches = new();

            // Loops through every word in the dictionary
            foreach (string word in dictionary)
            {
                _logger.Log(LogLevel.Debug, "Trying to compare [{Search}] with [{Dictionary}]", s, word);

                // Compute the distance between the dictionary word and the searched word
                double distance = Distance(s, word);

                // If distance is higher than max distance, discard it, otherwise save it 
                if (distance <= maxDistance)
                {
                    matches.Add(word, distance);
                    _logger.Log(LogLevel.Debug, "Possible match found: [{Match}] - [{Distance}]", word, distance);
                }
                else
                {
                    _logger.Log(LogLevel.Debug, "[{Match}] did not meet the distance threshold [{Distance}]", word,
                        distance);
                }
            }
            
            var result = matches
                // Orders the matches by distance, ascending (closest match first)
                .OrderBy(x => x.Value)
                // Converts the KeyPair value to just a string
                .Select(x => x.Key)
                .ToList();

            // No max amount has been set. Return all
            if (maxAmount <= 0) return result;
            
            // If the amount of results are more than the max amount
            if (maxAmount < result.Count)
            {
                _logger.Log(LogLevel.Debug, "Enforcing maxAmount of: [{Amount}]", maxAmount);
                // Only return the first set
                var take = result.Take(maxAmount);
                _logger.Log(LogLevel.Debug, "The following words have been taken: [{Take}]", take);
                return take;
            }
            // Otherwise, if the count is less than maxAmount, just ignore it
            return result;
        }

        public double Distance(string a, string b)
        {
            // Null check
            if (_dataSource == null) throw new InvalidOperationException($"{nameof(IDataSource)} in {nameof(ISpellCheckerService)} is null");
            if (string.IsNullOrWhiteSpace(a)) throw new ArgumentException($"Given string is invalid", nameof(a));
            if (string.IsNullOrWhiteSpace(b)) throw new ArgumentException($"Given string is invalid", nameof(b));

            // Ignore case
            a = a.ToLower();
            b = b.ToLower();
            char[] aArr, bArr;

            // Sets the array lengths to match the longest word
            // to avoid OutOfBounds.
            // Empty indices are given \0 null pointer
            if (a.Length >= b.Length)
            {
                aArr = new char[a.Length];
                bArr = new char[a.Length];
            }
            else
            {
                aArr = new char[b.Length];
                bArr = new char[b.Length];
            }

            // Puts the strings into their arrays
            // Does not use string.ToCharArray() because that's not guaranteed to give them the same size
            PopulateArrays(a, aArr);
            PopulateArrays(b, bArr);

            // Measuring the distance between the two words
            double distance = MeasureWordDistanceRecursive(aArr, bArr);

            return distance;
        }

        /// <summary>
        /// Measures the distance between two words, formatted as char arrays.
        /// This method will alter the <paramref name="current"/> array before
        /// recursively calling itself with the updated array. Once no more
        /// chances need to be made, the distance between the words will be
        /// returned
        /// </summary>
        /// <param name="current">
        /// The word to measure, in the format of a char array
        /// </param>
        /// <param name="target">
        /// The target word to measure against, in the format of a char array
        /// </param>
        /// <param name="distance">
        /// The current distance between the two words. Used to keep track of the current distance recursive calls
        /// </param>
        /// <returns>
        /// Returns the computed between the two words
        /// </returns>
        private double MeasureWordDistanceRecursive(char[] current, char[] target, double distance = 0)
        {
            _logger.Log(LogLevel.Debug, 
                "Target: [{Target}] - Current: [{Current}] - Distance: [{Distance}]", 
                target, current, distance);
            
            // Loops through the target word
            for (int i = 0; i < target.Length; i++)
            {
                // Gets a set of relative characters
                char expected = target[i];
                char expectedNext = i == (target.Length - 1) ? '\0' : target[i + 1];
                char actual = current[i];
                char actualNext = i == (current.Length - 1) ? '\0' : current[i + 1];
                char actualPrev = i == 0 ? '\0' : current[i - 1];
                
                // If the two characters on the current index matches
                if (actual == expected)
                {
                    // move in to the index
                    continue;
                }

                // Checks the current actual letter vs the next expected letter
                // Checks the next actual letter vs the current expected letter 
                if (actual == expectedNext && actualNext == expected)
                {
                    // Swap actual current and actual next
                    char tmp = current[i];
                    current[i] = current[i + 1];
                    current[i + 1] = tmp;
                    
                    // Add the distance
                    distance += 0.75;
                    
                    // Recursively call this method with the updated array and distance
                    return MeasureWordDistanceRecursive(current, target, distance);
                }

                // Checks the previous actual letter and the current expected letter
                // Checks the current actual letter and the next expected letter
                if (actualPrev == expected && actual == expectedNext)
                {
                    // Shift the array over once and insert the correct char
                    ShiftArray(current, i);
                    current[i] = target[i];
                    distance += 2;
                    
                    // Recursively call this method with the updated array and distance
                    return MeasureWordDistanceRecursive(current, target, distance);
                }

                if (expected == '\0')
                {
                    // If the target has a null pointer, the current must remove the char
                    current[i] = '\0';
                    distance += 1;
                    
                    // Recursively call this method with the updated array and distance
                    return MeasureWordDistanceRecursive(current, target, distance);
                }

                if (expected != '\0' && actual == '\0')
                {
                    current[i] = target[i];
                    distance += 2d;

                    return MeasureWordDistanceRecursive(current, target, distance);
                }

                // Replace the char in current with the correct char from target
                current[i] = target[i];
                distance += 0.5;
                
                // Recursively call this method with the updated array and distance
                return MeasureWordDistanceRecursive(current, target, distance);
            }
            
            return distance;
        }

        /// <summary>
        /// Shifts the array over one index, from the back up until the index
        /// </summary>
        /// <param name="arr">The array to work on</param>
        /// <param name="index">The index to start from</param>
        private static void ShiftArray(char[] arr, int index)
        {
            // Avoids OutOfBounds
            // If index is higher than array length, the method will exit on it's own
            if (index < 0) return;
            
            // Going backwards through the array until it hits the index
            for (int j = arr.Length - 1; j > index; j--)
            {
                arr[j] = arr[j - 1];
            }
        }

        /// <summary>
        /// Puts the string into an existing array
        /// </summary>
        /// <param name="str"></param>
        /// <param name="arr"></param>
        private static void PopulateArrays(string str, char[] arr)
        {
            for (int i = 0; i < str.Length; i++)
            {
                arr[i] = str[i];
            }
        }
    }
}
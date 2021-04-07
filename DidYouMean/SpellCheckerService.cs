using System;
using System.Collections.Generic;
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

        public IDataSource _dataSource { get; set; }

        public Task<IEnumerable<string>> GetSimilarWordsAsync(string s, double maxDistance, int maxAmount = 0)
        {
            if (_dataSource == null) throw new InvalidOperationException($"{nameof(IDataSource)} in {nameof(ISpellCheckerService)} is null");
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<string>> GetSimilarWordsForceAsync(string s, double maxDistance, int maxAmount = 0)
        {
            if (_dataSource == null) throw new InvalidOperationException($"{nameof(IDataSource)} in {nameof(ISpellCheckerService)} is null");
            throw new System.NotImplementedException();
        }

        public double Distance(string a, string b)
        {
            // Null check
            if (_dataSource == null) throw new InvalidOperationException($"{nameof(IDataSource)} in {nameof(ISpellCheckerService)} is null");
            if (string.IsNullOrWhiteSpace(a)) throw new ArgumentException($"Given string is invalid", nameof(a));
            if (string.IsNullOrWhiteSpace(b)) throw new ArgumentException($"Given string is invalid", nameof(b));

            a = a.ToLower();
            b = b.ToLower();
            char[] aArr, bArr;

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

            PopulateArrays(a, aArr);
            PopulateArrays(b, bArr);

            double distance = MeasureWordDistanceRecursive(aArr, bArr);

            return distance;
        }

        private double MeasureWordDistanceRecursive(char[] current, char[] target, double distance = 0)
        {
            _logger.Log(
                LogLevel.Debug, 
                "Target: [{Target}] - Current: [{Current}] - Distance: [{Distance}]", 
                target, current, distance);
            
            for (int i = 0; i < target.Length; i++)
            {
                char expected = target[i];
                char expectedNext = i == (target.Length - 1) ? '\0' : target[i + 1];
                char actual = current[i];
                char actualNext = i == (current.Length - 1) ? '\0' : current[i + 1];
                char actualPrev = i == 0 ? '\0' : current[i - 1];

                if (actual == expected)
                {
                    continue;
                }

                if (actual == expectedNext && actualNext == expected)
                {
                    // If the letter is one position off, swap them
                    char tmp = current[i];
                    current[i] = current[i + 1];
                    current[i + 1] = tmp;
                    distance += 0.75;
                    return MeasureWordDistanceRecursive(current, target, distance);
                }

                if (actualPrev == expected && actual == expectedNext)
                {
                    // Shift the array over once and insert the correct char
                    ShiftArray(current, i);
                    current[i] = target[i];
                    distance += 2;
                    return MeasureWordDistanceRecursive(current, target, distance);
                }

                if (expected == '\0')
                {
                    // If the target has a null pointer, the current must remove the char
                    current[i] = '\0';
                    distance += 1;
                    return MeasureWordDistanceRecursive(current, target, distance);
                }

                // Replace the char in current with the correct char from target
                current[i] = target[i];
                distance += 0.5;
                return MeasureWordDistanceRecursive(current, target, distance);
            }
            
            return distance;
        }

        private static void ShiftArray(char[] arr, int i)
        {
            for (int j = arr.Length - 1; j > i; j--)
            {
                arr[j] = arr[j - 1];
            }
        }

        private static void PopulateArrays(string str, char[] arr)
        {
            for (int i = 0; i < str.Length; i++)
            {
                arr[i] = str[i];
            }
        }
    }
}
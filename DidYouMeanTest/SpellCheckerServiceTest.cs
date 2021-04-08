using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DidYouMean;
using DidYouMean.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace DidYouMeanTest
{
    public class SpellCheckerServiceTest
    {
        private readonly ITestOutputHelper _output;

        public SpellCheckerServiceTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private static Mock<IDataSource> CreateMockDataSource()
        {
            Mock<IDataSource> mock = new Mock<IDataSource>();

            mock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<string>
                {
                    "abc", "abcd", "bcd", "acd", "dca", "aa", "cba", "cc",
                });
            return mock;
        }
        
        private ISpellCheckerService CreateSpellCheckerService(out Mock<IDataSource> mockDataSource)
        {
            mockDataSource = CreateMockDataSource();

            // logging 
            ILogger<SpellCheckerService> logger = _output.BuildLoggerFor<SpellCheckerService>();

            ISpellCheckerService service = new SpellCheckerService(
                mockDataSource.Object,
                logger);
            
            return service;
        }
        
        [Fact]
        public async Task GetSimilarWordsAsync_IDataSource_IsNull_ExpectException()
        {
            // Arrange
            ISpellCheckerService service = CreateSpellCheckerService(
                out Mock<IDataSource> mockDataSource);
            service._dataSource = null;
            
            // Act
            async Task Act() => await service.GetSimilarWordsAsync(
                It.IsAny<string>(),
                It.IsAny<double>(),
                It.IsAny<int>());

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(Act);
        }

        [Theory]
        [InlineData("ab",  2, 0, new[] {"aa", "cc", "abc"})]
        [InlineData("cd",  1, 0, new[] {"cc", "aa"})]
        [InlineData("abc", 1, 0, new string[0])]
        [InlineData("acc", 3, 2, new[] {"abc", "acd"})]
        [InlineData("a",   6, 0, new[] {"aa", "cc", "abc", "acd", "bcd", "dca", "cba", "abcd"})]
        public async Task GetSimilarWordsAsync_ResponseTest(string input, int maxDistance, int maxAmount, string[] results)
        {
            // Arrange
            ISpellCheckerService service = CreateSpellCheckerService(
                out Mock<IDataSource> mockDataSource);
            
            // Act
            IEnumerable<string> actual = await service.GetSimilarWordsAsync(input, maxDistance, maxAmount);

            // Assert
            actual.Should()
                .Equal(results);
        }
        
        [Fact]
        public async Task GetSimilarWordsForceAsync_IDataSource_IsNull_ExpectException()
        {
            // Arrange
            ISpellCheckerService service = CreateSpellCheckerService(
                out Mock<IDataSource> mockDataSource);
            service._dataSource = null;
            
            // Act
            async Task Act() => await service.GetSimilarWordsForceAsync(
                It.IsAny<string>(),
                It.IsAny<double>(),
                It.IsAny<int>());

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(Act);
        }

        [Theory]
        [InlineData("abc",  1, 3, new[] {"abc", "acd", "cba"})]
        [InlineData("abdc", 1, 0, new[] {"abcd"})]
        [InlineData("ab",   2, 0, new[] {"aa", "cc", "abc"})]
        [InlineData("cd",   1, 0, new[] {"cc", "aa"})]
        [InlineData("acc",  3, 2, new[] {"abc", "acd"})]
        [InlineData("a",    6, 0, new[] {"aa", "cc", "abc", "acd", "bcd", "dca", "cba", "abcd"})]
        public async Task GetSimilarWordsForceAsync(string input, int maxDistance, int maxAmount, string[] results)
        {
            // Arrange
            ISpellCheckerService service = CreateSpellCheckerService(
                out Mock<IDataSource> mockDataSource);
            
            // Act
            IEnumerable<string> actual = await service.GetSimilarWordsForceAsync(input, maxDistance, maxAmount);
            
            // Assert
            actual.Should()
                .Equal(results);
        }
        
        [Fact]
        public void Distance_IDataSource_IsNull_ExpectException()
        {
            // Arrange
            ISpellCheckerService service = CreateSpellCheckerService(
                out Mock<IDataSource> mockDataSource);
            service._dataSource = null;
            
            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                service.Distance(It.IsAny<string>(), It.IsAny<string>());
            });
        }

        [Theory]
        [InlineData("Piza",   "Pizza", 2.00d)]
        [InlineData("Pizza",  "Pizza", 0.00d)]
        [InlineData("Pixxa",  "Pizza", 1.00d)]
        [InlineData("Pizaz",  "Pizza", 0.75d)]
        [InlineData("Pizzas", "Pizza", 1.00d)]
        [InlineData("Pixxas", "Pizza", 2.00d)]
        [InlineData("c",    "cc",   2.00d)]
        [InlineData("ab",   "ac",   0.50d)]
        [InlineData("abc",  "acb",  0.75d)]
        [InlineData("ccc",  "cc",   1.00d)]
        [InlineData("abcd", "acbc", 1.25d)]
        [InlineData("cc",   "cc",   0.00d)]
        public void Distance_MeasureDistance(string input, string data, double distance)
        {
            // arrange
            ISpellCheckerService service = CreateSpellCheckerService(
                out Mock<IDataSource> mockDataSource);
            
            // Act
            double actual = service.Distance(input, data);
            
            // Assert
            Assert.Equal(distance, actual);
            mockDataSource.Verify(x => x.ExistsAsync(It.IsAny<string>()), Times.Never);
            mockDataSource.Verify(x => x.GetAllAsync(), Times.Never);
        }
    }
}
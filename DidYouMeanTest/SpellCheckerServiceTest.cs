using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DidYouMean;
using DidYouMean.Abstractions;
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
        [InlineData("Piza",   "Pizza", 2.00f)]
        [InlineData("Pizza",  "Pizza", 0.00f)]
        [InlineData("Pixxa",  "Pizza", 1.00f)]
        [InlineData("Pizaz",  "Pizza", 0.75f)]
        [InlineData("Pizzas", "Pizza", 1.00f)]
        [InlineData("Pixxas", "Pizza", 2.00f)]
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
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;
using NUnit.Framework;

namespace SeqLoggerProvider.Test.Extensions.Microsoft.Extensions.Logging.SeqLoggerLoggingBuilderExtensions
{
    [TestFixture]
    public class AddSeq
    {
        [Test]
        public void Always_ServiceCollectionIsValid()
        {
            var services = new ServiceCollection();

            services.AddLogging(builder => builder
                .AddSeq());

            using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                ValidateOnBuild = true,
                ValidateScopes  = true
            });
        }

        [Test]
        public void ConfigureIsGiven_ConfigureIsInvoked()
        {
            var services = new ServiceCollection();

            var mockConfigure = new Mock<Action<SeqLoggerConfiguration>>();

            services.AddLogging(builder => builder
                .AddSeq(configure: mockConfigure.Object));

            using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                ValidateOnBuild = true,
                ValidateScopes  = true
            });

            _ = serviceProvider.GetRequiredService<IOptions<SeqLoggerConfiguration>>().Value;

            mockConfigure.Verify(
                x => x(It.IsNotNull<SeqLoggerConfiguration>()),
                Times.Once);
        }

        [Test]
        public void ConfigureHttpClientIsGiven_ConfigureHttpClientIsInvoked()
        {
            var services = new ServiceCollection();

            var mockConfigureHttpClient = new Mock<Action<IHttpClientBuilder>>();

            services.AddLogging(builder => builder
                .AddSeq(configureHttpClient: mockConfigureHttpClient.Object));

            using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                ValidateOnBuild = true,
                ValidateScopes  = true
            });

            _ = serviceProvider.GetRequiredService<ILoggerFactory>();

            mockConfigureHttpClient.Verify(
                x => x(It.IsNotNull<IHttpClientBuilder>()),
                Times.Once);
        }
    }
}

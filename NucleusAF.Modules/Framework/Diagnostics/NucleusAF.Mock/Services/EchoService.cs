using Microsoft.Extensions.Logging;
using NucleusAF.Interfaces.Services.Modules;
using NucleusAF.Mock.Interfaces.Providers;
using NucleusAF.Mock.Interfaces.Services;
using NucleusAF.Mock.Models.Descriptors;

namespace NucleusAF.Mock.Services
{
    /// <summary>
    /// Extensible echo service that accepts descriptors through constructor injection.
    /// Other modules implementing IProviderEchoResult are automatically injected.
    /// </summary>
    public class EchoService : IEchoService
    {
        protected readonly ILogger<IEchoService> Logger;
        protected readonly IEnumerable<DescriptorEchoResult> EchoDescriptors;

        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
        public event EventHandler<MessageErrorEventArgs>? MessageError;

        public EchoService(ILogger<IEchoService> logger, IModuleResolver resolver, IEnumerable<IProviderEchoResult> resultHandlers)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(resolver);
            ArgumentNullException.ThrowIfNull(resultHandlers);

            this.Logger = logger;

            this.EchoDescriptors = ModulesConfigs(resolver, resultHandlers);
        }

        /// <summary>
        /// Echoes the provided message while triggering events that registered handlers can process.
        /// </summary>
        public string Echo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                MessageErrorEventArgs errorArgs = new(message, "The message provided was emtpy.");

                this.Logger?.LogInformation("[EchoService] Message Error!");

                foreach (DescriptorEchoResult descriptor in this.EchoDescriptors)
                    if (descriptor != null && descriptor.ProcessError != null)
                        descriptor.ProcessError(errorArgs, this);

                this.MessageError?.Invoke(this, errorArgs);
            }

            MessageReceivedEventArgs successArgs = new(message);

            this.Logger?.LogInformation("[EchoService] Message Received: {message}", message);

            foreach (DescriptorEchoResult descriptor in this.EchoDescriptors)
                if (descriptor != null && descriptor.ProcessSuccess != null)
                    descriptor.ProcessSuccess(successArgs, this);

            this.MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));

            return message;
        }

        private static IEnumerable<DescriptorEchoResult> ModulesConfigs(IModuleResolver resolver, IEnumerable<IProviderEchoResult> resultHandlers)
        {
            ArgumentNullException.ThrowIfNull(resultHandlers);

            return resolver.ResolveAndOrder<IProviderEchoResult, DescriptorEchoResult>(resultHandlers);
        }
    }
}
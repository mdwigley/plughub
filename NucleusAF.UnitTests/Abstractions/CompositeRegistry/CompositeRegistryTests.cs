using Microsoft.Extensions.DependencyInjection;
using NucleusAF.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Abstractions.CompositeRegistry;

namespace NucleusAF.UnitTests.Abstractions.CompositeRegistry
{
    [TestClass]
    public class CompositeRegistryTests
    {
        #region CompositeRegistryTests: Handler Keys

        internal class SomeTypeA { }
        internal class SomeTypeB { }

        #endregion

        #region CompositeRegistryTests: Handler Contracts

        internal interface ICompServTestHandler : ICompositeRegistryHandler
        {
            string Name { get; }
        }
        internal interface ICompServTestHandler<THandled> : ICompServTestHandler { }
        internal interface ICompServTestHandlerA : ICompServTestHandler { }
        internal interface ICompServTestHandlerB : ICompServTestHandler { }

        #endregion

        #region CompositeRegistryTests: Handler Definitions 

        internal class CompServTestHandlerA
            : ICompServTestHandlerA, ICompServTestHandler<SomeTypeA>, ICompositeRegistryComponent
        {
            public string Name => "HandlerA";
            public Type Key => typeof(SomeTypeA);
        }
        internal class CompServTestHandlerB
            : ICompServTestHandlerB, ICompServTestHandler<SomeTypeB>, ICompositeRegistryComponent
        {
            public string Name => "HandlerB";
            public Type Key => typeof(SomeTypeB);
        }

        #endregion

        #region CompositeRegistryTests: Handler Definitions 

        internal class CompServTest(IEnumerable<ICompServTestHandler> handlers)
            : CompositeComponentRegistryBase<ICompServTestHandler>(handlers)
        { }

        #endregion

        private ServiceProvider? serviceProvider;

        [TestInitialize]
        public void Setup()
        {
            ServiceCollection services = new();

            services.AddSingleton<ICompServTestHandler, CompServTestHandlerA>();
            services.AddSingleton<ICompServTestHandler, CompServTestHandlerB>();

            services.AddSingleton<CompServTest>();

            this.serviceProvider = services.BuildServiceProvider();
        }
        [TestCleanup]
        public void Teardown()
        {
            (this.serviceProvider as IDisposable)?.Dispose();
        }

        #region CompositeRegistryTests: Sanity

        [TestMethod]
        [TestCategory("Sanity")]
        public void CanResolveHandlersAndAccessorsByType()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            ICompServTestHandler handlerA = facade.GetComponent(typeof(SomeTypeA));
            Assert.AreEqual("HandlerA", handlerA.Name);

            ICompServTestHandler handlerB = facade.GetComponent(typeof(SomeTypeB));
            Assert.AreEqual("HandlerB", handlerB.Name);
        }

        #endregion
    }
}
using Microsoft.Extensions.DependencyInjection;
using NucleusAF.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Abstractions.CompositeRegistry;

namespace NucleusAF.UnitTests.Abstractions.CompositeRegistry
{
    [TestClass]
    public class CompositeAccessorRegistryTests
    {
        #region CompositeRegistryDiTests: Accessor Contracts

        public interface ICompServTestAccessor : ICompositeRegistryAccessor
        {
            string Label { get; }
        }
        public interface ICompServTestAccessorA : ICompServTestAccessor { }
        public interface ICompServTestAccessorB : ICompServTestAccessor { }

        #endregion

        #region CompositeRegistryDiTests: Accessor Definitions

        public class CompServAccessorA : ICompServTestAccessorA
        {
            public Type Key => typeof(ICompServTestAccessorA);
            public string Label => "AccessorA";
        }
        public class CompServAccessorB : ICompServTestAccessorB
        {
            public Type Key => typeof(ICompServTestAccessorB);
            public string Label => "AccessorB";
        }

        #endregion

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
            : ICompServTestHandlerA,
              ICompServTestHandler<SomeTypeA>,
              ICompositeRegistryHandlerFor<ICompServTestAccessorA>
        {
            public string Name => "HandlerA";
            public Type Key => typeof(SomeTypeA);
        }
        internal class CompServTestHandlerB
            : ICompServTestHandlerB,
              ICompServTestHandler<SomeTypeB>,
              ICompositeRegistryHandlerFor<ICompServTestAccessorB>
        {
            public string Name => "HandlerB";
            public Type Key => typeof(SomeTypeB);
        }
        internal class FakeHandler : ICompServTestHandler
        {
            public string Name => "Fake";
            public Type Key => typeof(Guid);
        }

        #endregion

        #region CompositeRegistryTests: Facade

        internal class CompServTest(IEnumerable<ICompServTestAccessor> accessors, IEnumerable<ICompServTestHandler> handlers)
            : CompositeAccessorRegistryBase<ICompServTestAccessor, ICompServTestHandler>(accessors, handlers)
        {
        }

        #endregion

        private ServiceProvider? serviceProvider;

        [TestInitialize]
        public void Setup()
        {
            ServiceCollection services = new();

            services.AddSingleton<ICompServTestHandler, CompServTestHandlerA>();
            services.AddSingleton<ICompServTestHandler, CompServTestHandlerB>();

            services.AddSingleton<ICompServTestAccessor, CompServAccessorA>();
            services.AddSingleton<ICompServTestAccessor, CompServAccessorB>();

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

            ICompServTestHandler handlerA = facade.GetRegistryHandler(typeof(SomeTypeA));
            Assert.AreEqual("HandlerA", handlerA.Name);

            ICompServTestHandler handlerB = facade.GetRegistryHandler(typeof(SomeTypeB));
            Assert.AreEqual("HandlerB", handlerB.Name);

            ICompServTestAccessor accessorA = facade.GetRegistryAccessor(typeof(ICompServTestAccessorA));
            Assert.AreEqual("AccessorA", accessorA.Label);

            ICompServTestAccessor accessorB = facade.GetRegistryAccessor(typeof(ICompServTestAccessorB));
            Assert.AreEqual("AccessorB", accessorB.Label);
        }

        #endregion

        #region CompositeRegistryTests: Handler

        [TestMethod]
        [TestCategory("THandler")]
        public void CanResolveMultipleHandlersForSameType()
        {
            ServiceCollection services = new();
            services.AddSingleton<ICompServTestHandler, CompServTestHandlerA>();
            services.AddSingleton<ICompServTestHandler, CompServTestHandlerA>();
            services.AddSingleton<ICompServTestAccessor, CompServAccessorA>();
            services.AddSingleton<CompServTest>();
            ServiceProvider provider = services.BuildServiceProvider();

            CompServTest facade = provider.GetRequiredService<CompServTest>();
            IReadOnlyList<ICompServTestHandler> handlers = facade.GetRegistryHandlers(typeof(SomeTypeA));

            Assert.IsTrue(handlers.Count >= 1);
            Assert.AreEqual("HandlerA", handlers[0].Name);
        }

        [TestMethod]
        [TestCategory("Handlers")]
        public void TryGetHandler_ForUnknownType_ReturnsFalse()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool found = facade.TryGetRegistryHandler(typeof(Guid), out ICompServTestHandler? handler);

            Assert.IsFalse(found);
            Assert.IsNull(handler);
        }

        [TestMethod]
        [TestCategory("Handlers")]
        public void GetHandler_UnknownType_Throw()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            KeyNotFoundException ex = Assert.ThrowsException<KeyNotFoundException>(
                () => facade.GetRegistryHandler(typeof(DateTime))
            );

            StringAssert.Contains(ex.Message, "DateTime");
        }

        [TestMethod]
        [TestCategory("Handlers")]
        public void TryGetHandler_WhenHandlerExists_ReturnsTrue()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool found = facade.TryGetRegistryHandler(typeof(SomeTypeA), out ICompServTestHandler? handler);

            Assert.IsTrue(found);
            Assert.IsNotNull(handler);
            Assert.AreEqual("HandlerA", handler!.Name);
        }

        [TestMethod]
        [TestCategory("Handlers")]
        public void TryGetHandler_WhenHandlerMissing_ReturnsFalse()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool found = facade.TryGetRegistryHandler(typeof(Guid), out ICompServTestHandler? handler);

            Assert.IsFalse(found);
            Assert.IsNull(handler);
        }

        [TestMethod]
        [TestCategory("Handlers")]
        public void TryGetHandlers_WhenHandlersExist_ReturnsList()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool found = facade.TryGetRegistryHandlers(typeof(SomeTypeA), out IReadOnlyList<ICompServTestHandler>? handlers);

            Assert.IsTrue(found);
            Assert.IsNotNull(handlers);
            Assert.AreEqual(1, handlers.Count);
            Assert.AreEqual("HandlerA", handlers[0].Name);
        }

        [TestMethod]
        [TestCategory("Handlers")]
        public void TryGetHandlers_WhenNoHandlersExist_ReturnsFalseAndNull()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool found = facade.TryGetRegistryHandlers(typeof(Guid), out IReadOnlyList<ICompServTestHandler>? handlers);

            Assert.IsFalse(found);
            Assert.IsNull(handlers);
        }

        [TestMethod]
        [TestCategory("Handlers")]
        public void IsHandlerRegistered_ForKnownHandler_ReturnsTrue()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();
            ICompServTestHandler handlerA = facade.GetRegistryHandler(typeof(SomeTypeA));

            bool registered = facade.DoesRegistryHandlerExist(typeof(SomeTypeA), handlerA);

            Assert.IsTrue(registered);
        }

        [TestMethod]
        [TestCategory("Handlers")]
        public void IsHandlerRegistered_ForUnknownHandler_ReturnsFalse()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            CompServTestHandlerA fakeHandler = new();

            bool registered = facade.DoesRegistryHandlerExist(typeof(SomeTypeA), fakeHandler);

            Assert.IsFalse(registered);
        }

        #endregion

        #region CompositeRegistryTests: Accessors

        [TestMethod]
        [TestCategory("Accessors")]
        public void TryGetAccessor_ForUnknownAccessor_ReturnsFalse()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool found = facade.TryGetRegistryAccessor(typeof(IDisposable), out ICompServTestAccessor? accessor);

            Assert.IsFalse(found);
            Assert.IsNull(accessor);
        }

        [TestMethod]
        [TestCategory("Accessors")]
        public void GetAccessor_ForUnknownType_Throw()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            KeyNotFoundException ex = Assert.ThrowsException<KeyNotFoundException>(
                () => facade.GetRegistryAccessor(typeof(IDisposable))
            );

            StringAssert.Contains(ex.Message, "IDisposable");
        }

        [TestMethod]
        [TestCategory("Accessors")]
        public void TryGetAccessor_WhenAccessorExists_ReturnsTrue()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool found = facade.TryGetRegistryAccessor(typeof(ICompServTestAccessorA), out ICompServTestAccessor? accessor);

            Assert.IsTrue(found);
            Assert.IsNotNull(accessor);
            Assert.AreEqual("AccessorA", accessor!.Label);
        }

        [TestMethod]
        [TestCategory("Accessors")]
        public void TryGetAccessor_WhenAccessorMissing_ReturnsFalse()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool found = facade.TryGetRegistryAccessor(typeof(IDisposable), out ICompServTestAccessor? accessor);

            Assert.IsFalse(found);
            Assert.IsNull(accessor);
        }
        [TestMethod]
        [TestCategory("Accessors")]
        public void TryGetAccessors_WhenAccessorExists_ReturnsList()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool found = facade.TryGetRegistryAccessors(typeof(ICompServTestAccessorA), out IReadOnlyList<ICompServTestAccessor>? accessors);

            Assert.IsTrue(found);
            Assert.IsNotNull(accessors);
            Assert.AreEqual(1, accessors.Count);
        }

        [TestMethod]
        [TestCategory("Accessors")]
        public void TryGetAccessors_WhenNoAccessorsExist_ReturnsFalseAndNull()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool found = facade.TryGetRegistryAccessors(typeof(Guid), out IReadOnlyList<ICompServTestAccessor>? accessors);

            Assert.IsFalse(found);
            Assert.IsNull(accessors);
        }
        [TestMethod]
        [TestCategory("Accessors")]
        public void IsAccessorRegistered_ForKnownAccessor_ReturnsTrue()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();
            ICompServTestAccessor accessorA = facade.GetRegistryAccessor(typeof(ICompServTestAccessorA));

            bool registered = facade.DoesRegistryAccessorExist(typeof(ICompServTestAccessorA), accessorA);

            Assert.IsTrue(registered);
        }

        [TestMethod]
        [TestCategory("Accessors")]
        public void IsAccessorRegistered_ForUnknownAccessor_ReturnsFalse()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            CompServAccessorA fakeAccessor = new();
            bool registered = facade.DoesRegistryAccessorExist(typeof(ICompServTestAccessorA), fakeAccessor);

            Assert.IsFalse(registered);
        }

        #endregion

        #region CompositeRegistryTests: Lineage

        [TestMethod]
        [TestCategory("Lineage")]
        public void HandlerIsBoundToCorrectAccessor()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            ICompServTestHandler handlerA = facade.GetRegistryHandler(typeof(SomeTypeA));
            ICompServTestAccessor accessorA = facade.GetRegistryAccessor(typeof(ICompServTestAccessorA));

            Assert.IsTrue(handlerA.GetType()
                .GetInterfaces()
                .Any(i => i.IsGenericType &&
                          i.GetGenericTypeDefinition() == typeof(ICompositeRegistryHandlerFor<>) &&
                          i.GenericTypeArguments[0] == accessorA.GetType().GetInterfaces().First()));
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void IsAccessorForHandlerByType_ForCorrectPair_ReturnsTrue()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();
            ICompServTestAccessor accessorA = facade.GetRegistryAccessor(typeof(ICompServTestAccessorA));

            bool result = facade.IsRegistryAccessorForHandler(typeof(SomeTypeA), accessorA);

            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void IsAccessorForHandlerByType_ForWrongPair_ReturnsFalse()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();
            ICompServTestAccessor accessorB = facade.GetRegistryAccessor(typeof(ICompServTestAccessorB));

            bool result = facade.IsRegistryAccessorForHandler(typeof(SomeTypeA), accessorB);

            Assert.IsFalse(result);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void IsAccessorAvailableForType_WhenAccessorExists_ReturnsTrue()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool result = facade.IsRegistryAccessorAvailableFor(typeof(SomeTypeA));

            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void IsAccessorAvailableForType_WhenNoAccessorExists_ReturnsFalse()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool result = facade.IsRegistryAccessorAvailableFor(typeof(Guid));

            Assert.IsFalse(result);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void IsAccessorForHandler_CorrectInstances_ReturnsTrueFor()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();
            ICompServTestHandler handlerA = facade.GetRegistryHandler(typeof(SomeTypeA));
            ICompServTestAccessor accessorA = facade.GetRegistryAccessor(typeof(ICompServTestAccessorA));

            bool result = facade.IsRegistryAccessorForHandler(handlerA, accessorA);

            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void IsAccessorForHandler_ForMismatchedInstances_ReturnsFalse()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();
            ICompServTestHandler handlerA = facade.GetRegistryHandler(typeof(SomeTypeA));
            ICompServTestAccessor accessorB = facade.GetRegistryAccessor(typeof(ICompServTestAccessorB));

            bool result = facade.IsRegistryAccessorForHandler(handlerA, accessorB);

            Assert.IsFalse(result);
        }
        [TestMethod]
        [TestCategory("Lineage")]
        public void IsAccessorAvailableForHandler_WhenAccessorExists_ReturnsTrue()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();
            ICompServTestHandler handlerB = facade.GetRegistryHandler(typeof(SomeTypeB));

            bool result = facade.IsRegistryAccessorAvailableFor(handlerB);

            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void IsAccessorAvailableForHandler_FalseWhenNoAccessorExists_Returns()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            FakeHandler fakeHandler = new();

            bool result = facade.IsRegistryAccessorAvailableFor(fakeHandler);

            Assert.IsFalse(result);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void GetAccessorForType_ReturnsCorrectAccessor()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            ICompServTestAccessor accessorA = facade.GetRegistryAccessorFor(typeof(SomeTypeA));

            Assert.AreEqual("AccessorA", accessorA.Label);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void GetAccessorForType_WhenNoAccessorExists_Throw()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            KeyNotFoundException ex = Assert.ThrowsException<KeyNotFoundException>(
                () => facade.GetRegistryAccessorFor(typeof(Guid))
            );

            StringAssert.Contains(ex.Message, "Guid");
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void GetAccessorsForType_ReturnsListOfAccessors()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            IReadOnlyList<ICompServTestAccessor> accessors = facade.GetRegistryAccessorsFor(typeof(SomeTypeB));

            Assert.AreEqual(1, accessors.Count);
            Assert.AreEqual("AccessorB", accessors[0].Label);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void GetAccessorsForType_WhenNoAccessorExists_Throw()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            KeyNotFoundException ex = Assert.ThrowsException<KeyNotFoundException>(
                () => facade.GetRegistryAccessorsFor(typeof(Guid))
            );

            StringAssert.Contains(ex.Message, "Guid");
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void GetAccessorForHandler_ReturnsCorrectAccessor()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();
            ICompServTestHandler handlerA = facade.GetRegistryHandler(typeof(SomeTypeA));

            ICompServTestAccessor accessorA = facade.GetRegistryAccessorFor(handlerA);

            Assert.AreEqual("AccessorA", accessorA.Label);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void GetAccessorForHandler_WhenNoAccessorExists_Throw()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            FakeHandler fakeHandler = new();

            KeyNotFoundException ex = Assert.ThrowsException<KeyNotFoundException>(
                () => facade.GetRegistryAccessorFor(fakeHandler)
            );

            StringAssert.Contains(ex.Message, "FakeHandler");
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void GetAccessorsForHandler_ReturnsListOfAccessors()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();
            ICompServTestHandler handlerB = facade.GetRegistryHandler(typeof(SomeTypeB));

            IReadOnlyList<ICompServTestAccessor> accessors = facade.GetRegistryAccessorsFor(handlerB);

            Assert.AreEqual(1, accessors.Count);
            Assert.AreEqual("AccessorB", accessors[0].Label);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void GetAccessorsForHandler_WhenNoAccessorExists_Throw()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            FakeHandler fakeHandler = new();

            KeyNotFoundException ex = Assert.ThrowsException<KeyNotFoundException>(
                () => facade.GetRegistryAccessorsFor(fakeHandler)
            );

            StringAssert.Contains(ex.Message, "FakeHandler");
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void TryGetAccessorForType_WhenLineageExists_ReturnsTrue()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool found = facade.TryGetRegistryAccessorFor(typeof(SomeTypeA), out ICompServTestAccessor? accessor);

            Assert.IsTrue(found);
            Assert.IsNotNull(accessor);
            Assert.AreEqual("AccessorA", accessor!.Label);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void GetAccessorForType_WhenNoLineageExists_Throw()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            KeyNotFoundException ex = Assert.ThrowsException<KeyNotFoundException>(
                () => facade.GetRegistryAccessorFor(typeof(Guid))
            );

            StringAssert.Contains(ex.Message, "Guid");
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void TryGetAccessorsForType_WhenLineageExists_ReturnsList()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool found = facade.TryGetRegistryAccessorsFor(typeof(SomeTypeB), out IReadOnlyList<ICompServTestAccessor>? accessors);

            Assert.IsTrue(found);
            Assert.IsNotNull(accessors);
            Assert.AreEqual(1, accessors.Count);
            Assert.AreEqual("AccessorB", accessors[0].Label);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void TryGetAccessorsForType_WhenNoLineageExists_ReturnsFalseAndNull()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            bool found = facade.TryGetRegistryAccessorsFor(typeof(Guid), out IReadOnlyList<ICompServTestAccessor>? accessors);

            Assert.IsFalse(found);
            Assert.IsNull(accessors);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void TryGetAccessorForHandler_WhenLineageExists_ReturnsTrue()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();
            ICompServTestHandler handlerA = facade.GetRegistryHandler(typeof(SomeTypeA));

            bool found = facade.TryGetRegistryAccessorFor(handlerA, out ICompServTestAccessor? accessor);

            Assert.IsTrue(found);
            Assert.IsNotNull(accessor);
            Assert.AreEqual("AccessorA", accessor!.Label);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void TryGetAccessorForHandler_WhenNoLineageExists_ReturnsFalse()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            FakeHandler fakeHandler = new();

            bool result = facade.TryGetRegistryAccessorFor(fakeHandler, out ICompServTestAccessor? accessor);

            Assert.IsFalse(result, "Expected TryGetRegistryAccessorFor to return false when no lineage exists.");
            Assert.IsNull(accessor, "Expected accessor to be null when no lineage exists.");
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void TryGetAccessorsForHandler_WhenLineageExists_ReturnsList()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();
            ICompServTestHandler handlerB = facade.GetRegistryHandler(typeof(SomeTypeB));

            bool found = facade.TryGetRegistryAccessorsFor(handlerB, out IReadOnlyList<ICompServTestAccessor>? accessors);

            Assert.IsTrue(found);
            Assert.IsNotNull(accessors);
            Assert.AreEqual(1, accessors.Count);
            Assert.AreEqual("AccessorB", accessors[0].Label);
        }

        [TestMethod]
        [TestCategory("Lineage")]
        public void TryGetAccessorsForHandler_WhenNoLineageExists_ReturnsFalseAndNull()
        {
            CompServTest facade = this.serviceProvider!.GetRequiredService<CompServTest>();

            FakeHandler fakeHandler = new();
            bool found = facade.TryGetRegistryAccessorsFor(fakeHandler, out IReadOnlyList<ICompServTestAccessor>? accessors);

            Assert.IsFalse(found);
            Assert.IsNull(accessors);
        }

        #endregion
    }
}
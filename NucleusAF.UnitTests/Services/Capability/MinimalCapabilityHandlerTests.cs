using Microsoft.Extensions.Logging.Abstractions;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Services.Capabilities;
using NucleusAF.Services.Capabilities.Accessors;
using NucleusAF.Services.Capabilities.Handlers;

namespace NucleusAF.UnitTests.Services.Capability
{
    [TestClass]
    public class MinimalCapabilityHandlerTests
    {
        internal enum CapabilitySlots
        {
            Permissions = 0
        }

        internal sealed class TestResourceKey(string name) : IResourceKey
        {
            public string Name { get; } = name;
            public override string ToString() => this.Name;

            public bool Matches(object? other)
            {
                if (other is TestResourceKey key)
                    return string.Equals(this.Name, key.Name, StringComparison.Ordinal);

                return other is string str ? string.Equals(this.Name, str, StringComparison.Ordinal) : false;
            }

            public override bool Equals(object? obj)
            {
                return obj is TestResourceKey key && string.Equals(this.Name, key.Name, StringComparison.Ordinal);
            }
            public override int GetHashCode() => this.Name.GetHashCode(StringComparison.Ordinal);
        }

        private CapabilityService capabilityService = null!;
        private MinimalCapabilityHandler capabilityHandler = null!;
        private MinimalCapabilityAccessor capabilityAccessor = null!;

        [TestInitialize]
        public void Setup()
        {
            this.capabilityHandler = new MinimalCapabilityHandler(new NullLogger<ICapabilityHandler>());
            this.capabilityAccessor = new MinimalCapabilityAccessor(new NullLogger<ICapabilityAccessor>());

            this.capabilityService = new CapabilityService(
                [this.capabilityAccessor],
                [this.capabilityHandler],
                new NullLogger<ICapabilityService>());
        }

        [TestCleanup]
        public void Cleanup()
        {
            (this.capabilityService as IDisposable)?.Dispose();
        }

        [TestMethod]
        public void ObligatoryTest() { }
    }
}
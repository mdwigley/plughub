using Microsoft.Extensions.Logging.Abstractions;
using NucleusAF.Attributes;
using NucleusAF.Models;
using NucleusAF.Models.Descriptors;
using NucleusAF.Services.Modules;

namespace NucleusAF.UnitTests.Services.Modules
{
    [DescriptorProvider("GetForwardTestDescriptors", sortContext: DescriptorSortContext.Forward)]
    internal interface IProviderForwardTest
    {
        public IEnumerable<TestDescriptor> GetForwardTestDescriptors();
    }
    internal class TestForward(IEnumerable<TestDescriptor> descriptors) : IProviderForwardTest
    {
        private readonly IEnumerable<TestDescriptor> descriptors = descriptors;

        public IEnumerable<TestDescriptor> GetForwardTestDescriptors() => this.descriptors;
    }

    [DescriptorProvider("GetReverseTestDescriptors")]
    internal interface IProviderReverseTest
    {
        public IEnumerable<TestDescriptor> GetReverseTestDescriptors();
    }
    internal class TestReverse(IEnumerable<TestDescriptor> descriptors) : IProviderReverseTest
    {
        private readonly IEnumerable<TestDescriptor> descriptors = descriptors;

        public IEnumerable<TestDescriptor> GetReverseTestDescriptors() => this.descriptors;
    }

    internal record TestDescriptor(
        Guid ModuleId,
        Guid DescriptorId,
        string Version,
        IEnumerable<DescriptorReference>? LoadBefore = null,
        IEnumerable<DescriptorReference>? LoadAfter = null,
        IEnumerable<DescriptorReference>? DependsOn = null,
        IEnumerable<DescriptorReference>? ConflictsWith = null)
            : Descriptor(ModuleId, DescriptorId, Version, LoadBefore, LoadAfter, DependsOn, ConflictsWith);

    [TestClass]
    public sealed class ModuleResolverTests
    {
        private readonly MSTestHelpers msTestHelpers = new();
        private ModuleResolver? moduleResolver;

        [TestInitialize]
        public void Setup()
        {
            this.moduleResolver = new ModuleResolver(new NullLogger<ModuleResolver>());
        }

        [TestCleanup]
        public void Cleanup()
        {
            Thread.Sleep(100);
            Serilog.Log.CloseAndFlush();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            this.msTestHelpers!.Dispose();
        }

        #region ModuleResolverTests: Constructor

        [TestMethod]
        [TestCategory("Constructor")]
        public void Constructor_ValidLogger_CreatesInstance()
        {
            // Arrange & Act
            ModuleResolver resolver = new(new NullLogger<ModuleResolver>());

            // Assert
            Assert.IsInstanceOfType<ModuleResolver>(resolver, "ModuleResolver should be created successfully");
        }

        [TestMethod]
        [TestCategory("Constructor")]
        public void Constructor_NullLogger_Throw()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new ModuleResolver(null!));
        }

        #endregion

        #region ModuleResolverTests: Basic Resolution

        [TestMethod]
        [TestCategory("ResolveDescriptors")]
        public void ResolveDescriptors_EmptyCollectionReturnsEmpty()
        {
            // Arrange
            List<TestDescriptor> descriptors = [];

            // Act
            IEnumerable<TestDescriptor> result = this.moduleResolver!.ResolveDescriptors(descriptors);

            // Assert
            Assert.AreEqual(0, result.Count(), "Should return empty collection for empty input");
        }

        [TestMethod]
        [TestCategory("ResolveDescriptors")]
        public void ResolveDescriptors_SingleDescriptorReturnsSingle()
        {
            // Arrange
            List<TestDescriptor> descriptors =
            [
                CreateTestDescriptor("Module1", "1.0.0")
            ];

            // Act
            IEnumerable<TestDescriptor> result = this.moduleResolver!.ResolveDescriptors(descriptors);

            // Assert
            Assert.AreEqual(1, result.Count(), "Should return single descriptor");
            Assert.AreEqual(descriptors[0].ModuleId, result.First().ModuleId, "Should return the correct descriptor");
        }

        [TestMethod]
        [TestCategory("ResolveDescriptors")]
        public void ResolveDescriptors_MultipleIndependentDescriptorsReturnsAll()
        {
            // Arrange
            List<TestDescriptor> descriptors =
            [
                CreateTestDescriptor("Module1", "1.0.0"),
                CreateTestDescriptor("Module2", "1.0.0"),
                CreateTestDescriptor("Module3", "1.0.0")
            ];

            // Act
            IEnumerable<TestDescriptor> result = this.moduleResolver!.ResolveDescriptors(descriptors);

            // Assert
            Assert.AreEqual(3, result.Count(), "Should return all independent descriptors");

            HashSet<Guid> returnedIds = [.. result.Select(d => d.ModuleId)];

            Assert.IsTrue(returnedIds.Contains(descriptors[0].ModuleId), "Should contain Module1");
            Assert.IsTrue(returnedIds.Contains(descriptors[1].ModuleId), "Should contain Module2");
            Assert.IsTrue(returnedIds.Contains(descriptors[2].ModuleId), "Should contain Module3");
        }

        [TestMethod]
        [TestCategory("ResolveDescriptors")]
        public void ResolveDescriptors_DuplicateDescriptorIDFiltersOutDuplicates()
        {
            // Arrange
            Guid sharedDescriptorId = Guid.NewGuid();
            Guid module1Id = Guid.NewGuid();
            Guid module2Id = Guid.NewGuid();
            string version = "1.0.0";

            List<TestDescriptor> descriptors =
            [
                CreateTestDescriptor(module1Id, sharedDescriptorId, version),
                CreateTestDescriptor(module2Id, sharedDescriptorId, version)
            ];

            // Act
            DescriptorResolutionContext<TestDescriptor> context = this.moduleResolver!.ResolveContext(descriptors);

            // Assert
            Assert.AreEqual(2, descriptors.Count, "Original input contains two descriptors");
            Assert.AreEqual(1, context.DuplicateIdDisabled.Count, "One duplicate should be tracked as disabled");
            Assert.AreEqual(1, context.IdToDescriptor.Count, "One descriptor with unique DescriptorId should remain");

            IEnumerable<TestDescriptor> result = this.moduleResolver.ResolveDescriptors(descriptors);
            Assert.AreEqual(1, result.Count(), "Duplicate descriptors should be filtered out from final resolution");
        }

        #endregion

        #region ModuleResolverTests: Dependency Resolution

        [TestMethod]
        [TestCategory("ResolveDescriptors")]
        public void ResolveDescriptors_MissingDependencyFiltersOutInvalidDescriptor()
        {
            // Arrange
            Guid module1Id = Guid.NewGuid();
            Guid module2Id = Guid.NewGuid();
            Guid missingId = Guid.NewGuid();
            Guid descriptor1Id = Guid.NewGuid();
            Guid descriptor2Id = Guid.NewGuid();
            Guid missingDescriptorId = Guid.NewGuid();

            List<TestDescriptor> descriptors =
            [
                CreateTestDescriptor(module1Id, descriptor1Id, "1.0.0"),
                CreateTestDescriptor(module2Id, descriptor2Id, "1.0.0", dependsOn: [new DescriptorReference(missingId, missingDescriptorId, "1.0.0", "2.0.0")])
            ];

            // Act
            IEnumerable<TestDescriptor> result = this.moduleResolver!.ResolveDescriptors(descriptors);

            // Assert
            Assert.AreEqual(1, result.Count(), "Should return only valid descriptors");
            Assert.AreEqual(module1Id, result.First().ModuleId, "Should return only Module1");
        }

        [TestMethod]
        [TestCategory("ResolveDescriptors")]
        public void ResolveDescriptors_VersionMismatchFiltersOutIncompatibleDescriptor()
        {
            // Arrange
            Guid module1Id = Guid.NewGuid();
            Guid module2Id = Guid.NewGuid();
            Guid descriptor1Id = Guid.NewGuid();
            Guid descriptor2Id = Guid.NewGuid();

            List<TestDescriptor> descriptors =
            [
                CreateTestDescriptor(module1Id, descriptor1Id, "3.0.0"),
                CreateTestDescriptor(module2Id, descriptor2Id, "1.0.0", dependsOn: [new DescriptorReference(module1Id, descriptor1Id, "1.0.0", "2.0.0")])
            ];

            // Act
            IEnumerable<TestDescriptor> result = this.moduleResolver!.ResolveDescriptors(descriptors);

            // Assert
            Assert.AreEqual(1, result.Count(), "Should return only compatible descriptors");
            Assert.AreEqual(descriptors[0].ModuleId, result.First().ModuleId, "Should return Module1 (even though incompatible)");
        }

        #endregion

        #region ModuleResolverTests: Conflict Resolution

        [TestMethod]
        [TestCategory("ResolveDescriptors")]
        public void ResolveDescriptors_ConflictingDescriptorsFiltersOutConflicted()
        {
            // Arrange
            Guid module1Id = Guid.NewGuid();
            Guid module2Id = Guid.NewGuid();
            Guid descriptor1Id = Guid.NewGuid();
            Guid descriptor2Id = Guid.NewGuid();

            List<TestDescriptor> descriptors =
            [
                CreateTestDescriptor(module1Id, descriptor1Id, "1.0.0"),
                CreateTestDescriptor(module2Id, descriptor2Id, "1.0.0", conflictsWith: [new DescriptorReference(module1Id, descriptor1Id, "1.0.0", "2.0.0")])
            ];

            // Act
            IEnumerable<TestDescriptor> result = this.moduleResolver!.ResolveDescriptors(descriptors);

            // Assert
            Assert.AreEqual(1, result.Count(), "Should filter out conflicting descriptors");
            Assert.AreEqual(module1Id, result.First().ModuleId, "Should keep Module1 and remove conflicting Module2");
        }

        #endregion

        #region ModuleResolverTests: Load Ordering

        [TestMethod]
        [TestCategory("ResolveDescriptors")]
        public void ResolveDescriptors_LoadBeforeConstraintRespectsOrdering()
        {
            // Arrange
            Guid module1Id = Guid.NewGuid();
            Guid module2Id = Guid.NewGuid();
            Guid descriptor1Id = Guid.NewGuid();
            Guid descriptor2Id = Guid.NewGuid();

            List<TestDescriptor> descriptors =
            [
                CreateTestDescriptor(module2Id, descriptor2Id, "1.0.0", loadBefore: [new DescriptorReference(module1Id, descriptor1Id, "1.0.0", "2.0.0")]),
                CreateTestDescriptor(module1Id, descriptor1Id, "1.0.0")
            ];

            // Act
            IList<TestDescriptor> result = [.. this.moduleResolver!.ResolveDescriptors(descriptors)];

            // Assert
            Assert.AreEqual(2, result.Count, "Should return both descriptors");
            Assert.AreEqual(module2Id, result[0].ModuleId, "Module2 should load first (LoadBefore)");
            Assert.AreEqual(module1Id, result[1].ModuleId, "Module1 should load second");
        }

        [TestMethod]
        [TestCategory("ResolveDescriptors")]
        public void ResolveDescriptors_LoadAfterConstraintRespectsOrdering()
        {
            // Arrange
            Guid module1Id = Guid.NewGuid();
            Guid module2Id = Guid.NewGuid();
            Guid descriptor1Id = Guid.NewGuid();
            Guid descriptor2Id = Guid.NewGuid();

            List<TestDescriptor> descriptors =
            [
                CreateTestDescriptor(module1Id, descriptor1Id, "1.0.0"),
                CreateTestDescriptor(module2Id, descriptor2Id, "1.0.0", loadAfter: [new DescriptorReference(module1Id, descriptor1Id, "1.0.0", "2.0.0")])
            ];

            // Act
            IList<TestDescriptor> result = [.. this.moduleResolver!.ResolveDescriptors(descriptors)];

            // Assert
            Assert.AreEqual(2, result.Count, "Should return both descriptors");
            Assert.AreEqual(module1Id, result[0].ModuleId, "Module1 should load first");
            Assert.AreEqual(module2Id, result[1].ModuleId, "Module2 should load second (LoadAfter)");
        }

        #endregion

        #region ModuleResolverTests: SortContext

        [TestMethod]
        [TestCategory("ResolveAndOrder")]
        public void ResolveAndOrder_ForwardContext_ReturnsInForwardOrder()
        {
            // Arrange
            IReadOnlyList<TestDescriptor> descriptors =
            [
                CreateTestDescriptor(Guid.NewGuid(), Guid.NewGuid(), "1.0.0"),
                CreateTestDescriptor(Guid.NewGuid(), Guid.NewGuid(), "1.0.0")
            ];

            TestForward module = new(descriptors);

            // Act
            IReadOnlyList<TestDescriptor> result =
                this.moduleResolver!.ResolveAndOrder<IProviderForwardTest, TestDescriptor>([module]);

            // Assert
            CollectionAssert.AreEqual(descriptors.ToList(), result.ToList(), "Forward context should preserve order");
        }

        [TestMethod]
        [TestCategory("ResolveAndOrder")]
        public void ResolveAndOrder_ReverseContext_ReturnsInReverseOrder()
        {
            IEnumerable<TestDescriptor> descriptors =
            [
                CreateTestDescriptor(Guid.NewGuid(), Guid.NewGuid(), "1.0.0"),
                CreateTestDescriptor(Guid.NewGuid(), Guid.NewGuid(), "1.0.0")
            ];

            TestReverse module = new(descriptors);

            IReadOnlyList<TestDescriptor> result = this.moduleResolver!.ResolveAndOrder<IProviderReverseTest, TestDescriptor>([module]);

            CollectionAssert.AreEqual(descriptors.Reverse().ToList(), result.ToList(), "Reverse context should invert order");
        }

        #endregion

        #region ModuleResolverTests: Complex Scenarios

        [TestMethod]
        [TestCategory("ResolveDescriptors")]
        public void ResolveDescriptors_ValidDependencyIncludesBothModules()
        {
            // Arrange
            Guid module1Id = Guid.NewGuid();
            Guid module2Id = Guid.NewGuid();
            Guid descriptor1Id = Guid.NewGuid();
            Guid descriptor2Id = Guid.NewGuid();

            List<TestDescriptor> descriptors =
            [
                CreateTestDescriptor(module1Id, descriptor1Id, "1.0.0"),
                CreateTestDescriptor(module2Id, descriptor2Id, "1.0.0", dependsOn: [new DescriptorReference(module1Id, descriptor1Id, "1.0.0", "2.0.0")])
            ];

            // Act
            IEnumerable<TestDescriptor> result = this.moduleResolver!.ResolveDescriptors(descriptors);

            // Assert
            Assert.AreEqual(2, result.Count(), "Should include both modules when dependency is satisfied");

            HashSet<Guid> returnedIds = [.. result.Select(d => d.ModuleId)];

            Assert.IsTrue(returnedIds.Contains(module1Id), "Should contain Module1");
            Assert.IsTrue(returnedIds.Contains(module2Id), "Should contain Module2");
        }

        [TestMethod]
        [TestCategory("ResolveDescriptors")]
        public void ResolveDescriptors_MissingDependencyFiltersOutDependentModule()
        {
            // Arrange
            Guid module1Id = Guid.NewGuid();
            Guid module2Id = Guid.NewGuid();
            Guid missingId = Guid.NewGuid();
            Guid descriptor1Id = Guid.NewGuid();
            Guid descriptor2Id = Guid.NewGuid();
            Guid missingDescriptorId = Guid.NewGuid();

            List<TestDescriptor> descriptors =
            [
                CreateTestDescriptor(module1Id, descriptor1Id, "1.0.0"),
                CreateTestDescriptor(module2Id, descriptor2Id, "1.0.0", dependsOn: [new DescriptorReference(missingId, missingDescriptorId, "1.0.0", "2.0.0")])
            ];

            // Act
            IEnumerable<TestDescriptor> result = this.moduleResolver!.ResolveDescriptors(descriptors);

            // Assert
            Assert.AreEqual(1, result.Count(), "Should filter out Module2 due to missing dependency");
            Assert.AreEqual(module1Id, result.First().ModuleId, "Should only include Module1");
        }

        [TestMethod]
        [TestCategory("ResolveDescriptors")]
        public void ResolveDescriptors_DependencyWithoutLoadOrderDoesNotAffectSequence()
        {
            // Arrange
            Guid module1Id = Guid.NewGuid();
            Guid module2Id = Guid.NewGuid();
            Guid descriptor1Id = Guid.NewGuid();
            Guid descriptor2Id = Guid.NewGuid();

            List<TestDescriptor> descriptors =
            [
                CreateTestDescriptor(module2Id, descriptor2Id, "1.0.0", dependsOn: [new DescriptorReference(module1Id, descriptor1Id, "1.0.0", "2.0.0")]),
                CreateTestDescriptor(module1Id, descriptor1Id, "1.0.0")
            ];

            // Act
            IList<TestDescriptor> result = [.. this.moduleResolver!.ResolveDescriptors(descriptors)];

            // Assert
            Assert.AreEqual(2, result.Count, "Should return both descriptors");

            HashSet<Guid> returnedIds = [.. result.Select(d => d.ModuleId)];
            Assert.IsTrue(returnedIds.Contains(module1Id), "Should contain Module1");
            Assert.IsTrue(returnedIds.Contains(module2Id), "Should contain Module2");
        }

        [TestMethod]
        [TestCategory("ResolveDescriptors")]
        public void ResolveDescriptors_DependencyAndLoadOrderBothConstraintsApplied()
        {
            // Arrange
            Guid module1Id = Guid.NewGuid();
            Guid module2Id = Guid.NewGuid();
            Guid module3Id = Guid.NewGuid();
            Guid descriptor1Id = Guid.NewGuid();
            Guid descriptor2Id = Guid.NewGuid();
            Guid descriptor3Id = Guid.NewGuid();

            List<TestDescriptor> descriptors =
            [
                CreateTestDescriptor(module1Id, descriptor1Id, "1.0.0"),
                CreateTestDescriptor(module2Id, descriptor2Id, "1.0.0", dependsOn: [new DescriptorReference(module1Id, descriptor1Id, "1.0.0", "2.0.0")],
                    loadAfter: [new DescriptorReference(module1Id, descriptor1Id, "1.0.0", "2.0.0")]),
                CreateTestDescriptor(module3Id, descriptor3Id, "1.0.0")
            ];

            // Act
            List<TestDescriptor> result = [.. this.moduleResolver!.ResolveDescriptors(descriptors)];

            // Assert
            Assert.AreEqual(3, result.Count, "Should return all descriptors (dependency satisfied)");

            int index1 = result.ToList().FindIndex(d => d.ModuleId == module1Id);
            int index2 = result.ToList().FindIndex(d => d.ModuleId == module2Id);

            Assert.IsTrue(index1 < index2, "Module1 should load before Module2 (LoadAfter constraint)");
        }


        #endregion

        #region ModuleResolverTests: Error Handling

        [TestMethod]
        [TestCategory("ErrorHandling")]
        public void ResolveDescriptors_DeterministicOrderingReturnsSameOrderForSameInput()
        {
            // Arrange
            List<TestDescriptor> descriptors =
            [
                CreateTestDescriptor("Module3", "1.0.0"),
                CreateTestDescriptor("Module1", "1.0.0"),
                CreateTestDescriptor("Module2", "1.0.0")
            ];

            // Act
            IList<TestDescriptor> result1 = [.. this.moduleResolver!.ResolveDescriptors(descriptors)];
            IList<TestDescriptor> result2 = [.. this.moduleResolver!.ResolveDescriptors(descriptors)];

            // Assert
            Assert.AreEqual(result1.Count, result2.Count, "Results should have same count");
            for (int i = 0; i < result1.Count; i++)
            {
                Assert.AreEqual(result1[i].ModuleId, result2[i].ModuleId,
                    $"Module at index {i} should be the same in both results for deterministic ordering");
            }
        }

        #endregion


        private static TestDescriptor CreateTestDescriptor(
            string moduleName,
            string version,
            IEnumerable<DescriptorReference>? dependsOn = null,
            IEnumerable<DescriptorReference>? conflictsWith = null,
            IEnumerable<DescriptorReference>? loadBefore = null,
            IEnumerable<DescriptorReference>? loadAfter = null)
        {
            return CreateTestDescriptor(
                Guid.Parse(moduleName.GetHashCode().ToString("X").PadLeft(32, '0')[..8] + "-0000-0000-0000-000000000000"),
                Guid.NewGuid(),
                version,
                dependsOn,
                conflictsWith,
                loadBefore,
                loadAfter);
        }

        private static TestDescriptor CreateTestDescriptor(
            Guid moduleId,
            Guid descriptorId,
            string version,
            IEnumerable<DescriptorReference>? dependsOn = null,
            IEnumerable<DescriptorReference>? conflictsWith = null,
            IEnumerable<DescriptorReference>? loadBefore = null,
            IEnumerable<DescriptorReference>? loadAfter = null)
        {
            return new TestDescriptor(
                ModuleId: moduleId,
                DescriptorId: descriptorId,
                Version: version,
                LoadBefore: loadBefore,
                LoadAfter: loadAfter,
                DependsOn: dependsOn,
                ConflictsWith: conflictsWith);
        }
    }
}

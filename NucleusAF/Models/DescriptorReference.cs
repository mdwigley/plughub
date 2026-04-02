namespace NucleusAF.Models
{
    /// <summary>
    /// Represents a reference to another descriptor, describing constraints or requirements for relationships such as dependencies, conflicts, or optional features.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DescriptorReference"/> class with details
    /// identifying the target module descriptor, its version range, and other optional constraints or metadata.
    /// </remarks>
    /// <param name="moduleId">The unique identifier (GUID) of the referenced module.</param>
    /// <param name="descriptorId">The unique identifier (GUID) of the referenced descriptor.</param>
    /// <param name="minVersion">Minimum acceptable version (inclusive) for the descriptor.</param>
    /// <param name="maxVersion">Maximum acceptable version (inclusive) for the descriptor.</param>
    public class DescriptorReference(Guid moduleId, Guid descriptorId, string minVersion, string maxVersion)
    {
        /// <summary>
        /// Unique identifier (GUID) for the referenced module.
        /// </summary>
        public Guid ModuleId { get; set; } = moduleId;

        /// <summary>
        /// Unique identifier (GUID) for the referenced descriptor.
        /// </summary>
        public Guid DescriptorId { get; set; } = descriptorId;

        /// <summary>
        /// Minimum acceptable version (inclusive) for the referenced descriptor.
        /// </summary>
        public string MinVersion { get; set; } = minVersion;

        /// <summary>
        /// Maximum acceptable version (inclusive) for the referenced descriptor.
        /// </summary>
        public string MaxVersion { get; set; } = maxVersion;

        /// <summary>
        /// Compares two version strings in the format "major.minor.patch".
        /// Returns -1 if <paramref name="v1"/> is less than <paramref name="v2"/>,
        /// 1 if <paramref name="v1"/> is greater than <paramref name="v2"/>, or 0 if equal.
        /// </summary>
        /// <param name="v1">The first version string.</param>
        /// <param name="v2">The second version string.</param>
        /// <returns>An integer indicating the comparison result.</returns>
        private static int CompareVersions(string v1, string v2)
        {
            if (v1 == null && v2 == null)
            {
                return 0;
            }

            if (v1 == null)
            {
                return -1;
            }

            if (v2 == null)
            {
                return 1;
            }

            int[] parts1 = [.. v1.Split('.').Select(int.Parse)];
            int[] parts2 = [.. v2.Split('.').Select(int.Parse)];
            int maxLength = Math.Max(parts1.Length, parts2.Length);

            for (int i = 0; i < maxLength; i++)
            {
                int p1 = i < parts1.Length ? parts1[i] : 0;
                int p2 = i < parts2.Length ? parts2[i] : 0;
                if (p1 != p2)
                {
                    return p1.CompareTo(p2);
                }
            }
            return 0;
        }

        /// <summary>
        /// Checks if the given descriptorId and version are within this reference's version range.
        /// </summary>
        /// <param name="moduleId">The unique identifier (GUID) of the module to check.</param>
        /// <param name="descriptorId">The unique identifier (GUID) of the descriptor to check.</param>
        /// <param name="version">The version of the descriptor to check.</param>
        /// <returns>
        /// <c>true</c> if the descriptorId matches and the version is within the range; otherwise, <c>false</c>.
        /// </returns>
        public bool Matches(Guid moduleId, Guid descriptorId, string version)
        {
            return moduleId != this.ModuleId || descriptorId != this.DescriptorId
                ? false
                : CompareVersions(version, this.MinVersion) >= 0 &&
                   CompareVersions(version, this.MaxVersion) <= 0;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// Two <see cref="DescriptorReference"/> instances are equal if their
        /// <see cref="DescriptorId"/>, <see cref="MinVersion"/>, and <see cref="MaxVersion"/> are all equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// <c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object? obj)
        {
            return obj is DescriptorReference other &&
                   this.ModuleId == other.ModuleId &&
                   this.DescriptorId == other.DescriptorId &&
                   CompareVersions(this.MinVersion, other.MinVersion) == 0 &&
                   CompareVersions(this.MaxVersion, other.MaxVersion) == 0;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() =>
            HashCode.Combine(this.ModuleId, this.DescriptorId, this.MinVersion, this.MaxVersion);

        /// <summary>
        /// Determines whether two <see cref="DescriptorReference"/> instances are equal.
        /// </summary>
        /// <param name="a">The first instance to compare.</param>
        /// <param name="b">The second instance to compare.</param>
        /// <returns>
        /// <c>true</c> if both instances are equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(DescriptorReference a, DescriptorReference b)
            => a?.Equals(b) ?? b is null;

        /// <summary>
        /// Determines whether two <see cref="DescriptorReference"/> instances are not equal.
        /// </summary>
        /// <param name="a">The first instance to compare.</param>
        /// <param name="b">The second instance to compare.</param>
        /// <returns>
        /// <c>true</c> if the instances are not equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(DescriptorReference a, DescriptorReference b)
            => !(a == b);

        /// <summary>
        /// Returns <c>true</c> if the descriptor's version is less than the minimum version of the range.
        /// </summary>
        /// <param name="range">The descriptor version range to compare against.</param>
        /// <param name="descriptor">A tuple containing the module's id, descriptor's id, and version.</param>
        /// <returns>
        /// <c>true</c> if the descriptor's version is less than the minimum version; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator <(DescriptorReference range, (Guid moduleId, Guid descriptorId, string version) descriptor)
            => range.ModuleId == descriptor.moduleId &&
               range.DescriptorId == descriptor.descriptorId &&
               CompareVersions(descriptor.version, range.MinVersion) < 0;

        /// <summary>
        /// Returns <c>true</c> if the descriptor's version is greater than the maximum version of the range.
        /// </summary>
        /// <param name="range">The descriptor version range to compare against.</param>
        /// <param name="descriptor">A tuple containing the module's id, descriptor's id, and version.</param>
        /// <returns>
        /// <c>true</c> if the descriptors's version is greater than the maximum version; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator >(DescriptorReference range, (Guid moduleId, Guid descriptorId, string version) descriptor)
            => range.ModuleId == descriptor.moduleId &&
               range.DescriptorId == descriptor.descriptorId &&
               CompareVersions(descriptor.version, range.MaxVersion) > 0;
    }
}

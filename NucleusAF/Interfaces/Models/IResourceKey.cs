namespace NucleusAF.Interfaces.Models
{
    public interface IResourceKey
    {
        bool Matches(object? other);
    }
}
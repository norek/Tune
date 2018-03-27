namespace Tune.Core.Services
{
    public interface IAssemblyPathsRepository
    {
        string GetAssemblyPathBy(string name, DiagnosticAssembyPlatform platform);
    }
}

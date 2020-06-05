using System.Threading.Tasks;
using NuGetPe;
using PackageExplorerViewModel;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageViewModelFactory
    {
        Task<PackageViewModel> CreateViewModel(IPackage package, string packagePath, string packageSource);
        PackageChooserViewModel CreatePackageChooserViewModel(NuGet.Configuration.PackageSource? fixedPackageSource);
        PluginManagerViewModel CreatePluginManagerViewModel();
    }
}

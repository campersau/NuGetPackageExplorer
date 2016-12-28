using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet;

namespace PackageExplorerViewModel.Rules
{
    [Export(typeof(IPackageRule))]
    internal class MissingContentOrDependencyRule : IPackageRule
    {
        #region IPackageRule Members

        public IEnumerable<PackageIssue> Validate(IPackage package)
        {
            if (!HasContentOrDependency(package))
            {
                yield return new PackageIssue(
                    "Package has no content or dependency.",
                    "The package does not contain any files or dependencies or framework assembly references to be a valid package.",
                    "Add files or package dependencies or framework assembly references.",
                    PackageIssueLevel.Error
                    );
            }
        }

        #endregion

        private static bool HasContentOrDependency(IPackage package)
        {
            return package.GetFiles().Any() || 
                   package.DependencySets.SelectMany(p => p.Dependencies).Any() || 
                   package.FrameworkAssemblies.Any();
        }
    }
}
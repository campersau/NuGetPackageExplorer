using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet;

namespace PackageExplorerViewModel
{
    public class EmptyPackage : IPackage
    {

        public EmptyPackage()
        {
            PackageAssemblyReferences = new List<PackageReferenceSet>();
        }

        #region IPackage Members

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get { return Enumerable.Empty<IPackageAssemblyReference>(); }
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return Enumerable.Empty<IPackageFile>();
        }

        public Stream GetStream()
        {
            return null;
        }

        public IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            yield break;
        }

        public void ExtractContents(IFileSystem fileSystem, string extractPath)
        {

        }

        public string Id
        {
            get { return "MyPackage"; }
        }

        public SemanticVersion Version
        {
            get { return new SemanticVersion("1.0.0"); }
        }

        public string Title
        {
            get { return string.Empty; }
        }

        public IEnumerable<string> Authors
        {
            get { yield return Environment.UserName; }
        }

        public IEnumerable<string> Owners
        {
            get { return Enumerable.Empty<string>(); }
        }

        public Uri IconUrl
        {
            get { return null; }
        }

        public Uri LicenseUrl
        {
            get { return null; }
        }

        public Uri ProjectUrl
        {
            get { return null; }
        }

        public bool RequireLicenseAcceptance
        {
            get { return false; }
        }

        public bool DevelopmentDependency
        {
            get { return false; }
        }

        public string Description
        {
            get { return "My package description."; }
        }

        public string Summary
        {
            get { return null; }
        }

        public string ReleaseNotes
        {
            get { return null; }
        }

        public string Copyright
        {
            get { return null; }
        }

        public string Language
        {
            get { return null; }
        }

        public string Tags
        {
            get { return null; }
        }

        public IEnumerable<PackageDependencySet> DependencySets
        {
            get { return Enumerable.Empty<PackageDependencySet>(); }
        }

        public Uri ReportAbuseUrl
        {
            get { return null; }
        }

        public int DownloadCount
        {
            get { return -1; }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies
        {
            get { return Enumerable.Empty<FrameworkAssemblyReference>(); }
        }

        public bool IsAbsoluteLatestVersion
        {
            get { return false; }
        }

        public bool IsLatestVersion
        {
            get { return false; }
        }

        public DateTimeOffset? Published
        {
            get { return DateTimeOffset.Now; }
        }

        public Version MinClientVersion
        {
            get { return null; }
        }

        public bool Listed
        {
            get { return false; }
        }

        public ICollection<PackageReferenceSet> PackageAssemblyReferences { get; private set; }

        #endregion
    }
}
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using NuGet;

namespace NuGetPackageExplorer.Types
{
    public class EmptyFolderFile : IPackageFile
    {
        private readonly string _path;
        private readonly string _effectivePath;
        private readonly FrameworkName _targetFramework;

        public EmptyFolderFile(string folderPath)
        {
            _path = System.IO.Path.Combine(folderPath, "_._");

            _targetFramework = VersionUtility.ParseFrameworkNameFromFilePath(Path, out _effectivePath);
        }

        public string EffectivePath { get { return _path; } }

        public string Path { get { return _effectivePath; } }

        public IEnumerable<FrameworkName> SupportedFrameworks
        {
            get
            {
                if (TargetFramework != null)
                {
                    yield return TargetFramework;
                }
                yield break;
            }
        }

        public FrameworkName TargetFramework { get { return _targetFramework; } }

        public Stream GetStream()
        {
            return Stream.Null;
        }
    }
}

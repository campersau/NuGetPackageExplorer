using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using NuGet;

namespace PackageExplorerViewModel.Rules
{
    [Export(typeof(IPackageRule))]
    internal class MisplacedScriptFileRule : IPackageRule
    {
        private const string ToolsFolder = "tools";
        private const string ScriptExtension = ".ps1";

        #region IPackageRule Members

        public IEnumerable<PackageIssue> Validate(IPackage package)
        {
            foreach (IPackageFile file in package.GetFiles())
            {
                string path = file.Path;
                if (!path.EndsWith(ScriptExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!path.StartsWith(ToolsFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    yield return CreatePackageIssueForMisplacedScript(path);
                }
                else
                {
                    string directory = Path.GetDirectoryName(path);
                    string name = Path.GetFileNameWithoutExtension(path);
                    if (!directory.Equals(ToolsFolder, StringComparison.OrdinalIgnoreCase) ||
                        !name.Equals("install", StringComparison.OrdinalIgnoreCase) &&
                        !name.Equals("uninstall", StringComparison.OrdinalIgnoreCase) &&
                        !name.Equals("init", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return CreatePackageIssueForUnrecognizedScripts(path);
                    }
                }
            }
        }

        #endregion

        private static PackageIssue CreatePackageIssueForMisplacedScript(string path)
        {
            return new PackageIssue(
                "PowerScript file outside tools folder",
                "The script file '" + path +
                "' is outside the 'tools' folder and hence will not be executed during installation of this package.",
                "Move it into the 'tools' folder.",
                PackageIssueLevel.Warning
                );
        }

        private static PackageIssue CreatePackageIssueForUnrecognizedScripts(string path)
        {
            return new PackageIssue(
                "Unrecognized PowerScript file",
                "The script file '" + path +
                "' is not recognized by NuGet and hence will not be executed during installation of this package.",
                "Rename it to install.ps1, uninstall.ps1 or init.ps1 and place it directly under 'tools'.",
                PackageIssueLevel.Warning
                );
        }
    }
}
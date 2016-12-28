﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using NuGet;

namespace PackageExplorerViewModel.Rules
{
    [Export(typeof(IPackageRule))]
    internal class MisplacedTransformFileRule : IPackageRule
    {
        private const string ContentFolder = "content";
        private const string CodeTransformExtension = ".pp";
        private const string ConfigTransformExtension = ".transform";

        #region IPackageRule Members

        public IEnumerable<PackageIssue> Validate(IPackage package)
        {
            foreach (IPackageFile file in package.GetFiles())
            {
                string path = file.Path;

                // if not a .transform file, ignore 
                if (!path.EndsWith(CodeTransformExtension, StringComparison.OrdinalIgnoreCase) &&
                    !path.EndsWith(ConfigTransformExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // if not inside 'content' folder, warn
                if (!path.StartsWith(ContentFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    yield return CreatePackageIssueForMisplacedContent(path);
                }
            }
        }

        #endregion

        private static PackageIssue CreatePackageIssueForMisplacedContent(string path)
        {
            return new PackageIssue(
                "Transform file outside content folder",
                "The transform file '" + path +
                "' is outside the 'content' folder and hence will not be transformed during installation of this package.",
                "Move it into the 'content' folder.",
                PackageIssueLevel.Warning);
        }
    }
}
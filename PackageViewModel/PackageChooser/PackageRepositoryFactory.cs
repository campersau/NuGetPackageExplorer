using System;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v2;
using System.Collections.Generic;

namespace PackageExplorerViewModel
{
    public static class PackageRepositoryFactory
    {
        private static List<Lazy<INuGetResourceProvider>> _providers;
        private static List<Lazy<INuGetResourceProvider>> Providers
        {
            get
            {
                if (_providers == null)
                {
                    _providers = new List<Lazy<INuGetResourceProvider>>();
                    _providers.AddRange(Repository.Provider.GetCoreV3());
                    _providers.AddRange(Repository.Provider.GetCoreV2());
                }
                return _providers;
            }
        }

        public static SourceRepository CreateRepository(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            return Repository.CreateSource(Providers, source);
        }
    }
}
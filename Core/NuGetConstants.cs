namespace NuGetPe
{
    public static class NuGetConstants
    {
        public static readonly string DefaultFeedUrl = "https://api.nuget.org/v3/index.json";
        public static readonly string V2FeedUrl = "https://www.nuget.org/api/v2/";
        public static readonly string V2LegacyFeedUrl = "https://nuget.org/api/v2/";
        public static readonly string PluginFeedUrl = "http://www.myget.org/F/npe/";

        public const string V2LegacyNuGetPublishFeed = "https://nuget.org";
        public const string NuGetPublishFeed = "https://www.nuget.org";

        public static readonly NuGet.Configuration.PackageSource DefaultFeedPackageSource = new NuGet.Configuration.PackageSource(DefaultFeedUrl);
        public static readonly NuGet.Configuration.PackageSource NuGetPublishFeedPackageSource = new NuGet.Configuration.PackageSource(NuGetPublishFeed);
        public static readonly NuGet.Configuration.PackageSource PluginFeedPackageSource = new NuGet.Configuration.PackageSource(PluginFeedUrl);
    }
}

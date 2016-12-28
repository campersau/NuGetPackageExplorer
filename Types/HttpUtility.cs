using System;
using System.Collections.Specialized;

namespace NuGetPackageExplorer.Types
{
    public static class HttpUtility
    {
        public static NameValueCollection ParseQueryString(string query)
        {
            var result = new NameValueCollection();
            if (!string.IsNullOrEmpty(query))
            {
                if (query.StartsWith("?", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Substring(1);
                }

                var parts = query.Split('&', '=');
                for (int i = 0; i < parts.Length; i += 2)
                {
                    var name = parts[i];
                    var value = (i + 1 < parts.Length) ? parts[i + 1] : null;
                    result[name] = value;
                }
            }

            return result;
        }
    }
}
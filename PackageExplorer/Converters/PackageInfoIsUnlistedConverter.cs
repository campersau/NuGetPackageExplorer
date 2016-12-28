using NuGet.Protocol.Core.Types;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PackageExplorer
{
    public class PackageInfoIsUnlistedConverter : IValueConverter
    {
        // This is temporary until we fix the gallery to have proper first class support for this.
        // The magic unpublished date is 1900-01-01T00:00:00
        public static readonly DateTimeOffset Unpublished = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeZoneInfo.Local.BaseUtcOffset);

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = (IPackageSearchMetadata)value;
            return item.Published == Unpublished;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

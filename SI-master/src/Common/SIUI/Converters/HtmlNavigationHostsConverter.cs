using System.Globalization;
using System.Windows.Data;
using SIUI.ViewModel;

namespace SIUI.Converters;

public sealed class HtmlNavigationHostsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ContentViewModel { Value: var address, OriginalValue: string original }
            && original.StartsWith("http://<GAMEHOST>:6999/package/Html/", StringComparison.Ordinal)
            && Uri.TryCreate(address, UriKind.Absolute, out var package)
            && package.Scheme == Uri.UriSchemeHttp
            && package.Port == 6999
            && package.AbsolutePath.StartsWith("/package/Html/", StringComparison.Ordinal))
        {
            return package.GetLeftPart(UriPartial.Authority) + "/package/Html/";
        }

        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

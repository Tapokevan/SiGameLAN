using SImulator.Properties;
using SIStorage.Service.Contract.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SImulator.Converters;

public sealed class NameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var publisher = (Publisher)value;
        return publisher.Id == -2 ? Resources.All : (publisher.Id == -1 ? Resources.NotSet : publisher.Name);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => 
        throw new NotImplementedException();
}

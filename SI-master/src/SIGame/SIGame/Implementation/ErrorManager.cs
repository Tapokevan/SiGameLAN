using SIGame.Contracts;
using System;
using System.IO;
using System.Windows;

namespace SIGame.Implementation;

internal sealed class ErrorManager : IErrorManager
{
    public bool SendErrorReport(Exception exception)
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SI-LAN", "Logs");
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "errors.log"), DateTimeOffset.Now + " " + exception + Environment.NewLine);
        }
        catch (IOException) { }
        Application.Current.Dispatcher.Invoke(() => MessageBox.Show(exception.Message, "СИ • Домашняя игра", MessageBoxButton.OK, MessageBoxImage.Error));
        return true;
    }
    public void SendDelayedReports() { }
}

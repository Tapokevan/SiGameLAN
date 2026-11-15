using System.Windows.Controls;
using System.Linq;

namespace SIGame
{
    /// <summary>
    /// Interaction logic for NewGameView.xaml
    /// </summary>
    public partial class NewGameView : UserControl
    {
        public NewGameView()
        {
            InitializeComponent();
            var addresses = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(a.Address))
                .Select(a => a.Address.ToString());
            LocalAddresses.Text = "Ваш IP для подключения друзей  " + string.Join("   /   ", addresses);
        }
    }
}

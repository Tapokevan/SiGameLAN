using SIGame.ViewModel;
using SIUI.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIGame;

/// <summary>
/// Defines game room.
/// </summary>
public partial class Studia : UserControl
{
    public Studia() => InitializeComponent();

    private void UpdateRoomLayout(object sender, RoutedEventArgs e)
    {
        var cardsRow = ((CheckBox)sender).IsChecked == true ? 3 : 1;
        if (playerCards is null || Grid.GetRow(playerCards) == cardsRow)
        {
            return;
        }

        (upperRoomRow.Height, lowerRoomRow.Height) = (lowerRoomRow.Height, upperRoomRow.Height);
        (upperRoomRow.MinHeight, lowerRoomRow.MinHeight) = (lowerRoomRow.MinHeight, upperRoomRow.MinHeight);
        Grid.SetRow(playerCards, cardsRow);
        Grid.SetRow(gameArea, cardsRow == 3 ? 1 : 3);
    }

    // TODO: can Table call the command directly?
    private void Table_MediaEnded(object sender, RoutedEventArgs e)
    {
        var mediaElement = (MediaElement)e.OriginalSource;

        var contentType = MediaController.GetContentType(mediaElement);
        var contentValue = MediaController.GetContentValue(mediaElement);

        ((GameViewModel)DataContext).OnMediaContentCompleted(contentType, contentValue);
    }

    private void UserControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is GameViewModel room && room.PressGameButton.CanBeExecuted)
        {
            room.PressGameButton.Execute(null);
        }
    }

    private void LeaveRoom_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is GameViewModel room
            && MessageBox.Show(Window.GetWindow(this), "Выйти из комнаты?", "СИ • Домашняя игра",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            room.EndGame.Execute(null);
        }
    }
}

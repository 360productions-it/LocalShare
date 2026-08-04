using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LocalShare.App.ViewModels;
using LocalShare.Core.Models;

namespace LocalShare.App.Views;

public partial class PeersView : UserControl
{
    public PeersView()
    {
        InitializeComponent();
    }

    private void UserControl_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0 && DataContext is PeersViewModel vm)
            {
                vm.HandleDroppedFiles(files);
            }
        }
    }

    private void UserControl_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void PeerCard_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && sender is FrameworkElement element && element.DataContext is Peer targetPeer)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0 && DataContext is PeersViewModel vm)
            {
                vm.HandleDroppedFiles(files, targetPeer);
            }
        }
    }

    private void PeerCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is Peer clickedPeer && DataContext is PeersViewModel vm)
        {
            vm.TogglePeerSelectionCommand.Execute(clickedPeer);
        }
    }
}

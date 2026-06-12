using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GradeBook.Services;
using GradeBook.ViewModels;

namespace GradeBook.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider.GetService(typeof(MainViewModel)) as MainViewModel;
            // #region agent log
            DebugLog.Write("H3", "MainWindow.xaml.cs:MainWindow", "data context assigned", new { hasDataContext = DataContext != null, dataContextType = DataContext?.GetType().Name });
            // #endregion
        }

        private void StudentsGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row == null) return;

            row.Focus();
            row.IsSelected = true;
        }

        private static T? FindVisualParent<T>(DependencyObject? child)
            where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) return parent;
                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }
    }
}
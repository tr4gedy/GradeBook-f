using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GradeBook.ViewModels;

namespace GradeBook.Views
{
    public partial class MainWindow : Window
    {
        // Конструктор принимает ViewModel через Dependency Injection
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        // Фикс: выделение строки DataGrid при клике правой кнопкой мыши (для контекстного меню)
        private void StudentsGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && e.OriginalSource is DependencyObject dep)
            {
                while (dep != null && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }
                if (dep is DataGridRow row)
                {
                    dataGrid.SelectedItem = row.DataContext;
                }
            }
        }
    }
}
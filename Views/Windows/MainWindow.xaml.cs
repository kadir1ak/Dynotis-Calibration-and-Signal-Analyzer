using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using OxyPlot;
using OxyPlot.Series;
using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Device;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Sensors;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Serial;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Interface;

namespace Dynotis_Calibration_and_Signal_Analyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Definitions
        private readonly SerialPortsManager serialPortsManager;

        private Dynotis _dynotis;
        public Dynotis dynotis
        {
            get => _dynotis;
            set
            {
                if (_dynotis != value)
                {
                    _dynotis = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            #region Cihaz Oluşturma
            // Dynotis cihazını oluştur
            dynotis = new Dynotis();
            DataContext = this;
            #endregion

            #region Serial Port Yönetimi
            // Serial port yöneticisi oluşturuldu
            serialPortsManager = new SerialPortsManager();

            // ComboBox'ı SerialPorts koleksiyonuna bağla
            portComboBox.ItemsSource = serialPortsManager.SerialPorts;

            // Kaynak temizleme işlemleri
            Closed += (s, e) => serialPortsManager.Dispose();
            #endregion
        }
        private void Itki_DataList_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {

        }

        private void Itki_DataList_Sorting(object sender, DataGridSortingEventArgs e)
        {

        }

        private void Tork_DataList_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {

        }

        private void Tork_DataList_Sorting(object sender, DataGridSortingEventArgs e)
        {

        }

        private void Cross_DataList_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {

        }

        private void Cross_DataList_Sorting(object sender, DataGridSortingEventArgs e)
        {

        }

        private void Akım_DataList_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {

        }

        private void Akım_DataList_Sorting(object sender, DataGridSortingEventArgs e)
        {

        }

        private void Voltaj_DataList_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {

        }

        private void Voltaj_DataList_Sorting(object sender, DataGridSortingEventArgs e)
        {

        }

        private void Uygulanan_Itki_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void Uygulanan_Itki_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Uygulanan_Tork_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void Uygulanan_Tork_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Uygulanan_Tork_Mesafesi_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void Uygulanan_Tork_Mesafesi_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Uygulanan_Akım_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void Uygulanan_Akım_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Uygulanan_Voltaj_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void Uygulanan_Voltaj_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Calibration_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExcelExport_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExcelImport_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddPointButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DeletePointButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Port_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (portComboBox.SelectedItem is string selectedPort)
            {
                try
                {
                    // Seçilen portu bağla
                    await dynotis.SerialPortConnect(selectedPort);

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to connect to port {selectedPort}: {ex.Message}",
                                    "Connection Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            }
        }

        private void PlotButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Itki_Sonuc_Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Tork_Sonuc_Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Tablolar_TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl tabControl && tabControl.SelectedItem is TabItem selectedTab)
            {
                // Seçili tab'a göre mod ayarla
                switch (selectedTab.Name)
                {
                    case "TabItem_Itki":
                        dynotis.Interface.Mode = Mode.Thrust;
                        break;
                    case "TabItem_Tork":
                        dynotis.Interface.Mode = Mode.Torque;
                        break;
                    case "TabItem_Cross":
                        dynotis.Interface.Mode = Mode.LoadCellTest;
                        break;
                    case "TabItem_Akım":
                        dynotis.Interface.Mode = Mode.Current;
                        break;
                    case "TabItem_Voltaj":
                        dynotis.Interface.Mode = Mode.Voltage;
                        break;
                    default:
                        break;
                }
                UpdateGroupBoxVisibility(dynotis.Interface.Mode);
            }
        }
        private void UpdateGroupBoxVisibility(Mode mode)
        {
            // Tüm GroupBox'ları gizle
            Itki_Border.Visibility = Visibility.Collapsed;
            Tork_Border.Visibility = Visibility.Collapsed;
            Akım_Border.Visibility = Visibility.Collapsed;
            Voltaj_Border.Visibility = Visibility.Collapsed;

            Itki_Data_Border.Visibility = Visibility.Collapsed;
            Tork_Data_Border.Visibility = Visibility.Collapsed;
            Akım_Data_Border.Visibility = Visibility.Collapsed;
            Voltaj_Data_Border.Visibility = Visibility.Collapsed;

            // Seçili mode'a göre GroupBox'ı göster
            switch (mode)
            {
                case Mode.Thrust:
                    Itki_Border.Visibility = Visibility.Visible;
                    Itki_Data_Border.Visibility = Visibility.Visible;
                    Tork_Data_Border.Visibility = Visibility.Visible;
                    break;

                case Mode.Torque:
                    Tork_Border.Visibility = Visibility.Visible;
                    Itki_Data_Border.Visibility = Visibility.Visible;
                    Tork_Data_Border.Visibility = Visibility.Visible;
                    break;

                case Mode.LoadCellTest:
                    Itki_Border.Visibility = Visibility.Visible;
                    Tork_Border.Visibility = Visibility.Visible;
                    Itki_Data_Border.Visibility = Visibility.Visible;
                    Tork_Data_Border.Visibility = Visibility.Visible;
                    break;

                case Mode.Current:
                    Akım_Border.Visibility = Visibility.Visible;
                    Akım_Data_Border.Visibility = Visibility.Visible;
                    Voltaj_Data_Border.Visibility = Visibility.Visible;
                    break;

                case Mode.Voltage:
                    Voltaj_Border.Visibility = Visibility.Visible;
                    Akım_Data_Border.Visibility = Visibility.Visible;
                    Voltaj_Data_Border.Visibility = Visibility.Visible;
                    break;

                default:
                    break;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}


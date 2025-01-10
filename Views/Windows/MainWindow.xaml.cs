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
using System.Text.RegularExpressions;

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
        private void Clear_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Calibration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dynotis != null)
                {
                    await dynotis.PerformCalibrationAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExcelExport_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExcelImport_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void AddPointButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dynotis != null)
                {
                    await dynotis.AddPointAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }

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
        private void NumericTextBoxDouble_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.,]+"); // Allow only numbers, dots, and commas
            e.Handled = regex.IsMatch(e.Text);
        }

        private void NumericTextBoxInt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+"); // Allow only numbers
            e.Handled = regex.IsMatch(e.Text);
        }

        private void NumericTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text.Length > 9)
            {
                textBox.Text = textBox.Text.Substring(0, 9);
                textBox.CaretIndex = textBox.Text.Length; // Move caret to end
            }
        }

        private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true; // Prevent space key input
            }
        }

        private void Tork_Dara_Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Itki_Dara_Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}


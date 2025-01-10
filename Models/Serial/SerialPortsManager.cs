using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Windows;

namespace Dynotis_Calibration_and_Signal_Analyzer.Models.Serial
{
    public class SerialPortsManager : IDisposable
    {
        private ManagementEventWatcher _serialPortsRemovedWatcher;
        private ManagementEventWatcher _serialPortsAddedWatcher;
        public ObservableCollection<string> SerialPorts { get; }
        public event Action<string> SerialPortAdded;
        public event Action<string> SerialPortRemoved;
        public SerialPortsManager()
        {
            SerialPorts = new ObservableCollection<string>();
            InitializeEventWatchers();
            ScanSerialPorts();
        }
        private void InitializeEventWatchers()
        {
            try
            {
                // Watch for serial ports removal
                _serialPortsRemovedWatcher = CreateEventWatcher(
                    "SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3",
                    OnSerialPortRemoved
                );

                // Watch for serial ports addition
                _serialPortsAddedWatcher = CreateEventWatcher(
                    "SELECT * FROM __InstanceOperationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_SerialPort'",
                    OnSerialPortAdded
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing event watchers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private ManagementEventWatcher CreateEventWatcher(string query, EventArrivedEventHandler eventHandler)
        {
            var watcher = new ManagementEventWatcher(new ManagementScope("root\\CIMV2"), new WqlEventQuery(query));
            watcher.EventArrived += eventHandler;
            watcher.Start();
            return watcher;
        }
        private void OnSerialPortRemoved(object sender, EventArrivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(ScanSerialPorts);
        }
        private void OnSerialPortAdded(object sender, EventArrivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(ScanSerialPorts);
        }
        public void ScanSerialPorts()
        {
            try
            {
                var existingPorts = SerialPorts.ToList();
                var currentPorts = SerialPort.GetPortNames().ToList();

                // Add new ports
                foreach (var port in currentPorts.Except(existingPorts))
                {
                    SerialPorts.Add(port);
                    SerialPortAdded?.Invoke(port);
                }

                // Remove missing ports
                foreach (var port in existingPorts.Except(currentPorts))
                {
                    SerialPorts.Remove(port);
                    SerialPortRemoved?.Invoke(port);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error scanning serial ports: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public IEnumerable<string> GetSerialPorts()
        {
            return SerialPorts.ToList();
        }
        public void Dispose()
        {
            DisposeWatcher(_serialPortsRemovedWatcher);
            DisposeWatcher(_serialPortsAddedWatcher);
        }
        private void DisposeWatcher(ManagementEventWatcher watcher)
        {
            try
            {
                watcher?.Stop();
                watcher?.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disposing watcher: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

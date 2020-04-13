using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace bluetooth
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class MainPage : Page
    {
        Guid midiServiceUuid = Guid.Parse("03B80E5A-EDE8-4B33-A751-6CE34EC4C700");
        Guid midiCharacteristicUuid = Guid.Parse("7772E5DB-3868-4112-A1A9-F2669D106BF3");
        BluetoothLEDevice bluetoothLeDevice = null;
        List<ulong> midiDevices = new List<ulong>();
        ArrayList midiSignals = new ArrayList();

        public MainPage()
        {
            this.InitializeComponent();
            BluetoothLEAdvertisementWatcher watcher = new BluetoothLEAdvertisementWatcher();
            watcher.Received += OnAdvertisementReceived;
            watcher.Start();
        }

        private void Scan(object sender, RoutedEventArgs e)
        {
            devices.Text = "";
            foreach(ulong device in midiDevices)
            {
                devices.Text += (device.ToString("x") + "\n");
            }
        }

        private void Connect(object sender, RoutedEventArgs e)
        {
            foreach(ulong device in midiDevices)
            {
                ConnectDevice(device);
            }
        }

        private void Disonnect(object sender, RoutedEventArgs e)
        {
            DisconnectDevice();
            midi.Text = "";
        }

        private void ReadData(object sender, RoutedEventArgs e)
        {
            foreach (byte[] data in midiSignals)
            {
                string signal = data[2].ToString() + ";" + data[3].ToString() + ";" + data[4].ToString();
                midi.Text += (signal + "\n");
            }
            midiSignals = null;
        }

        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {
            ulong bluetoothAddress = eventArgs.BluetoothAddress;
            List<Guid> services = new List<Guid>(eventArgs.Advertisement.ServiceUuids);

            if (services.Contains(midiServiceUuid) & !midiDevices.Contains(bluetoothAddress))
            {
                midiDevices.Add(bluetoothAddress);
            }
        }

        private async void ConnectDevice(ulong bluetoothAddress)
        {
            GattDeviceService midiService = null;
            GattCharacteristic midiCharacteristic = null;
            bluetoothLeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
            GattDeviceServicesResult serviceResult = await bluetoothLeDevice.GetGattServicesAsync();
            if (serviceResult.Status == GattCommunicationStatus.Success)
            {
                devices.Text = "Connected";
                List<GattDeviceService> services = new List<GattDeviceService>(serviceResult.Services);
                foreach (GattDeviceService service in services)
                {
                    if (service.Uuid == midiServiceUuid)
                    {
                        midiService = service;
                    }
                }
                GattCharacteristicsResult characteristicResult = await midiService.GetCharacteristicsAsync();
                List<GattCharacteristic> characteristics = new List<GattCharacteristic>(characteristicResult.Characteristics);
                foreach (GattCharacteristic characteristic in characteristics)
                {
                    if (characteristic.Uuid == midiCharacteristicUuid)
                    {
                        midiCharacteristic = characteristic;
                    }
                }
                GattCommunicationStatus status = await midiCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status == GattCommunicationStatus.Success)
                {
                    midiCharacteristic.ValueChanged += Characteristic_ValueChanged;
                }
            }
        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            DataReader reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] midiData = new Byte[5];

            reader.ReadBytes(midiData);
            string signal = midiData[2].ToString() + ";" + midiData[3].ToString() + ";" + midiData[4].ToString();
            midiSignals.Add(midiData);
        }

        private void DisconnectDevice()
        {
            bluetoothLeDevice.Dispose();
            midiSignals = null;
            midiDevices = null;
            System.GC.Collect();
            devices.Text = "Disconnected, Scan Again";
        }
    }
}

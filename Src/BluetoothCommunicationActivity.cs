using Android.Bluetooth;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothSerialCommunication.Src
{
    [Activity(Label = "通信界面")]
    public class BluetoothCommunicationActivity : Activity {
        private BluetoothDevice? _device;
        private BluetoothGatt? _gatt;
        private TextView? _displayTextView;
        private EditText? _inputEditText;
        private BluetoothGattCharacteristic? _writeCharacteristic;
        private BluetoothGattCharacteristic? _notifyCharacteristic;

        protected override void OnCreate(Bundle? savedInstanceState) {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.bluetooth_communication);

            // 获取传递的参数
            var deviceAddress = Intent.GetStringExtra("deviceAddress");
            var serviceUUIDStr = Intent.GetStringExtra("serviceUUID");
            var writeUUIDStr = Intent.GetStringExtra("writeUUID");
            var notifyUUIDStr = Intent.GetStringExtra("notifyUUID");

            var serviceUUID = UUID.FromString(serviceUUIDStr);
            var writeUUID = UUID.FromString(writeUUIDStr);
            var notifyUUID = UUID.FromString(notifyUUIDStr);

            // 初始化蓝牙连接
            _device = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(deviceAddress);
            _gatt = _device.ConnectGatt(this, false, new MyGattCallback(this, serviceUUID, writeUUID, notifyUUID));

            // 初始化UI
            _displayTextView = FindViewById<TextView>(Resource.Id.tvDisplay);
            _inputEditText = FindViewById<EditText>(Resource.Id.etInput);
            var btnSend = FindViewById<Button>(Resource.Id.btnSend);
             
            // 设置发送按钮点击事件
            btnSend.Click += OnSendClick;

            // 获取特征（使用传递的UUID）
            var service = _gatt.GetService(serviceUUID);
            if (service == null) {
                UpdateDisplay("服务未找到，请检查UUID\n", Android.Graphics.Color.Red);
                return;
            }
            _writeCharacteristic = service.GetCharacteristic(writeUUID); // 写入特征（ESP32接收）
            _notifyCharacteristic = service.GetCharacteristic(notifyUUID); // 通知特征（ESP32发送）

            if (_writeCharacteristic == null || _notifyCharacteristic == null) {
                UpdateDisplay("特征未找到，请检查UUID\n", Android.Graphics.Color.Red);
                return;
            }


            // 启动通知
            _gatt.SetCharacteristicNotification(_writeCharacteristic, true);
            var descriptor = _writeCharacteristic.GetDescriptor(UUID.FromString("00002902-0000-1000-8000-00805f9b34fb"));
            if (descriptor != null) {
                _ = descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                _gatt.WriteDescriptor(descriptor);
            }
        }

        private void OnSendClick(object sender, EventArgs e) {
            var input = _inputEditText?.Text;
            if (string.IsNullOrEmpty(input) || _writeCharacteristic == null || _gatt == null) return;

            // 发送数据
            var bytes = Encoding.UTF8.GetBytes(input);
            _writeCharacteristic.SetValue(bytes);
            _gatt.WriteCharacteristic(_writeCharacteristic);

            // 显示发送内容
            UpdateDisplay($"发送: {input}\n", Android.Graphics.Color.Green);
            _inputEditText.Text = string.Empty;
        }

        // 处理特征值变化（接收数据）
        private void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic) {
            if (characteristic == null || _notifyCharacteristic == null) return;

            if (characteristic.Uuid == _notifyCharacteristic.Uuid) {
                var value = characteristic.GetValue();
                if (value != null) {
                    var str = Encoding.UTF8.GetString(value);
                    RunOnUiThread(() => UpdateDisplay($"接收: {str}\n", Android.Graphics.Color.Blue));
                }
            }
        }

        // 更新显示框
        private void UpdateDisplay(string text, Android.Graphics.Color color) {
            RunOnUiThread(() => {
                _displayTextView.Text += text;
                _displayTextView.SetTextColor(color);
            });
        }

        // 释放资源
        protected override void OnDestroy() {
            base.OnDestroy();
            if (_gatt != null) {
                _gatt.Disconnect();
                _gatt.Close();
                _gatt.Dispose();
                _gatt = null;
            }
        }

        // 自定义的BluetoothGattCallback类
        private class MyGattCallback : BluetoothGattCallback {
            private BluetoothCommunicationActivity _activity;
            private readonly UUID _serviceUUID;
            private readonly UUID _writeUUID;
            private readonly UUID _notifyUUID;

            public MyGattCallback(BluetoothCommunicationActivity activity, UUID serviceUUID, UUID writeUUID, UUID notifyUUID) {
                _activity = activity;
                _serviceUUID = serviceUUID;
                _writeUUID = writeUUID;
                _notifyUUID = notifyUUID;
            }

            public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState) {
                base.OnConnectionStateChange(gatt, status, newState);
                if (newState == ProfileState.Connected) {
                    _activity.UpdateDisplay("连接成功，正在发现服务...\n", Android.Graphics.Color.Green);
                    gatt.DiscoverServices();
                } else if (newState == ProfileState.Disconnected) {
                    _activity.UpdateDisplay("连接断开\n", Android.Graphics.Color.Red);
                }
            }

            public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status) {
                base.OnServicesDiscovered(gatt, status);
                if (status == GattStatus.Success) {
                    // 获取服务和特征
                    var service = gatt.GetService(_serviceUUID);
                    if (service == null) {
                        _activity.UpdateDisplay("服务未找到，请检查UUID\n", Android.Graphics.Color.Red);
                        return;
                    }

                    _activity._writeCharacteristic = service.GetCharacteristic(_writeUUID);
                    _activity._notifyCharacteristic = service.GetCharacteristic(_notifyUUID);

                    if (_activity._writeCharacteristic == null || _activity._notifyCharacteristic == null) {
                        _activity.UpdateDisplay("特征未找到，请检查UUID\n", Android.Graphics.Color.Red);
                        return;
                    }

                    // 启动通知
                    gatt.SetCharacteristicNotification(_activity._notifyCharacteristic, true);
                    var descriptor = _activity._notifyCharacteristic.GetDescriptor(UUID.FromString("00002902-0000-1000-8000-00805f9b34fb"));
                    if (descriptor != null) {
                        descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                        gatt.WriteDescriptor(descriptor);
                    }

                    _activity.UpdateDisplay("服务和特征就绪，可以开始通信\n", Android.Graphics.Color.Green);
                } else {
                    _activity.UpdateDisplay("服务发现失败，请重试\n", Android.Graphics.Color.Red);
                }
            }

            public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic) {
                base.OnCharacteristicChanged(gatt, characteristic);
                _activity.OnCharacteristicChanged(gatt, characteristic);
            }
        }
    }
}

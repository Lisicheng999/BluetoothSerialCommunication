using Android.Bluetooth;
using Android.Content;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothSerialCommunication.Src
{
    // 自定义的BluetoothGattCallback类（内部类）
    public class MyGattCallback : BluetoothGattCallback {
        private readonly WeakReference<DeviceDetailActivity> _activityRef;

        public MyGattCallback(DeviceDetailActivity activity) {
            _activityRef = new WeakReference<DeviceDetailActivity>(activity);
        }

        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState) {
            base.OnConnectionStateChange(gatt, status, newState);

            if (newState == ProfileState.Connected) {
                UpdateLog("连接成功！开始发现服务...\n");
                gatt.DiscoverServices();
            } else if (newState == ProfileState.Disconnected) {
                UpdateLog("连接断开\n");
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status) {
            base.OnServicesDiscovered(gatt, status);

            if (status == GattStatus.Success) {
                UpdateLog("服务发现完成！\n");

                // 打印所有服务和特征的UUID
                foreach (var gattService in gatt.Services) {
                    // Replace the line with the error
                    UpdateLog($"发现服务: {gattService.Uuid}\n");
                    foreach (var characteristic in gattService.Characteristics) {
                        UpdateLog($"Bluetooth\t特征: {characteristic.Uuid}");
                    }
                }

                // 获取用户输入的UUID
                var activity = GetActivity();
                if (activity == null) return;

                var serviceUUID = UUID.FromString(activity._serviceUUIDInput.Text);
                var writeCharacteristicUUID = UUID.FromString(activity._writeCharacteristicInput.Text);
                var notifyCharacteristicUUID = UUID.FromString(activity._notifyCharacteristicInput.Text);

                // 获取服务和特征
                var service = gatt.GetService(serviceUUID);
                if (service == null) {
                    UpdateLog("服务未找到，请检查UUID\n");
                    return;
                }

                var writeCharacteristic = service.GetCharacteristic(writeCharacteristicUUID);
                var notifyCharacteristic = service.GetCharacteristic(notifyCharacteristicUUID);

                // 验证服务和特征是否存在
                if (service == null) {
                    UpdateLog($"未找到服务: {serviceUUID}\n");
                } else if (writeCharacteristic == null) {
                    UpdateLog($"未找到写特征: {writeCharacteristicUUID}\n");
                } else if (notifyCharacteristic == null) {
                    UpdateLog($"未找到通知特征: {notifyCharacteristicUUID}\n");
                } else {
                    // 启动通知
                    gatt.SetCharacteristicNotification(notifyCharacteristic, true);
                    // 跳转到通信页面
                    var intent = new Intent(activity, typeof(BluetoothCommunicationActivity));
                    intent.PutExtra("deviceAddress", activity._device.Address);
                    intent.PutExtra("serviceUUID", serviceUUID.ToString());
                    intent.PutExtra("writeUUID", writeCharacteristicUUID.ToString());
                    intent.PutExtra("notifyUUID", notifyCharacteristicUUID.ToString());
                    activity.StartActivity(intent);
                }
            } else {
                UpdateLog("服务发现失败\n");
            }
        }

        private void UpdateLog(string message) {
            if (GetActivity() is DeviceDetailActivity activity) {
                activity.RunOnUiThread(() => activity._logTextView.Text += message);
            }
        }

        private DeviceDetailActivity? GetActivity() {
            return _activityRef.TryGetTarget(out var activity) ? activity : null;
        }
    }
}

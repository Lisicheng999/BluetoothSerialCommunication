using Android.Bluetooth;
using Android.Content;
using Android.Util;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothSerialCommunication.Src
{
    public class MyBroadcastReceiver : BroadcastReceiver {
        private readonly Action<Context, Intent> onReceiveAction;
        private MainActivity _mainActivity; // 存储 MainActivity 的引用

        public MyBroadcastReceiver(Action<Context, Intent> onReceiveAction) {
            this.onReceiveAction = onReceiveAction;
        }

        public MyBroadcastReceiver(Action<Context, Intent> onReceiveAction, MainActivity mainActivity) {
            this.onReceiveAction = onReceiveAction;
            this._mainActivity = mainActivity;
        }

        public override void OnReceive(Context context, Intent intent) {
            //Log.Debug("BluetoothScan", "OnReceive 方法已触发"); // 调试日志
            ((Activity)context).RunOnUiThread(() => {
                var action = intent.Action;
                if (action == BluetoothDevice.ActionFound) {
                    var device = (BluetoothDevice?)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                    var rssi = intent.GetShortExtra(BluetoothDevice.ExtraRssi, short.MinValue);

                    if (device != null) {
                        Log.Debug("BluetoothScan", $"发现设备: {device.Name} - {device.Address}"); // 确保日志正确输出
                        if (!_mainActivity.devices.Any(d => d.Address == device.Address)) {
                            _mainActivity.devices.Add(new Device(
                                device.Name ?? "未知",
                                device.Address ?? "未知地址",
                                rssi));
                            _mainActivity.adapter?.NotifyDataSetChanged();
                        }
                    }
                }
            });
        }
    }
}

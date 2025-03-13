using Android.Content;
using Android.Util;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Android.Icu.Text.Transliterator;

namespace BluetoothSerialCommunication.Src {
    public class DeviceAdapter : BaseAdapter<Device> {
        private readonly Context context;
        private readonly List<Device> devices;

        public DeviceAdapter(Context context, List<Device> devices) {
            this.context = context;
            this.devices = devices;
        }

        public override int Count => devices.Count;

        public override Device this[int position] => devices[position];

        public override long GetItemId(int position) {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent) {
            var view = convertView ?? LayoutInflater.From(context).Inflate(Resource.Layout.device_item, parent, false);

            var device = devices[position];

            // 绑定数据到UI控件
            view.FindViewById<TextView>(Resource.Id.tvDeviceName).Text = device.Name;
            view.FindViewById<TextView>(Resource.Id.tvDeviceAddress).Text = $"MAC地址: {device.Address}";
            view.FindViewById<TextView>(Resource.Id.tvRssi).Text = $"信号强度: {device.Rssi} dBm";

            return view;
        }
    }
}
//namespace BluetoothSerialCommunication.Src
//{
//    public class DeviceAdapter : ArrayAdapter<Device> {
//        public DeviceAdapter(Context context, List<Device> devices)
//            : base(context, 0, devices) { }

//        public override View GetView(int position, View? convertView, ViewGroup parent) {
//            var view = convertView ??
//                LayoutInflater.From(Context).Inflate(Resource.Layout.device_item, parent, false);

//            var device = GetItem(position);

//            if (device != null) {
//                var tvName = view.FindViewById<TextView>(Resource.Id.tvDeviceName);
//                var tvAddress = view.FindViewById<TextView>(Resource.Id.tvDeviceAddress);
//                var tvRssi = view.FindViewById<TextView>(Resource.Id.tvRssi);

//                tvName.Text = device.Name ?? "未知";
//                tvAddress.Text = device.Address ?? "未知地址";
//                tvRssi.Text = $"RSSI: {device.Rssi} dBm";

//                Log.Debug("DeviceAdapter", $"渲染设备: {device.Name} - {device.Address}"); // 调试日志
//            }

//            return view;
//        }
//    }
//}

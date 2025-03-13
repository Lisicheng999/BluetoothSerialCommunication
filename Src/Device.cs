using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothSerialCommunication.Src
{
    public class Device
    {
        public string Name { get; set; } = "未知";
        public string Address { get; set; } = "未知地址";
        public int Rssi { get; set; } // 信号强度（单位dBm）

        public Device(string name, string address, int rssi) {
            Name = name ?? "未知";
            Address = address ?? "未知地址";
            Rssi = rssi;
        }
    }
}

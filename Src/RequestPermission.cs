using Android.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothSerialCommunication.Src
{
    class RequestPermission
    {
        // 新增权限请求方法
        private void RequestPermissions() {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) {
                var permissions = new[] {
                    Android.Manifest.Permission.BluetoothScan,
                    Android.Manifest.Permission.BluetoothConnect,
                    Android.Manifest.Permission.AccessFineLocation
                };
            }
        }

        //// 新增权限检查方法
        //private bool HasPermissions() {
        //    if (Build.VERSION.SdkInt >= BuildVersionCodes.S) {
        //        var permissions = new[] {
        //            Android.Manifest.Permission.BluetoothScan,
        //            Android.Manifest.Permission.BluetoothConnect,
        //            Android.Manifest.Permission.AccessFineLocation
        //        };
        //        foreach (var permission in permissions) {
        //            if (CheckSelfPermission(permission) != Permission.Granted)
        //                return false;
        //        }
        //        return true;
        //    }
        //    return true; // 旧版本默认有权限
        //}
    }
}

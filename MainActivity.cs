using Android;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using BluetoothSerialCommunication.Src;
using System.Timers;
using Android.App;
using Android.Content.PM;
using Android.Widget;
using Java.Util;
using System;
using System.Collections.Generic;
using Android.Util;
using Android.Views;

namespace BluetoothSerialCommunication
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity {
        BluetoothAdapter? bluetoothAdapter;
        public List<Device> devices = new List<Device>();
        public DeviceAdapter? adapter;
        private readonly BroadcastReceiver receiver;
        private System.Timers.Timer scanTimer;
        private ElapsedEventHandler elapsedHandler; // 保存委托的引用
        private System.Timers.Timer uiTimer;
        private ElapsedEventHandler uiUpdater;


        public MainActivity() {
            // 初始化广播接收器
            receiver = new MyBroadcastReceiver((context, intent) =>
            {
                var action = intent.Action;
                if (action == BluetoothDevice.ActionFound) {
                    var device = (BluetoothDevice?)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                    var rssi = intent.GetShortExtra(BluetoothDevice.ExtraRssi, short.MinValue);

                    if (device != null) {
                        RunOnUiThread(() =>
                        {
                            // 检查是否已存在相同 MAC 地址的设备
                            if (!devices.Any(d => d.Address == device.Address)) {
                                devices.Add(new Device(
                                    device.Name ?? "未知",
                                    device.Address ?? "未知地址",
                                    rssi));
                                adapter?.NotifyDataSetChanged();
                            }
                        });
                    }
                }
            }, this);

            // 初始化定时器
            scanTimer = new System.Timers.Timer(10000); // Specify the namespace explicitly
            //scanTimer.Elapsed += (sender, e) => {
            //    RunOnUiThread(() => {
            //        StopScan();
            //        StartScan();
            //    });
            //};
        }

        protected override void OnCreate(Bundle? savedInstanceState) {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // 请求权限
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) {
                var permissions = new string[]
                {
                    Android.Manifest.Permission.BluetoothScan,
                    Android.Manifest.Permission.BluetoothConnect,
                    Android.Manifest.Permission.AccessFineLocation,
                    //Android.Manifest.Permission.BluetoothAdvertise,
                    //Android.Manifest.Permission.BluetoothAdmin,
                };
                RequestPermissions(permissions, 1);
            }

            // 初始化蓝牙适配器
            bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            if (bluetoothAdapter == null) {
                Toast.MakeText(this, "设备不支持蓝牙", ToastLength.Short)?.Show();
                Finish();
            }

            // 初始化 ListView 和适配器
            //var listView = FindViewById<ListView>(Resource.Id.deviceListView);
            //adapter = new DeviceAdapter(this, devices); // v1.0使用自定义适配器
            //listView.Adapter = adapter;  // v1.0

            // 初始化 ListView 的点击事件
            var listView = FindViewById<ListView>(Resource.Id.deviceListView);
            adapter = new DeviceAdapter(this, devices); // v1.0使用自定义适配器
            listView.Adapter = adapter;  // v1.0
            listView.ItemClick += (sender, e) => {
                // 获取选中的设备
                var selectedDevice = devices[e.Position];

                // 创建 Intent 并传递设备数据
                var intent = new Intent(this, typeof(DeviceDetailActivity));
                intent.PutExtra("name", selectedDevice.Name);
                intent.PutExtra("address", selectedDevice.Address);
                intent.PutExtra("rssi", selectedDevice.Rssi);

                // 启动新Activity并应用进入动画
                StartActivity(intent);
                OverridePendingTransition(
                    Resource.Animation.slide_right_in, // 进入动画（从右侧滑入）
                    Resource.Animation.slide_left_out  // 主界面退出动画（向左滑出）
                );
            };

            // 绑定开始按钮
            var scanButton = FindViewById<Button>(Resource.Id.btnScan);
            if (scanButton != null) {
                scanButton.Click += (s, e) => StartScan();
            }

            // 绑定停止按钮
            var stopButton = FindViewById<Button>(Resource.Id.btnStop);
            if (stopButton != null) {
                stopButton.Click += (s, e) => StopScan();
            }
        }

        private void StartScan() {
            // 检查蓝牙是否已启用
            CheckBluetoothEnabled();

            // 检查权限
            if (!HasPermissions()) {
                MyRequestPermissions();
                return;
            }

            // 清空旧数据
            devices.Clear();
            adapter?.NotifyDataSetChanged();

            // 取消当前扫描（如果正在扫描）
            if (bluetoothAdapter?.IsDiscovering == true)
                bluetoothAdapter.CancelDiscovery();

            // 注册广播接收器
            RegisterReceiver(receiver, new IntentFilter(BluetoothDevice.ActionFound));

            // 启动扫描
            bluetoothAdapter?.StartDiscovery();
            Log.Debug("BluetoothScan", "扫描已启动");

            // 检查定时器是否已被释放
            if (scanTimer == null) {
                scanTimer = new System.Timers.Timer(10000);
            }

            // 创建委托并订阅事件
            elapsedHandler = (sender, e) => {
                RunOnUiThread(() => {
                    AutoScanKeepAlive();
                });
            };

            scanTimer.Elapsed += elapsedHandler;
            scanTimer.Start();

            // 更新UI状态
            RunOnUiThread(() => {
                var statusTextView = FindViewById<TextView>(Resource.Id.tvStatus);
                statusTextView.Text = "正在扫描";
                statusTextView.Visibility = ViewStates.Visible; // 显示状态

                // 恢复停用按钮，禁用开始按钮
                var btnScan = FindViewById<Button>(Resource.Id.btnScan);
                btnScan.Enabled = false;
                btnScan.Visibility = ViewStates.Gone;  // 完全移除
                //btnScan.Visibility = ViewStates.Invisible; // 仅隐藏
                var btnStop = FindViewById<Button>(Resource.Id.btnStop);
                btnStop.Enabled = true;
                btnStop.Visibility = ViewStates.Visible;
            });

            // UI更新
            if (uiTimer == null) {
                uiTimer = new System.Timers.Timer(2000);
            }

            uiUpdater = (sender, e) => {
                RunOnUiThread(() => {
                    adapter?.NotifyDataSetChanged(); // 强制刷新适配器
                });
            };

            uiTimer.Elapsed += uiUpdater;
            uiTimer.Start();
        }

        private void StopScan() {
            // 停止扫描并注销接收器
            if (bluetoothAdapter?.IsDiscovering == true)
                bluetoothAdapter.CancelDiscovery();
            UnregisterReceiver(receiver);
            scanTimer.Stop();
            scanTimer.Interval = 10000; // 重置定时器
            OnDestroy(); // 注销定时
            Log.Debug("BluetoothScan", "扫描已停止");

            uiTimer.Stop();
            uiTimer.Interval = 2000;
            if (uiTimer != null) {
                uiTimer.Elapsed -= uiUpdater;
                uiTimer.Stop();
                uiTimer.Dispose();
                uiTimer = null;
                uiUpdater = null;
            }

            // 更新UI状态
            RunOnUiThread(() => {
                var statusTextView = FindViewById<TextView>(Resource.Id.tvStatus);
                statusTextView.Text = "扫描已停止";
                statusTextView.Visibility = ViewStates.Visible; // 显示停止状态

                // 恢复开始按钮，禁用停止按钮
                var btnScan = FindViewById<Button>(Resource.Id.btnScan);
                btnScan.Enabled = true;
                btnScan.Visibility = ViewStates.Visible;
                var btnStop = FindViewById<Button>(Resource.Id.btnStop);
                btnStop.Enabled = false;
                btnStop.Visibility = ViewStates.Gone;
            });

        }

        private void AutoScanKeepAlive() {
            // 清空旧数据
            devices.Clear();
            adapter?.NotifyDataSetChanged();

            // 取消当前扫描（如果正在扫描）
            if (bluetoothAdapter?.IsDiscovering == true)
                bluetoothAdapter.CancelDiscovery();
            // 注销接收器
            UnregisterReceiver(receiver);

            // 注册广播接收器
            RegisterReceiver(receiver, new IntentFilter(BluetoothDevice.ActionFound));

            // 启动扫描
            bluetoothAdapter?.StartDiscovery();
            Log.Debug("BluetoothScan", "Scanning Keep Alive");
        }

        // 注销定时
        protected override void OnDestroy() {
            base.OnDestroy();

            if (scanTimer != null) {
                // 使用委托引用注销事件
                scanTimer.Elapsed -= elapsedHandler;
                scanTimer.Stop();
                scanTimer.Dispose();
                scanTimer = null; // 释放引用并设置为 null
                elapsedHandler = null; // 释放引用
            }
        }

        // 检查蓝牙是否开启
        private void CheckBluetoothEnabled() {
            if (bluetoothAdapter?.IsEnabled == false) {
                var enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableBtIntent, 1001);
            }
        }

        // 处理权限请求结果
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults) {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == 1) {
                if (grantResults.All(p => p == Android.Content.PM.Permission.Granted)) {
                    // 权限已授予，可以开始扫描
                    StartScan(); // 直接启动扫描
                } else {
                    Toast.MakeText(this, "需要权限才能扫描蓝牙设备", ToastLength.Short)?.Show();
                }
            }
        }

        // 新增权限请求方法
        private void MyRequestPermissions() {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) {
                var permissions = new[] {
                    Android.Manifest.Permission.BluetoothScan,
                    Android.Manifest.Permission.BluetoothConnect,
                    Android.Manifest.Permission.AccessFineLocation
        };
                RequestPermissions(permissions, 1);
            }
        }

        // 新增权限检查方法
        private bool HasPermissions() {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) {
                var permissions = new[] {
                    Android.Manifest.Permission.BluetoothScan,
                    Android.Manifest.Permission.BluetoothConnect,
                    Android.Manifest.Permission.AccessFineLocation
                };
                foreach (var permission in permissions) {
                    if (CheckSelfPermission(permission) != Permission.Granted)
                        return false;
                }
                return true;
            }
            return true; // 旧版本默认有权限
        }
    }
}
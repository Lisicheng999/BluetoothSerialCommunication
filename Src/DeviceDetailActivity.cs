using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace BluetoothSerialCommunication.Src {
	[Activity(Label = "Detial", ScreenOrientation = ScreenOrientation.Portrait)]
	public class DeviceDetailActivity : Activity {
		private BluetoothDevice _device;
		private BluetoothGatt _gatt;
		private TextView _logTextView;

		private List<string> _serviceUuids = new List<string>();
		private Dictionary<string, List<string>> _serviceCharacteristics = new Dictionary<string, List<string>>();

		protected override void OnCreate(Bundle? savedInstanceState) {
			base.OnCreate(savedInstanceState);
			SetContentView(Resource.Layout.device_detail);

			// 获取设备信息
			var device = GetDeviceFromIntent();
			_device = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(device.Address);

			// 绑定UI元素
			FindViewById<TextView>(Resource.Id.tvName).Text = $"名称：{device.Name}";
			FindViewById<TextView>(Resource.Id.tvAddress).Text = $"MAC地址：{device.Address}";
			FindViewById<TextView>(Resource.Id.tvRssi).Text = $"信号强度：{device.Rssi} dBm";

			// 初始化日志显示框
			_logTextView = FindViewById<TextView>(Resource.Id.logTextView);

			// 绑定连接按钮
			var btnConnect = FindViewById<Button>(Resource.Id.btnConnect);
			btnConnect.Click += OnConnectClick;

			// 添加选择按钮
			var btnSelectUUID = FindViewById<Button>(Resource.Id.btnSelectUUID);
			btnSelectUUID.Click += (s, e) => ShowUUIDSelectionDialog();

			// 设置返回按钮
			FindViewById<Button>(Resource.Id.btnBack).Click += (s, e) => {
				Finish();
				OverridePendingTransition(
					Resource.Animation.slide_left_in,
					Resource.Animation.slide_right_out
				);
			};
		}

        // 新增重载方法：支持颜色参数
        private void UpdateLog(string message, Android.Graphics.Color color) {
            RunOnUiThread(() => {
                var spannable = new Android.Text.SpannableString(_logTextView.Text + message);
                var start = _logTextView.Text.Length;
                spannable.SetSpan(new Android.Text.Style.ForegroundColorSpan(color), start, spannable.Length(), Android.Text.SpanTypes.ExclusiveExclusive);
                _logTextView.SetText(spannable, TextView.BufferType.Spannable);
            });
        }

        // 单参数版本（默认黑色）
        private void UpdateLog(string message) {
			UpdateLog(message, Android.Graphics.Color.Black);
		}

		private void OnConnectClick(object sender, EventArgs e) {
			UpdateLog("正在连接设备...\n");
			_gatt = _device.ConnectGatt(this, false, new MyGattCallback(this));
		}

        // 显示服务和特征选择对话框
        private void ShowUUIDSelectionDialog() {
            if (_serviceCharacteristics.Count == 0) {
                UpdateLog("未发现可用服务和特征\n", Android.Graphics.Color.Red);
                return;
            }

            // 仅生成服务选项
            var options = new List<string>();
            foreach (var service in _serviceCharacteristics.Keys) {
                options.Add($"服务: {service}");
            }

            // 创建对话框
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("选择服务");
            builder.SetItems(options.ToArray(), (sender, e) => {
                var selectedService = options[e.Which];
                var serviceUUID = selectedService.Split(':').Last().Trim().ToUpper();

                var characteristics = _serviceCharacteristics[serviceUUID];

                // 强制检查ESP32的特征UUID是否存在
                var writeCharacteristicUUID = characteristics.Contains("6E400002-B5A3-F393-E0A9-E50E24DCCA9E")
                    ? "6E400002-B5A3-F393-E0A9-E50E24DCCA9E"
                    : null;

                var notifyCharacteristicUUID = characteristics.Contains("6E400003-B5A3-F393-E0A9-E50E24DCCA9E")
                    ? "6E400003-B5A3-F393-E0A9-E50E24DCCA9E"
                    : null;

                if (string.IsNullOrEmpty(writeCharacteristicUUID) || string.IsNullOrEmpty(notifyCharacteristicUUID)) {
                    UpdateLog("服务缺少必要的特征，请选择其他服务\n", Android.Graphics.Color.Red);
                    return;
                }

                ConnectToDevice(serviceUUID, writeCharacteristicUUID, notifyCharacteristicUUID);
            });

            // 在主线程显示对话框
            RunOnUiThread(() => builder.Show());
        }

        // 新增方法：传递三个参数（服务、写、通知UUID）
        private void ConnectToDevice(string serviceUUID, string writeUUID, string notifyUUID) {
            var intent = new Intent(this, typeof(BluetoothCommunicationActivity));
            intent.PutExtra("deviceAddress", _device.Address);
            intent.PutExtra("serviceUUID", serviceUUID);
            intent.PutExtra("writeUUID", writeUUID); // 写特征UUID
            intent.PutExtra("notifyUUID", notifyUUID); // 通知特征UUID
            StartActivity(intent);
        }

		// 自定义的BluetoothGattCallback类（内部类）
		private class MyGattCallback : BluetoothGattCallback {
			private readonly WeakReference<DeviceDetailActivity> _activityRef;

			public MyGattCallback(DeviceDetailActivity activity) {
				_activityRef = new WeakReference<DeviceDetailActivity>(activity);
			}

			public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState) {
				base.OnConnectionStateChange(gatt, status, newState);

				if (newState == ProfileState.Connected) {
					var activity = GetActivity();
					if (activity == null) return;

					activity.UpdateLog("连接成功！开始发现服务...\n");
					gatt.DiscoverServices();
				} else if (newState == ProfileState.Disconnected) {
					UpdateLog("连接断开\n");
				}
			}

			public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status) {
				base.OnServicesDiscovered(gatt, status);

				if (status == GattStatus.Success) {
                    UpdateLog("服务发现完成\n********************************\n", Android.Graphics.Color.Green);
                    UpdateLog($"发现 {gatt.Services.Count} 个服务\n", Android.Graphics.Color.Blue);

                    // 打印所有服务和特征的UUID
                    foreach (var gattService in gatt.Services) {
						// Replace the line with the error
						UpdateLog($"发现服务\n: {gattService.Uuid}\n\n");
						foreach (var characteristic in gattService.Characteristics) {
							UpdateLog($"Bluetooth\t特征: {characteristic.Uuid}\n");
						}
						UpdateLog($"--------------------------------\n");
					}

					var activity = GetActivity();
					if (activity == null) return;

					// 收集服务和特征的UUID
					activity._serviceCharacteristics.Clear();
					foreach (var service in gatt.Services) {
						var characteristics = new List<string>();
						foreach (var characteristic in service.Characteristics) {
							characteristics.Add(characteristic.Uuid.ToString().ToUpper());
						}
						if (characteristics.Any()) {
							activity._serviceCharacteristics.Add(service.Uuid.ToString().ToUpper(), characteristics);
						}
					}

                    // 显示选择对话框
                    //activity.ShowUUIDSelectionDialog();
                    // 在主线程调用 ShowUUIDSelectionDialog
                    activity.RunOnUiThread(() => activity.ShowUUIDSelectionDialog());
                } else {
					UpdateLog("服务发现失败\n\n", Android.Graphics.Color.Red);
				}
			}

            private void UpdateLog(string message, Android.Graphics.Color color = default) {
                var activity = GetActivity();
                if (activity != null) {
                    var actualColor = color == default ? Android.Graphics.Color.Black : color;
                    activity.UpdateLog(message, actualColor);
                }
            }

            private DeviceDetailActivity? GetActivity() {
				return _activityRef.TryGetTarget(out var activity) ? activity : null;
			}
		}

		protected override void OnDestroy() {
			base.OnDestroy();
			if (_gatt != null) {
				_gatt.Disconnect();
                _gatt.Close();
				_gatt.Dispose();
                _gatt = null;
            }
		}

		private Device GetDeviceFromIntent() {
			var intent = Intent;
			return new Device(
				intent.GetStringExtra("name") ?? "未知",
				intent.GetStringExtra("address") ?? "未知",
				intent.GetShortExtra("rssi", (short)-127)
			);
		}
	}
}
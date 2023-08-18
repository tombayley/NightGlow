using System.Runtime.InteropServices;

namespace NightGlow.WindowsApi.Types;

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct PowerBroadcastSetting(Guid PowerSettingId);
using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace ClassAlerts {
public static class Startup {
 const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
 public static string Command {get{return "\""+Assembly.GetExecutingAssembly().Location+"\"";}}
 public static bool Enabled {get{using(var key=Registry.CurrentUser.OpenSubKey(RunKey))return key!=null&&String.Equals(key.GetValue("ClassAlerts") as string,Command,StringComparison.OrdinalIgnoreCase);}}
 public static void SetEnabled(bool enabled){using(var key=Registry.CurrentUser.CreateSubKey(RunKey)){if(enabled)key.SetValue("ClassAlerts",Command,RegistryValueKind.String);else if(String.Equals(key.GetValue("ClassAlerts") as string,Command,StringComparison.OrdinalIgnoreCase))key.DeleteValue("ClassAlerts",false);}}
 public static string DataFolder {get{return File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"installed.flag"))?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ClassAlerts","UserData"):Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"UserData");}}
}
}

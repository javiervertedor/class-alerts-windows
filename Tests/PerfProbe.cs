using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
class PerfProbe {
 [STAThread]static void Main(string[] args){
  if(args.Length>4&&args[4]=="software")System.Windows.Media.RenderOptions.ProcessRenderMode=System.Windows.Interop.RenderMode.SoftwareOnly;
  var assembly=Assembly.LoadFrom(Path.GetFullPath(args[0]));var type=assembly.GetType("ClassAlerts.Dial");var dial=(FrameworkElement)Activator.CreateInstance(type);string mode=args[1];
  type.GetField("Mode").SetValue(dial,mode);type.GetField("Caption").SetValue(dial,"Computer Science: Algorithms and programming lesson");
  var theme=type.GetField("Theme").GetValue(dial);theme.GetType().GetProperty("LabelSize").SetValue(theme,22.0,null);
  var size=(Size)type.GetMethod("DesignSize").Invoke(null,new object[]{mode});dial.Width=size.Width;dial.Height=size.Height;
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var window=new Window{WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=System.Windows.Media.Brushes.Transparent,Content=dial,Width=size.Width+16,Height=size.Height+40,ShowActivated=false,ShowInTaskbar=false,Left=10,Top=10,Title="Class Alerts resource test"};
  var watch=Stopwatch.StartNew();var process=Process.GetCurrentProcess();double cpuStart=process.TotalProcessorTime.TotalSeconds;int sample=0;var lines=new System.Collections.Generic.List<string>();lines.Add("seconds,cpu_seconds,working_mb,private_mb,managed_mb,gen0,gen1,gen2");
  var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(200)};timer.Tick+=delegate{double elapsed=watch.Elapsed.TotalSeconds;type.GetField("Time").SetValue(dial,TimeSpan.FromSeconds(Math.Max(0,300-elapsed)).ToString(@"mm\:ss"));type.GetField("Fraction").SetValue(dial,Math.Max(0,1-elapsed/300));dial.InvalidateVisual();if(elapsed>=sample*10){process.Refresh();lines.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:F2},{1:F3},{2:F2},{3:F2},{4:F2},{5},{6},{7}",elapsed,process.TotalProcessorTime.TotalSeconds-cpuStart,process.WorkingSet64/1048576.0,process.PrivateMemorySize64/1048576.0,GC.GetTotalMemory(false)/1048576.0,GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)));sample++;}if(elapsed>=Double.Parse(args[3])){timer.Stop();File.WriteAllLines(args[2],lines);window.Close();app.Shutdown();}};
  window.Show();timer.Start();app.Run();
 }
}


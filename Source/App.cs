using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms=System.Windows.Forms;

namespace ClassAlerts {
public class Controller : IDisposable {
 public Preferences Prefs;public Store Store;public Sounds Sounds;public Overlay Overlay;
 SettingsWindow settings;Forms.NotifyIcon tray;DispatcherTimer timer;bool scheduled=true,paused,ended;double total=300,held;DateTime deadline,previous;string manualName="Manual timer";HashSet<string> fired=new HashSet<string>();DateTime ledgerDay;bool stopping;
 public Controller(string data,bool preview=false){
  Store=new Store(data);Prefs=Store.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"config.json"));Sounds=new Sounds(data);Sounds.PlaybackFailed+=message=>Notify("Could not play alert: "+message);Overlay=new Overlay();Overlay.Dial.Theme=Store.Clone(Prefs);Overlay.SetMode(Prefs.View,Prefs.Scale);
  if(Prefs.HasPosition){Overlay.Left=Prefs.Left;Overlay.Top=Prefs.Top;if(!VisiblePosition())Overlay.Recover();}
  Overlay.ToggleRequested+=Toggle;Overlay.Recovered+=delegate{Overlay.Recover();};
  Overlay.ContextMenu=MakeMenu();if(!preview)MakeTray();previous=DateTime.Now;ledgerDay=previous.Date;
  if(!preview)Overlay.Show();Update();timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(200)};timer.Tick+=delegate{Update();if(settings==null)Overlay.Raise();};if(!preview)timer.Start();
 }
 bool VisiblePosition(){var area=new Rect(SystemParameters.VirtualScreenLeft,SystemParameters.VirtualScreenTop,SystemParameters.VirtualScreenWidth,SystemParameters.VirtualScreenHeight);return area.IntersectsWith(new Rect(Overlay.Left,Overlay.Top,Overlay.Width,Overlay.Height));}
 MenuItem Item(string label,Action a){var item=new MenuItem{Header=label};item.Click+=delegate{a();};return item;}
 ContextMenu MakeMenu(){var menu=new ContextMenu();menu.Items.Add(Item("Settings…",OpenSettings));menu.Items.Add(new Separator());var views=new MenuItem{Header="Timer view"};foreach(string v in new[]{"Round","Bar","Numeric"}){string mode=v;views.Items.Add(Item(v,delegate{var p=Store.Clone(Prefs);p.View=mode;try{Apply(p);}catch(Exception ex){Notify(ex.Message);}}));}menu.Items.Add(views);menu.Items.Add(Item("Start a 5-minute timer",delegate{StartManual(5,"Manual timer");}));menu.Items.Add(Item("Pause / resume",delegate{string result=Pause();if(scheduled)Notify(result);}));menu.Items.Add(Item("Use lesson timetable",UseSchedule));menu.Items.Add(new Separator());menu.Items.Add(Item("Lock / click through",delegate{Overlay.ClickThrough=true;Overlay.ApplyStyle();Notify("Timer locked. Press Ctrl+Alt+O to recover it, or right-click the tray icon.");}));menu.Items.Add(Item("Hide timer",Toggle));menu.Items.Add(new Separator());menu.Items.Add(Item("Quit Class Alerts",Quit));return menu;}
 void MakeTray(){tray=new Forms.NotifyIcon{Text="Class Alerts — right-click for settings",Icon=System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location),Visible=true};var menu=new Forms.ContextMenuStrip();menu.Items.Add("Settings…",null,delegate{OpenSettings();});menu.Items.Add("Show / hide timer",null,delegate{Toggle();});menu.Items.Add("Recover & unlock timer",null,delegate{Overlay.Recover();});menu.Items.Add("Quit Class Alerts",null,delegate{Quit();});tray.ContextMenuStrip=menu;}
 void Notify(string text){if(tray!=null){tray.BalloonTipTitle="Class Alerts";tray.BalloonTipText=text;tray.ShowBalloonTip(5000);}}
 void OpenSettings(){if(settings!=null){settings.Activate();return;}settings=new SettingsWindow(this);settings.Closed+=delegate{settings=null;};settings.Show();settings.Activate();}
 public void Apply(Preferences p){Store.Check(p);Sounds.ValidateChoice(p.StartSound);Sounds.ValidateChoice(p.WarningSound);Sounds.ValidateChoice(p.EndSound);p.Left=Overlay.Left;p.Top=Overlay.Top;p.HasPosition=true;Store.Save(p);Prefs=Store.Clone(p);Overlay.Dial.Theme=Store.Clone(Prefs);Overlay.SetMode(Prefs.View,Prefs.Scale);previous=DateTime.Now;Update();}
 void Toggle(){if(Overlay.IsVisible)Overlay.Hide();else{Overlay.Show();Overlay.Raise();}}
 public void StartManual(double minutes,string name){Schedule.ValidateName(name);if(Double.IsNaN(minutes)||Double.IsInfinity(minutes)||minutes<=0||minutes>1440)throw new ArgumentException("Invalid timer duration.");scheduled=false;paused=false;ended=false;total=minutes*60;deadline=DateTime.UtcNow.AddSeconds(total);manualName=name;Overlay.Show();Play(Prefs.StartSound);Update();}
 public string Pause(){if(scheduled)return "Timetable mode follows the clock. Start a manual timer to pause it.";if(ended)return "This timer has finished.";if(paused){deadline=DateTime.UtcNow.AddSeconds(held);paused=false;}else{held=Math.Max(0,(deadline-DateTime.UtcNow).TotalSeconds);paused=true;}Update();return paused?"Timer paused.":"Timer resumed.";}
 public void UseSchedule(){scheduled=true;paused=false;previous=DateTime.Now;Overlay.Show();Update();}
 void Play(string id){try{Sounds.Play(id);}catch(Exception ex){Notify("Could not play alert: "+ex.Message);}}
 void Update(){DateTime now=DateTime.Now;Overlay.Dial.IsWarning=false;double remaining=0,fraction=0;string name,footer,time;
  if(scheduled){
   if(ledgerDay!=now.Date){fired.Clear();ledgerDay=now.Date;}
   var due=Schedule.Due(Prefs.Lessons,previous,now).Where(a=>fired.Add(a.Key)).ToList();
   // One sound at a time: a starting lesson takes priority at a shared end/start boundary.
   if(due.Count>0){var a=due.Last();Play(a.Kind=="start"?Prefs.StartSound:a.Kind=="warning"?Prefs.WarningSound:Prefs.EndSound);}
   var slots=Schedule.Slots(Prefs.Lessons,now);var slot=slots.FirstOrDefault(s=>s.Start<=now&&s.End>now);
   if(slot!=null){Overlay.Dial.IsWarning=Schedule.InWarning(slot,now);remaining=(slot.End-now).TotalSeconds;fraction=remaining/(slot.End-slot.Start).TotalSeconds;name=slot.Lesson.Name;footer="Ends "+slot.End.ToString("HH:mm");time=Schedule.Clock(remaining);}
   else{var next=slots.FirstOrDefault(s=>s.Start>now);name=next==null?"No lessons":"Next: "+next.Lesson.Name;footer=next==null?"Right-click to set up":next.Start.ToString("ddd HH:mm");time="—";fraction=1;}
  }else{remaining=paused?held:Math.Max(0,(deadline-DateTime.UtcNow).TotalSeconds);fraction=remaining/total;name=manualName;footer=paused?"Paused":remaining<=0?"Time is up":"Ends "+deadline.ToLocalTime().ToString("HH:mm");time=Schedule.Clock(remaining);if(remaining<=0&&!ended){ended=true;Play(Prefs.EndSound);}}
  previous=now;Overlay.Dial.Caption=name;Overlay.Dial.Time=time;Overlay.Dial.Footer=footer;Overlay.Dial.Fraction=Math.Max(0,Math.Min(1,fraction));Overlay.Dial.InvalidateVisual();Overlay.ToolTip=name+" · "+footer+"\nRight-click for settings";
 }
 public void Quit(){if(stopping)return;stopping=true;try{Prefs.Left=Overlay.Left;Prefs.Top=Overlay.Top;Prefs.HasPosition=true;Store.Save(Prefs);}catch(Exception ex){MessageBox.Show("Could not save the timer position: "+ex.Message,"Class Alerts");}Dispose();Application.Current.Shutdown();}
 public void Dispose(){if(timer!=null)timer.Stop();if(settings!=null)settings.Close();if(tray!=null){tray.Visible=false;tray.Dispose();tray=null;}Sounds.Dispose();Overlay.Close();}
}
public static class Program {
 [STAThread]public static void Main(string[] args){
  if(args.Contains("--uninstall-cleanup")){try{Uninstall.Run();}catch(Exception ex){MessageBox.Show("Class Alerts could not remove its user data. Close the app and retry uninstall.\n\n"+ex.Message,"Class Alerts");Environment.ExitCode=1;}return;}
  if(args.Contains("--enable-startup")||args.Contains("--disable-startup")){try{Startup.SetEnabled(args.Contains("--enable-startup"));}catch{Environment.ExitCode=1;}return;}
  if(args.Contains("--self-test")||args.Contains("--render-test")){try{var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};if(args.Contains("--self-test"))Tests.Run();else Tests.Render();}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-error.txt"),ex.ToString());Environment.ExitCode=1;}return;}
  bool created;using(var mutex=new Mutex(true,"Local\\ClassAlerts.Desktop.v2",out created)){if(!created){MessageBox.Show("Class Alerts is already running. Right-click its timer or tray icon for settings. Ctrl+Alt+O recovers the timer.","Class Alerts");return;}
   var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Controller controller=null;
   try{controller=new Controller(Startup.DataFolder);using(var stop=new EventWaitHandle(false,EventResetMode.AutoReset,Uninstall.StopEvent)){var registration=ThreadPool.RegisterWaitForSingleObject(stop,delegate{app.Dispatcher.BeginInvoke(new Action(delegate{controller.Quit();}));},null,Timeout.Infinite,true);try{app.Run();}finally{registration.Unregister(null);}}}
   catch(Exception ex){MessageBox.Show("Class Alerts could not continue. Your saved settings have not been reset.\n\n"+ex.Message,"Class Alerts");if(controller!=null)controller.Dispose();}
  }
 }
}
}







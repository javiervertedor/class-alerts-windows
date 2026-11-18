using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClassAlerts {
public static class Tests {
 static string Output {get{return AppDomain.CurrentDomain.BaseDirectory;}}
 static void Assert(bool value,string message){if(!value)throw new Exception(message);}
 static void Reject(Action action,string message){bool failed=false;try{action();}catch{failed=true;}Assert(failed,message);}
 static string Scratch(){string path=Path.Combine(Environment.GetEnvironmentVariable("CLASSALERTS_TEST_ROOT")??Path.GetFullPath(Path.Combine(Output,"..","build","tests")),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(path);return path;}
 public static void Run(){
  var d=new DateTime(2026,11,16,9,30,0);var lessons=new List<Lesson>{new Lesson{Day="Monday",Name="Test",Start="09:00",End="10:00",WarningMinutes=5}};
  Schedule.Validate(lessons);var slots=Schedule.Slots(lessons,d);Assert(slots.Count==2&&slots[0].End-d==TimeSpan.FromMinutes(30)&&slots[1].Start==new DateTime(2026,11,23,9,0,0),"Weekly recurrence");
  Assert(Schedule.Clock(-1)=="00:00"&&Schedule.Clock(60.1)=="01:01"&&Schedule.Clock(3600)=="1:00:00","Countdown formatting");
  Assert(Schedule.Due(lessons,d.Date.AddHours(8).AddMinutes(59),d.Date.AddHours(9)).Single().Kind=="start","Start alert");
  Assert(Schedule.Due(lessons,d.Date.AddHours(9).AddMinutes(54),d.Date.AddHours(9).AddMinutes(55)).Single().Kind=="warning","Warning alert");
  Assert(Schedule.Due(lessons,d.Date.AddHours(9).AddMinutes(59),d.Date.AddHours(10)).Single().Kind=="end","End alert");
  Assert(Schedule.Due(lessons,d.Date.AddHours(9),d.Date.AddHours(9)).Count==0,"No repeated tick alerts");
  Assert(Schedule.Due(lessons,d.Date.AddHours(8),d.Date.AddHours(11)).Count==0,"No stale alerts after sleep");
  Assert(Schedule.Due(lessons,d.Date.AddDays(7).AddHours(8).AddMinutes(59),d.Date.AddDays(7).AddHours(9)).Count==1,"Next-week alert");
  lessons.Add(new Lesson{Day="Monday",Name="Overlap",Start="09:30",End="10:30"});Reject(delegate{Schedule.Validate(lessons);},"Reject overlap");lessons.RemoveAt(1);
  lessons[0].Start="9am";Reject(delegate{Schedule.Validate(lessons);},"Reject malformed time");lessons[0].Start="09:00";lessons[0].WarningMinutes=60;Reject(delegate{Schedule.Validate(lessons);},"Reject warning outside lesson");lessons[0].WarningMinutes=0;Schedule.Validate(lessons);
  lessons.Add(new Lesson{Day="Monday",Name="Adjacent",Start="10:00",End="10:30"});Schedule.Validate(lessons);
  string root=Scratch();var store=new Store(root);var p=store.Load(Path.Combine(Output,"config.json"));Assert(p.Lessons.Count==0,"Fresh installation has an empty timetable");p.Lessons.Add(new Lesson{Day="Monday",Name="Test lesson",Start="09:00",End="09:45"});store.Save(p);p.View="Bar";p.Scale=0.8;store.Save(p);var saved=store.Load("");Assert(saved.View=="Bar"&&saved.Scale==0.8&&File.Exists(store.SettingsPath+".bak"),"Persist settings with backup");
  p.Lessons[0].End="00:00";Reject(delegate{store.Save(p);},"Invalid save blocked");Assert(store.Load("").Lessons[0].End!="00:00","Invalid save preserves file");
  using(var bank=new Sounds(root)){foreach(var id in Sounds.BuiltIns.Skip(1))Sounds.ValidateWave(bank.FileFor(id));string original=Path.Combine(root,"example.wav");File.Copy(bank.FileFor("Chime"),original);string imported=bank.Import(original);File.Delete(original);bank.ValidateChoice(imported);Assert(bank.Choices().Any(c=>c.Id==imported),"Custom sound copied and selectable");File.WriteAllText(original,"not a WAV");Reject(delegate{bank.Import(original);},"Reject non-WAV");Reject(delegate{bank.FileFor("../outside.wav");},"Reject sound path traversal");}
  var overlay=new Overlay();foreach(var mode in new[]{"Round","Bar","Numeric","Round"}){overlay.SetMode(mode,0.7);Assert(overlay.Width==Dial.DesignSize(mode).Width*0.7,"View resizing");}overlay.Close();
  using(var controller=new Controller(Scratch(),true)){var copy=Store.Clone(controller.Prefs);copy.View="Numeric";copy.Lessons=new List<Lesson>();controller.Apply(copy);Assert(controller.Overlay.Dial.Mode=="Numeric"&&controller.Overlay.Dial.Caption=="No lessons","Applying edits updates live view");Assert(!controller.Overlay.IsVisible,"No settings or overlay shown in headless validation");}
  RenderDials(Output);
  TestCustomisation();TestAppearanceAndDaySort();TestWarningAndBounds();
  File.WriteAllText(Path.Combine(Output,"test-results.txt"),"PASS: weekly recurrence; countdown formatting; start/warning/end boundaries; no repeated or stale alerts; next-week alerts; invalid time, overlap and warning rejection; adjacent lessons; empty initial timetable; persistence and backup; failed-save preservation; built-in WAVs; copied custom sounds; invalid audio/path rejection; view switching; live settings application; transparent corners in all three views.\r\nFull-screen compatibility previously confirmed by the user; new compact layouts require their visual acceptance test.");
 }
 static void TestCustomisation(){
  string root=Scratch();string file=Path.Combine(root,"template.csv");ScheduleCsv.WriteTemplate(file);var rows=ScheduleCsv.Read(file);Assert(rows.Count==3&&rows[0].Name.Contains("计算机")&&rows[1].Day=="Tuesday","UTF-8 Chinese CSV import");
  string header="Day,Name,Start,End,WarningMinutes,StartAlert,EndAlert\n"; Assert(ScheduleCsv.Parse(header+"Monday,X,9:00,9:45:00,5,true,true")[0].Start=="09:00","Excel time normalization");
  var quoted=ScheduleCsv.Parse(header+"周一,\"中文, \"\"ICT\"\"\",09:00,09:45,5,是,否\n");Assert(quoted[0].Name=="中文, \"ICT\""&&quoted[0].StartAlert&&!quoted[0].EndAlert,"Quoted commas/quotes and Chinese values");
  Reject(delegate{ScheduleCsv.Parse(header+"Monday,X,wrong,09:45,5,true,true");},"Invalid CSV time");Reject(delegate{ScheduleCsv.Parse(header+"Monday,X,09:00,09:45,5,maybe,true");},"Invalid CSV boolean");Reject(delegate{ScheduleCsv.Parse(header+"Monday,\"unfinished,09:00,09:45,5,true,true");},"Malformed CSV quote");Reject(delegate{ScheduleCsv.Parse(header+"Monday,A,09:00,09:45,5,true,true\nMonday,B,09:30,10:00,5,true,true");},"CSV overlaps");
  var old=new Store(root);File.WriteAllText(old.SettingsPath,"{\"Version\":2,\"View\":\"Round\",\"Scale\":1,\"Lessons\":[]}");var migrated=old.Load("");Assert(migrated.FontName=="Segoe UI"&&migrated.CountdownSize==35,"Old settings get new defaults");migrated.Bold=true;migrated.FontName="Microsoft YaHei";migrated.CountdownSize=38;migrated.LabelSize=14;migrated.TextColour="#FFDD66";migrated.FillColour="#243850";migrated.BorderColour="#FF88AA";migrated.Lessons=rows;old.Save(migrated);var reload=old.Load("");Assert(reload.Bold&&reload.FontName=="Microsoft YaHei"&&reload.LabelSize==14&&reload.TextColour=="#FFDD66"&&reload.Lessons[1].Name=="中文课","Chinese and appearance persistence");
  foreach(var mode in new[]{"Round","Bar","Numeric"}){var size=Dial.DesignSize(mode);var dial=new Dial{Theme=reload,Mode=mode,Caption="中文课 · ICT",Time="04:32",Footer="结束 10:04",Width=size.Width,Height=size.Height,Fraction=0.65};var image=Bitmap(dial,(int)size.Width,(int)size.Height);Save(image,Path.Combine(Output,"Chinese-"+mode+".png"));}
  File.WriteAllText(Path.Combine(Output,"customisation-tests.txt"),"PASS: Chinese UTF-8 CSV; quoted commas and escaped quotes; Chinese weekdays/booleans; invalid times/values/quotes/overlaps; old settings migration; font/size/colour and Chinese persistence. Chinese previews rendered for all views.");
 }
 static void TestAppearanceAndDaySort(){using(var controller=new Controller(Scratch(),true)){
  controller.Prefs.Lessons=Schedule.Days.Reverse().SelectMany(day=>new[]{new Lesson{Day=day,Name="Late",Start="10:00",End="10:45"},new Lesson{Day=day,Name="Early",Start="09:00",End="09:45"}}).ToList();
  var window=new SettingsWindow(controller);Assert(window.Tabs.Items.Count==5,"Font controls merged into Appearance");Assert(window.bold!=null,"Bold checkbox exists");window.bold.IsChecked=true;
  var grid=window.ScheduleTable;var sorted=grid.Items.Cast<Lesson>().ToArray();Assert(sorted[0].Day=="Monday"&&sorted[0].Start=="09:00"&&sorted[13].Day=="Sunday","Default Monday-first order");
  var descending=new System.Windows.Controls.DataGridSortingEventArgs(grid.Columns[0]);window.HandleSorting(grid,descending);sorted=grid.Items.Cast<Lesson>().ToArray();Assert(descending.Handled&&sorted[0].Day=="Sunday"&&sorted[0].Start=="09:00"&&sorted[13].Day=="Monday","Day header reverses week, not start times");
  for(int i=0;i<14;i+=2)Assert(sorted[i].Start=="09:00"&&sorted[i+1].Start=="10:00","Start-time secondary sort for every day");
  window.HandleSorting(grid,new System.Windows.Controls.DataGridSortingEventArgs(grid.Columns[0]));sorted=grid.Items.Cast<Lesson>().ToArray();Assert(sorted[0].Day=="Monday"&&sorted[13].Day=="Sunday","Second click restores Monday-first order");window.Close();
 }File.WriteAllText(Path.Combine(Output,"appearance-sort-tests.txt"),"PASS: combined Appearance tab; bold checkbox; bold preference persistence in customisation tests; Monday–Sunday and Sunday–Monday day-header toggles; ascending start-time ordering within every day.");}
 static void TestWarningAndBounds(){
  var start=new DateTime(2026,11,18,9,0,0);var lesson=new Lesson{WarningMinutes=5};var slot=new Slot{Lesson=lesson,Start=start,End=start.AddMinutes(45)};
  Assert(!Schedule.InWarning(slot,slot.End.AddMinutes(-5).AddSeconds(-1)),"Normal before warning");Assert(Schedule.InWarning(slot,slot.End.AddMinutes(-5)),"Warning at exact threshold");Assert(Schedule.InWarning(slot,slot.End.AddSeconds(-1)),"Warning until lesson end");Assert(!Schedule.InWarning(slot,slot.End),"Warning clears at end");lesson.WarningMinutes=0;Assert(!Schedule.InWarning(slot,slot.End.AddSeconds(-1)),"Zero disables warning");Assert(!Schedule.InWarning(null,start),"No lesson clears warning");
  var secondary=new Rect(-1920,0,1920,1040);var fitted=Overlay.FitToWorkArea(new Rect(-100,1000,445,106),secondary);Assert(secondary.Contains(fitted)&&fitted.Right==0&&fitted.Bottom==1040,"Bar clamped to negative-coordinate secondary monitor");var scaled=new Rect(1280,-200,1280,680);Assert(scaled.Contains(Overlay.FitToWorkArea(new Rect(2500,440,445,106),scaled)),"Offset scaled-coordinate work area");Assert(new Rect(0,0,100,80).Contains(Overlay.FitToWorkArea(new Rect(80,70,278,66),new Rect(0,0,100,80))),"Oversized timer fits small work area");
  var overlay=new Overlay();var area=SystemParameters.WorkArea;overlay.Left=area.Right-166;overlay.Top=area.Bottom-166;overlay.SetMode("Bar",1.6);Assert(overlay.Left+overlay.Width<=area.Right&&overlay.Top+overlay.Height<=area.Bottom,"Live view switch fits work area");overlay.Close();
  var store=new Store(Scratch());var prefs=store.Load(Path.Combine(Output,"config.json"));prefs.WarningColour="#FF3300";store.Save(prefs);Assert(store.Load("").WarningColour=="#FF3300","Warning colour persistence");
  foreach(var mode in new[]{"Round","Bar","Numeric"}){var size=Dial.DesignSize(mode);var dial=new Dial{Theme=prefs,IsWarning=true,Mode=mode,Time="04:32",Caption="G10 Computer Science",Footer="Ends 10:04",Fraction=0.12,Width=size.Width,Height=size.Height};Save(Bitmap(dial,(int)size.Width,(int)size.Height),Path.Combine(Output,"Warning-"+mode+".png"));}
  File.WriteAllText(Path.Combine(Output,"warning-bounds-tests.txt"),"PASS: warning threshold boundaries; zero disables warning; no-lesson reset; colour persistence; negative/offset monitor bounds; oversize fitting; live round-to-bar resize containment. Warning previews rendered in all views.");
 }
 static RenderTargetBitmap Bitmap(FrameworkElement element,int w,int h){element.Measure(new Size(w,h));element.Arrange(new Rect(0,0,w,h));element.UpdateLayout();var bmp=new RenderTargetBitmap(w,h,96,96,PixelFormats.Pbgra32);bmp.Render(element);return bmp;}
 static void Save(BitmapSource bmp,string path){var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(path))encoder.Save(f);}
 static void RenderDials(string folder){foreach(var mode in new[]{"Round","Bar","Numeric"}){var size=Dial.DesignSize(mode);var d=new Dial{Mode=mode,Time="04:32",Caption="G10 Computer Science",Footer="Ends 10:04",Fraction=0.65,Width=size.Width,Height=size.Height};var b=Bitmap(d,(int)size.Width,(int)size.Height);byte[] pixel=new byte[4];b.CopyPixels(new Int32Rect(0,0,1,1),pixel,4,0);Assert(pixel[3]==0,mode+" corner must be transparent");Save(b,Path.Combine(folder,mode+".png"));}}
 public static void Render(){RenderDials(Output);using(var controller=new Controller(Scratch(),true)){var settings=new SettingsWindow(controller);settings.Show();for(int i=0;i<5;i++){settings.Tabs.SelectedIndex=i;settings.UpdateLayout();Save(Bitmap(settings,(int)settings.ActualWidth,(int)settings.ActualHeight),Path.Combine(Output,new[]{"Settings","Timetable","Sounds","QuickTimer","About"}[i]+".png"));}settings.Close();}}
}
}









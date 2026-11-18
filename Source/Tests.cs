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
  System.Threading.SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext(Application.Current.Dispatcher));
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
  TestCustomisation();TestAppearanceAndDaySort();TestWarningAndBounds();TestSoundLibraryAndValidation();TestSaveRefreshAndCleanup();TestLongCaptions();TestManualWarning();
  File.WriteAllText(Path.Combine(Output,"test-results.txt"),"PASS: weekly recurrence; countdown formatting; start/warning/end boundaries; no repeated or stale alerts; next-week alerts; invalid time, overlap and warning rejection; adjacent lessons; empty initial timetable; persistence and backup; failed-save preservation; built-in WAVs; copied custom sounds; invalid audio/path rejection; view switching; live settings application; transparent corners in all three views.\r\nFull-screen compatibility previously confirmed by the user; new compact layouts require their visual acceptance test.");
 }
 static T Await<T>(System.Threading.Tasks.Task<T> task){if(!task.IsCompleted){var frame=new System.Windows.Threading.DispatcherFrame();task.ContinueWith(t=>Application.Current.Dispatcher.BeginInvoke(new Action(delegate{frame.Continue=false;})));System.Windows.Threading.Dispatcher.PushFrame(frame);}return task.GetAwaiter().GetResult();}
 static void TestSoundLibraryAndValidation(){
  foreach(var bad in new[]{"24:00","12:60","-1:00","9pm","09:00:01","", "09.30"})Reject(delegate{Schedule.NormalizeTime(bad,true);},"Reject invalid time: "+bad);
  Assert(Schedule.NormalizeTime(" 9:05 ",false)=="09:05","Typed time normalization");Reject(delegate{Schedule.NormalizeTime("09:05:00",false);},"Typed seconds rejected");
  var duplicate=new Lesson{Name="Duplicate",Start="09:00",End="09:45"};Reject(delegate{Schedule.Validate(new List<Lesson>{duplicate,duplicate});},"Duplicate lessons rejected");
  var existing=new List<Lesson>{new Lesson{Start="09:00",End="09:45"}};var imported=ScheduleCsv.Parse("Day,Name,Start,End,WarningMinutes,StartAlert,EndAlert\nMonday,Imported,09:30,10:00,5,true,true");Reject(delegate{Schedule.Validate(existing.Concat(imported).ToList());},"Append conflicts rejected");Assert(existing.Count==1,"Rejected append preserves existing rows");
  string root=Scratch();using(var bank=new Sounds(root)){
   string original=Path.Combine(root,"School_bell.wav");File.Copy(bank.FileFor("Chime"),original);
   string first=Await(bank.ImportAsync(original)),second=Await(bank.ImportAsync(original));Assert(first!=second&&bank.Choices().Single(c=>c.Id==first).Label=="School bell"&&bank.Choices().Single(c=>c.Id==second).Label=="School bell (2)","Readable unique import names");
   byte[] bytes=File.ReadAllBytes(original);File.Delete(original);Assert(bytes.SequenceEqual(File.ReadAllBytes(bank.FileFor(first))),"Copied bytes survive original deletion");bank.Rename(first,"中文铃声");using(var reload=new Sounds(root))Assert(reload.Choices().Single(c=>c.Id==first).Label=="中文铃声","Rename persists independently of file ID");
   Reject(delegate{bank.Rename(first," ");},"Blank name rejected");Reject(delegate{bank.Delete("Chime");},"Built-in deletion blocked");bank.Delete(first);Assert(!File.Exists(bank.FileFor(first))&&!bank.Choices().Any(c=>c.Id==first),"Deleted custom sound removed");
   int count=bank.Choices().Count;string corrupt=Path.Combine(root,"broken.mp3");File.WriteAllText(corrupt,"This is not audio");Reject(delegate{Await(bank.ImportAsync(corrupt));},"Corrupt compressed audio rejected");Assert(bank.Choices().Count==count,"Failed import leaves no library copy");
   string fixtures=Environment.GetEnvironmentVariable("CLASSALERTS_AUDIO_FIXTURES");if(!String.IsNullOrEmpty(fixtures)){foreach(var extension in Sounds.Extensions){string input=Path.Combine(fixtures,"tone"+extension);string id;try{id=Await(bank.ImportAsync(input));}catch(Exception ex){throw new Exception("Audio fixture "+extension+": "+ex.Message,ex);}Assert(File.ReadAllBytes(input).SequenceEqual(File.ReadAllBytes(bank.FileFor(id))),"Verified exact copy: "+extension);bank.Delete(id);}File.WriteAllText(Path.Combine(Output,"audio-format-tests.txt"),"PASS: actual WAV, MP3, WMA, M4A and AAC files decoded and copied; corrupt MP3 rejected with copy cleanup.");}
  }
  using(var controller=new Controller(Scratch(),true)){var window=new SettingsWindow(controller);var silent=(System.Windows.Controls.ListBoxItem)window.SoundLibrary.Items[0];var radios=((System.Windows.Controls.Grid)silent.Content).Children.OfType<System.Windows.Controls.RadioButton>().ToArray();foreach(var radio in radios)radio.IsChecked=true;Assert(window.Draft.StartSound=="Silent"&&window.Draft.WarningSound=="Silent"&&window.Draft.EndSound=="Silent","One sound assigned to all alert types");var chime=(System.Windows.Controls.ListBoxItem)window.SoundLibrary.Items[1];((System.Windows.Controls.Grid)chime.Content).Children.OfType<System.Windows.Controls.RadioButton>().First().IsChecked=true;Assert(window.Draft.StartSound=="Chime"&&window.Draft.WarningSound=="Silent"&&window.Draft.EndSound=="Silent"&&!radios[0].IsChecked.Value,"Radio columns are independent and exclusive");window.Close();Assert(controller.Prefs.WarningSound=="Soft bell","Cancel preserves saved sound assignments");}
  File.WriteAllText(Path.Combine(Output,"library-validation-tests.txt"),"PASS: friendly unique names; original-independent copies; persisted Chinese rename; deletion and built-in protection; corrupt import cleanup; strict times; normalization; duplicate and append conflict rejection.");
 }
 static void TestSaveRefreshAndCleanup(){
  string root=Scratch();string own=Path.Combine(root,"ClassAlerts","UserData","Sounds");Directory.CreateDirectory(own);File.WriteAllText(Path.Combine(own,"sound.wav"),"test");File.WriteAllText(Path.Combine(root,"keep.txt"),"keep");Uninstall.RemoveData(root);Assert(!Directory.Exists(Path.Combine(root,"ClassAlerts"))&&File.Exists(Path.Combine(root,"keep.txt")),"Uninstall removes only app data");Uninstall.RemoveData(root);
  using(var controller=new Controller(Scratch(),true)){
   string input=Path.Combine(controller.Store.Root,"school_bell.wav");File.Copy(controller.Sounds.FileFor("Chime"),input);string id=controller.Sounds.Import(input);
   var window=new SettingsWindow(controller);window.Show();window.Draft.StartSound=id;window.Draft.WarningSound=id;window.RefreshSounds(id);controller.Sounds.Rename(id,"Renamed bell");window.RefreshSounds(id);
   foreach(var target in new[]{"Silent",id,"Double bell",id}){
    var row=window.SoundLibrary.Items.Cast<System.Windows.Controls.ListBoxItem>().Single(x=>((SoundChoice)x.Tag).Id==target);
    ((System.Windows.Controls.Grid)row.Content).Children.OfType<System.Windows.Controls.RadioButton>().First().IsChecked=true;
    Assert(window.SoundLibrary.Items.Cast<System.Windows.Controls.ListBoxItem>().Count(x=>((System.Windows.Controls.Grid)x.Content).Children.OfType<System.Windows.Controls.RadioButton>().First().IsChecked==true)==1,"Exactly one start selection after rename/refresh");Assert(window.Draft.StartSound==target&&window.Draft.WarningSound==id,"Selection model agrees after refresh");
   }
   window.Save();Assert(window.IsVisible&&controller.Prefs.StartSound==id&&controller.Store.Load("").StartSound==id,"Save applies and persists without closing");window.Draft.EndSound=id;window.Save();Assert(controller.Prefs.EndSound==id,"Repeated Save applies changes");window.Draft.EndSound="Silent";window.Close();Assert(controller.Prefs.EndSound==id,"Close discards unsaved changes after Save");
  }
  File.WriteAllText(Path.Combine(Output,"save-cleanup-tests.txt"),"PASS: Save keeps settings open and applies/persists edits; repeated Save; Close discards unsaved edits; one checked sound per column after import/rename/list rebuild; isolated uninstall cleanup and idempotence.");
 }
 static void TestLongCaptions(){
  string fifty=new String('课',50);Schedule.ValidateName(fifty);Reject(delegate{Schedule.ValidateName(fifty+"课");},"51-character name rejected");Assert(Dial.LimitCaption(fifty+"课")==fifty,"Legacy display capped at 50 characters");Assert(Dial.LimitCaption(new String('a',49)+"😀x")==new String('a',49)+"😀","Caption cap preserves surrogate pairs");
  Reject(delegate{ScheduleCsv.Parse("Day,Name,Start,End,WarningMinutes,StartAlert,EndAlert\nMonday,"+fifty+"课,09:00,09:45,5,true,true");},"Long CSV name rejected");Assert(Dial.ScrollOffset(0,100)==0&&Dial.ScrollOffset(2,100)>0&&Dial.ScrollOffset(0,0)==0,"Scroll begins after pause and fitting text stays still");for(int i=0;i<200;i++){double offset=Dial.ScrollOffset(i*.3,100);Assert(offset>=0&&offset<=100,"Scroll stays within caption bounds");}
  foreach(var mode in new[]{"Round","Bar","Numeric"}){var size=Dial.DesignSize(mode);var dial=new Dial{Mode=mode,Caption="Computer Science: Algorithms and programming lesson",Time="04:32",Footer="Ends 10:04",Width=size.Width,Height=size.Height};dial.Theme.LabelSize=22;Save(Bitmap(dial,(int)size.Width,(int)size.Height),Path.Combine(Output,"LongCaption-"+mode+".png"));dial=new Dial{Mode=mode,Caption=fifty,Time="04:32",Footer="Ends 10:04",Width=size.Width,Height=size.Height};dial.Theme.LabelSize=22;Save(Bitmap(dial,(int)size.Width,(int)size.Height),Path.Combine(Output,"LongChinese-"+mode+".png"));}
  File.WriteAllText(Path.Combine(Output,"caption-tests.txt"),"PASS: 50-character boundary; CSV rejection; Unicode-safe display cap; scrolling pause/movement/bounds; long English and Chinese captions rendered in all views.");
 }
 static void TestManualWarning(){
  foreach(double value in new[]{-1,5,6,Double.NaN,Double.PositiveInfinity})Reject(delegate{new ManualWarning(5,value);},"Invalid manual warning rejected");
  foreach(double value in new[]{0,-1,1441,Double.NaN,Double.PositiveInfinity})Reject(delegate{new ManualWarning(value,0);},"Invalid manual duration rejected");
  var off=new ManualWarning(5,0);Assert(!off.IsWarning(1)&&!off.TakeWarning(1,false),"Zero disables colour and sound");
  var warning=new ManualWarning(5,1);Assert(!warning.IsWarning(60.1)&&warning.IsWarning(60)&&!warning.IsWarning(0),"Warning colour threshold and end");Assert(!warning.TakeWarning(61,false)&&!warning.TakeWarning(60,true)&&warning.TakeWarning(60,false)&&!warning.TakeWarning(59,false),"Warning fires once and waits while paused");
  var decimalWarning=new ManualWarning(.5,.1);Assert(decimalWarning.TakeWarning(6,false),"Fractional warning minutes");Assert(!new ManualWarning(5,1).TakeWarning(0,false),"Expired timer does not play late warning");Assert(new ManualWarning(5,1).TakeWarning(60,false),"New timer resets warning");
  File.WriteAllText(Path.Combine(Output,"manual-warning-tests.txt"),"PASS: zero disables warning; negative/equal/greater/non-finite rejection; fractional minutes; exact threshold; one-shot alert; pause/resume; no warning at expiry; restart reset.");
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









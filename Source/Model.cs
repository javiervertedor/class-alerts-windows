using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Media;
using System.Text;
using System.Web.Script.Serialization;

namespace ClassAlerts {
public class Lesson {
 public string Day {get;set;} public string Name {get;set;} public string Start {get;set;} public string End {get;set;}
 public bool StartAlert {get;set;} public bool EndAlert {get;set;} public int WarningMinutes {get;set;}
 public Lesson(){Day="Monday";Name="New lesson";Start="09:00";End="09:45";StartAlert=true;EndAlert=true;WarningMinutes=5;}
}
public class Preferences {
 public bool Bold {get;set;}
 public string FontName {get;set;} public double CountdownSize {get;set;} public double LabelSize {get;set;}
 public string WarningColour {get;set;} public string TextColour {get;set;} public string FillColour {get;set;} public string BorderColour {get;set;}
 public int Version {get;set;} public string View {get;set;} public double Scale {get;set;}
 public List<Lesson> Lessons {get;set;} public string StartSound {get;set;} public string WarningSound {get;set;} public string EndSound {get;set;}
 public double Left {get;set;} public double Top {get;set;} public bool HasPosition {get;set;}
 public Preferences(){Version=2;View="Round";Scale=1;FontName="Segoe UI";CountdownSize=35;LabelSize=11;WarningColour="#FFB454";TextColour="#FFFFFF";FillColour="#142235";BorderColour="#66E0CB";Lessons=new List<Lesson>();StartSound="Chime";WarningSound="Soft bell";EndSound="Double bell";}
}
public class Slot { public Lesson Lesson; public DateTime Start,End; }
public class DayLessonComparer : System.Collections.IComparer {
 readonly bool descending;public DayLessonComparer(bool descending){this.descending=descending;}
 public int Compare(object a,object b){var x=(Lesson)a;var y=(Lesson)b;int day=Array.IndexOf(Schedule.Days,x.Day).CompareTo(Array.IndexOf(Schedule.Days,y.Day));if(day!=0)return descending?-day:day;TimeSpan startX,startY;bool validX=TimeSpan.TryParse(x.Start,out startX),validY=TimeSpan.TryParse(y.Start,out startY);if(validX!=validY)return validX?-1:1;return validX?startX.CompareTo(startY):String.Compare(x.Start,y.Start,StringComparison.Ordinal);}
}
public class Alert { public string Kind; public DateTime At; public string Key; }
public static class Schedule {
 public static readonly string[] Days={"Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday"};
 public static TimeSpan Parse(string text){DateTime t;if(!DateTime.TryParseExact(text,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out t))throw new InvalidDataException("Use 24-hour times such as 08:10 or 15:45.");return t.TimeOfDay;}
 public static void Validate(List<Lesson> lessons){
  if(lessons==null)throw new InvalidDataException("The timetable is missing.");
  foreach(var l in lessons){if(l==null||!Days.Contains(l.Day)||String.IsNullOrWhiteSpace(l.Name))throw new InvalidDataException("Every lesson needs a day and a name.");
   var s=Parse(l.Start);var e=Parse(l.End);if(e<=s)throw new InvalidDataException(l.Name+": end time must be after start time on the same day.");
   if(l.WarningMinutes<0||l.WarningMinutes>=(e-s).TotalMinutes)throw new InvalidDataException(l.Name+": warning must be 0 (off) or shorter than the lesson.");}
  foreach(var day in Days){var rows=lessons.Where(l=>l.Day==day).OrderBy(l=>Parse(l.Start)).ToList();for(int i=1;i<rows.Count;i++)if(Parse(rows[i].Start)<Parse(rows[i-1].End))throw new InvalidDataException(day+": "+rows[i-1].Name+" overlaps "+rows[i].Name+".");}
 }
 public static List<Slot> Slots(List<Lesson> lessons,DateTime now){var result=new List<Slot>();for(int d=-1;d<=7;d++){var day=now.Date.AddDays(d);foreach(var l in lessons.Where(l=>l.Day==day.DayOfWeek.ToString()))result.Add(new Slot{Lesson=l,Start=day+Parse(l.Start),End=day+Parse(l.End)});}return result.OrderBy(s=>s.Start).ToList();}
 public static List<Alert> Due(List<Lesson> lessons,DateTime previous,DateTime now){
  var found=new List<Alert>();if(now<=previous)return found;
  foreach(var s in Slots(lessons,now)){
   if(s.Lesson.StartAlert)Add(found,s,"start",s.Start,previous,now);
   if(s.Lesson.WarningMinutes>0)Add(found,s,"warning",s.End.AddMinutes(-s.Lesson.WarningMinutes),previous,now);
   if(s.Lesson.EndAlert)Add(found,s,"end",s.End,previous,now);
  }return found.OrderBy(a=>a.At).ThenBy(a=>a.Kind=="start"?1:0).ToList();
 }
 static void Add(List<Alert> found,Slot s,string kind,DateTime at,DateTime previous,DateTime now){if(at>previous&&at<=now&&(now-at).TotalSeconds<=90)found.Add(new Alert{At=at,Kind=kind,Key=s.Start.ToString("O")+"|"+s.Lesson.Name+"|"+kind});}
 public static bool InWarning(Slot slot,DateTime now){return slot!=null&&slot.Lesson.WarningMinutes>0&&now>=slot.Start&&now<slot.End&&(slot.End-now).TotalSeconds<=slot.Lesson.WarningMinutes*60;}
 public static string Clock(double seconds){int n=(int)Math.Ceiling(Math.Max(0,seconds));return n>=3600?String.Format("{0}:{1:00}:{2:00}",n/3600,n/60%60,n%60):String.Format("{0:00}:{1:00}",n/60,n%60);}
}
public class Store {
 public readonly string Root; public string SettingsPath {get{return Path.Combine(Root,"settings.json");}}
 static JavaScriptSerializer Json(){return new JavaScriptSerializer();}
 public Store(string root){Root=root;Directory.CreateDirectory(root);}
 public static Preferences Clone(Preferences p){return Json().Deserialize<Preferences>(Json().Serialize(p));}
 public Preferences Load(string seed){
  if(File.Exists(SettingsPath)){var p=Json().Deserialize<Preferences>(File.ReadAllText(SettingsPath));Check(p);return p;}
  var value=new Preferences();
  if(File.Exists(seed)){
   var source=Json().Deserialize<Dictionary<string,object>>(File.ReadAllText(seed));var schedule=source["schedule"] as Dictionary<string,object>;
   foreach(var day in schedule)foreach(var raw in (System.Collections.IEnumerable)day.Value){var r=(Dictionary<string,object>)raw;var l=new Lesson{Day=day.Key,Name=(string)r["name"],Start=(string)r["start"],End=(string)r["end"]};
    if(r.ContainsKey("alerts")){var a=(Dictionary<string,object>)r["alerts"];l.StartAlert=a.ContainsKey("start")&&Convert.ToBoolean(a["start"]);l.EndAlert=a.ContainsKey("end")&&Convert.ToBoolean(a["end"]);l.WarningMinutes=a.ContainsKey("before_end")&&!(a["before_end"] is bool)?Convert.ToInt32(a["before_end"]):0;}value.Lessons.Add(l);}
  }Check(value);return value;
 }
 public static void Check(Preferences p){if(p==null||p.Version!=2)throw new InvalidDataException("Unsupported settings version.");Schedule.Validate(p.Lessons);if(!new[]{"Round","Bar","Numeric"}.Contains(p.View)||Double.IsNaN(p.Scale)||p.Scale<0.7||p.Scale>1.6)throw new InvalidDataException("Invalid appearance settings.");if(Double.IsNaN(p.Left)||Double.IsInfinity(p.Left)||Double.IsNaN(p.Top)||Double.IsInfinity(p.Top))throw new InvalidDataException("Invalid timer position.");if(String.IsNullOrWhiteSpace(p.FontName)||Double.IsNaN(p.CountdownSize)||p.CountdownSize<14||p.CountdownSize>60||Double.IsNaN(p.LabelSize)||p.LabelSize<8||p.LabelSize>22)throw new InvalidDataException("Choose a font, countdown size 14–60 and label size 8–22.");foreach(var colour in new[]{p.TextColour,p.FillColour,p.BorderColour,p.WarningColour})if(colour==null||!System.Text.RegularExpressions.Regex.IsMatch(colour,@"^#[0-9a-fA-F]{6}$"))throw new InvalidDataException("Colours must use #RRGGBB, for example #66E0CB.");}
 public void Save(Preferences p){Check(p);string temp=SettingsPath+".tmp";File.WriteAllText(temp,Json().Serialize(p),Encoding.UTF8);if(File.Exists(SettingsPath))File.Replace(temp,SettingsPath,SettingsPath+".bak");else File.Move(temp,SettingsPath);}
}
public class SoundChoice { public string Id {get;set;} public string Label {get;set;} public override string ToString(){return Label;} }
public class Sounds : IDisposable {
 readonly string folder; SoundPlayer player;
 public static readonly string[] BuiltIns={"Silent","Chime","Soft bell","Double bell"};
 public Sounds(string root){folder=Path.Combine(root,"Sounds");Directory.CreateDirectory(folder);for(int i=1;i<BuiltIns.Length;i++){string p=Path.Combine(folder,BuiltIns[i]+".wav");if(!File.Exists(p))Tone(p,i);}}
 public List<SoundChoice> Choices(){var list=BuiltIns.Select(s=>new SoundChoice{Id=s,Label=s}).ToList();foreach(var f in Directory.GetFiles(folder,"custom-*.wav"))list.Add(new SoundChoice{Id=Path.GetFileName(f),Label="Custom · "+Path.GetFileNameWithoutExtension(f).Substring(7)});return list;}
 public string FileFor(string id){if(id=="Silent")return null;if(BuiltIns.Contains(id))return Path.Combine(folder,id+".wav");if(id!=null&&id.StartsWith("custom-")&&Path.GetFileName(id)==id&&id.EndsWith(".wav",StringComparison.OrdinalIgnoreCase))return Path.Combine(folder,id);throw new InvalidDataException("Choose a sound from the list.");}
 public void ValidateChoice(string id){var p=FileFor(id);if(p!=null&&!File.Exists(p))throw new FileNotFoundException("The selected sound is missing. Choose another sound.",p);}
 public static void ValidateWave(string path){
  if(new FileInfo(path).Length>20*1024*1024)throw new InvalidDataException("Please choose a WAV file smaller than 20 MB.");
  using(var input=File.OpenRead(path))using(var r=new BinaryReader(input)){
   if(new string(r.ReadChars(4))!="RIFF")throw new InvalidDataException("Choose an uncompressed PCM WAV file.");r.ReadUInt32();if(new string(r.ReadChars(4))!="WAVE")throw new InvalidDataException("This file is not WAV audio.");
   bool format=false,data=false;while(input.Position+8<=input.Length){string tag=new string(r.ReadChars(4));uint len=r.ReadUInt32();long stop=input.Position+len;if(stop>input.Length)throw new InvalidDataException("The WAV file is truncated.");if(tag=="fmt "){if(len<16||r.ReadUInt16()!=1)throw new InvalidDataException("Use uncompressed PCM WAV audio.");ushort channels=r.ReadUInt16();uint hz=r.ReadUInt32();r.ReadUInt32();r.ReadUInt16();ushort bits=r.ReadUInt16();if(channels<1||channels>2||hz<8000||hz>192000||(bits!=8&&bits!=16))throw new InvalidDataException("Use mono/stereo 8-bit or 16-bit PCM WAV audio.");format=true;}if(tag=="data"&&len>0)data=true;input.Position=stop+(len%2);}
   if(!format||!data)throw new InvalidDataException("The WAV file has no supported audio data.");
  }
  using(var sound=new SoundPlayer(path))sound.Load();
 }
 public string Import(string path){ValidateWave(path);string name=Path.GetFileNameWithoutExtension(path);foreach(char c in Path.GetInvalidFileNameChars())name=name.Replace(c,'_');if(name.Length>50)name=name.Substring(0,50);string id="custom-"+name+"-"+Guid.NewGuid().ToString("N").Substring(0,6)+".wav";File.Copy(path,Path.Combine(folder,id));return id;}
 public void Play(string id){Stop();string p=FileFor(id);if(p==null)return;player=new SoundPlayer(p);player.Load();player.Play();}
 public void Stop(){if(player!=null){player.Stop();player.Dispose();player=null;}}
 public void Dispose(){Stop();}
 static void Tone(string path,int kind){int rate=22050;double duration=kind==3?1.05:0.7;int count=(int)(rate*duration);using(var w=new BinaryWriter(File.Create(path))){w.Write(Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(Encoding.ASCII.GetBytes("data"));w.Write(count*2);for(int i=0;i<count;i++){double t=(double)i/rate;double phase=kind==3&&t>=0.45?t-0.45:t;double hz=kind==2?660:kind==1?880:740;double envelope=Math.Min(1,phase*100)*Math.Exp(-phase*6);double wave=(Math.Sin(2*Math.PI*hz*phase)+0.25*Math.Sin(2*Math.PI*hz*2*phase))*envelope;w.Write((short)(wave*6500));}}}
}
}



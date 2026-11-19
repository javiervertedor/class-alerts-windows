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
public class ManualWarning {
 readonly double seconds;bool fired;
 public ManualWarning(double durationMinutes,double warningMinutes){
  if(Double.IsNaN(durationMinutes)||Double.IsInfinity(durationMinutes)||durationMinutes<=0||durationMinutes>1440)throw new ArgumentException("Enter a duration greater than 0 and no more than 1440 minutes.");
  if(Double.IsNaN(warningMinutes)||Double.IsInfinity(warningMinutes)||warningMinutes<0||warningMinutes>=durationMinutes)throw new ArgumentException("Warning minutes must be 0 (off) or greater than 0 and smaller than the duration.");
  seconds=warningMinutes*60;
 }
 public bool IsWarning(double remaining){return seconds>0&&remaining>0&&remaining<=seconds;}
 public bool TakeWarning(double remaining,bool paused){if(paused||fired||!IsWarning(remaining))return false;fired=true;return true;}
}
public class Slot { public Lesson Lesson; public DateTime Start,End; }
public class DayLessonComparer : System.Collections.IComparer {
 readonly bool descending;public DayLessonComparer(bool descending){this.descending=descending;}
 public int Compare(object a,object b){var x=(Lesson)a;var y=(Lesson)b;int day=Array.IndexOf(Schedule.Days,x.Day).CompareTo(Array.IndexOf(Schedule.Days,y.Day));if(day!=0)return descending?-day:day;TimeSpan startX,startY;bool validX=TimeSpan.TryParse(x.Start,out startX),validY=TimeSpan.TryParse(y.Start,out startY);if(validX!=validY)return validX?-1:1;return validX?startX.CompareTo(startY):String.Compare(x.Start,y.Start,StringComparison.Ordinal);}
}
public class Alert { public string Kind; public DateTime At; public string Key; }
public static class Schedule {
 public static readonly string[] Days={"Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday"};
 public static string NormalizeTime(string text,bool csv){DateTime value;string[] formats=csv?new[]{"H:mm","HH:mm","H:mm:ss","HH:mm:ss"}:new[]{"H:mm","HH:mm"};if(!DateTime.TryParseExact((text??"").Trim(),formats,CultureInfo.InvariantCulture,DateTimeStyles.None,out value)||value.Second!=0)throw new InvalidDataException("Use a 24-hour time such as 09:00 or 15:45 (00:00–23:59), with no seconds.");return value.ToString("HH:mm",CultureInfo.InvariantCulture);}
 public static TimeSpan Parse(string text){DateTime t;if(!DateTime.TryParseExact(text,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out t))throw new InvalidDataException("Use 24-hour times such as 08:10 or 15:45.");return t.TimeOfDay;}
 public static void ValidateName(string name){if(new StringInfo(name??"").LengthInTextElements>50)throw new InvalidDataException("Lesson and activity names must be no longer than 50 characters.");}
 public static void Validate(List<Lesson> lessons){
  if(lessons==null)throw new InvalidDataException("The timetable is missing.");
  foreach(var l in lessons){if(l==null||!Days.Contains(l.Day)||String.IsNullOrWhiteSpace(l.Name))throw new InvalidDataException("Every lesson needs a day and a name.");
   TimeSpan s,e;try{s=Parse(l.Start);}catch(InvalidDataException){throw new InvalidDataException(l.Day+" · "+l.Name+": invalid start time. Use HH:mm (00:00–23:59).");}try{e=Parse(l.End);}catch(InvalidDataException){throw new InvalidDataException(l.Day+" · "+l.Name+": invalid end time. Use HH:mm (00:00–23:59).");}if(e<=s)throw new InvalidDataException(l.Name+": end time must be after start time on the same day.");
   if(l.WarningMinutes<0||l.WarningMinutes>=(e-s).TotalMinutes)throw new InvalidDataException(l.Name+": warning must be 0 (off) or shorter than the lesson.");}
  foreach(var day in Days){var rows=lessons.Where(l=>l.Day==day).OrderBy(l=>Parse(l.Start)).ToList();for(int i=1;i<rows.Count;i++)if(Parse(rows[i].Start)<Parse(rows[i-1].End))throw new InvalidDataException(day+": "+rows[i-1].Name+" ("+rows[i-1].Start+"–"+rows[i-1].End+") overlaps "+rows[i].Name+" ("+rows[i].Start+"–"+rows[i].End+").");}
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
// Parsed once per calendar day or timetable edit, rather than on every UI tick.
public sealed class SchedulePlan {
 public readonly DateTime Date;public readonly List<Slot> Slots;readonly List<Alert> alerts=new List<Alert>();
 public SchedulePlan(List<Lesson> lessons,DateTime now){Date=now.Date;Slots=Schedule.Slots(lessons,now);foreach(var slot in Slots){if(slot.Lesson.StartAlert)Add(slot,"start",slot.Start);if(slot.Lesson.WarningMinutes>0)Add(slot,"warning",slot.End.AddMinutes(-slot.Lesson.WarningMinutes));if(slot.Lesson.EndAlert)Add(slot,"end",slot.End);}alerts=alerts.OrderBy(a=>a.At).ThenBy(a=>a.Kind=="start"?1:0).ToList();}
 void Add(Slot slot,string kind,DateTime at){alerts.Add(new Alert{At=at,Kind=kind,Key=slot.Start.ToString("O")+"|"+slot.Lesson.Name+"|"+kind});}
 public IEnumerable<Alert> Due(DateTime previous,DateTime now){if(now<=previous)yield break;foreach(var alert in alerts){if(alert.At>now)yield break;if(alert.At>previous&&(now-alert.At).TotalSeconds<=90)yield return alert;}}
 public Slot Current(DateTime now){foreach(var slot in Slots){if(slot.Start>now)break;if(slot.End>now)return slot;}return null;}
 public Slot Next(DateTime now){foreach(var slot in Slots)if(slot.Start>now)return slot;return null;}
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
}

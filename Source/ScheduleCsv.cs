using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace ClassAlerts {
public static class ScheduleCsv {
 public const string Template="Day,Name,Start,End,WarningMinutes,StartAlert,EndAlert\r\nMonday,计算机科学 Computer Science,09:00,09:45,5,true,true\r\n星期二,中文课,10:00,10:45,5,true,true\r\nWednesday,Art,11:00,11:40,0,false,true\r\n";
 public static void WriteTemplate(string path){File.WriteAllText(path,Template,new UTF8Encoding(true));}
 public static List<Lesson> Read(string path){if(new FileInfo(path).Length>2*1024*1024)throw new InvalidDataException("The CSV is too large (maximum 2 MB).");try{using(var reader=new StreamReader(path,new UTF8Encoding(false,true),true))return Parse(reader.ReadToEnd());}catch(DecoderFallbackException){throw new InvalidDataException("Save the file as CSV UTF-8 in Excel to preserve Chinese text.");}}
 public static List<Lesson> Parse(string text){
  var records=Records(text.TrimStart('\uFEFF'));if(records.Count<2)throw new InvalidDataException("The CSV needs a header row and at least one lesson.");
  var headers=records[0].Select(h=>h.Trim()).ToArray();var required=new[]{"Day","Name","Start","End","WarningMinutes","StartAlert","EndAlert"};
  if(headers.Length!=7||headers.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=7||required.Any(h=>!headers.Contains(h,StringComparer.OrdinalIgnoreCase)))throw new InvalidDataException("Use the template headers: "+String.Join(", ",required));
  var result=new List<Lesson>();for(int i=1;i<records.Count;i++){var row=records[i];try{if(row.Length!=headers.Length)throw new InvalidDataException("Expected seven columns. Put names containing commas inside double quotes.");Func<string,string> get=h=>row[Array.FindIndex(headers,x=>String.Equals(x,h,StringComparison.OrdinalIgnoreCase))].Trim();int warning;if(!Int32.TryParse(get("WarningMinutes"),out warning))throw new InvalidDataException("WarningMinutes must be a whole number.");var lesson=new Lesson{Day=Day(get("Day")),Name=get("Name"),Start=Time(get("Start")),End=Time(get("End")),WarningMinutes=warning,StartAlert=Bool(get("StartAlert")),EndAlert=Bool(get("EndAlert"))};Schedule.Validate(new List<Lesson>{lesson});result.Add(lesson);}catch(Exception ex){throw new InvalidDataException("CSV record "+(i+1)+": "+ex.Message);}}
  Schedule.Validate(result);return result;
 }
 static string Time(string value){DateTime time;if(!DateTime.TryParseExact(value,new[]{"H:mm","HH:mm","H:mm:ss","HH:mm:ss"},System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out time)||time.Second!=0)throw new InvalidDataException("Use 24-hour times such as 09:00, with no seconds.");return time.ToString("HH:mm",System.Globalization.CultureInfo.InvariantCulture);}
 static string Day(string value){for(int i=0;i<7;i++){string digit=new[]{"一","二","三","四","五","六","日"}[i];if(String.Equals(value,Schedule.Days[i],StringComparison.OrdinalIgnoreCase)||value=="星期"+digit||value=="周"+digit||(i==6&&(value=="星期天"||value=="周天")))return Schedule.Days[i];}throw new InvalidDataException("Day must be Monday–Sunday, 星期一–星期日 or 周一–周日.");}
 static bool Bool(string value){switch(value.ToLowerInvariant()){case "true":case "1":case "yes":case "是":return true;case "false":case "0":case "no":case "否":return false;default:throw new InvalidDataException("Alert values must be true/false, yes/no, 1/0 or 是/否.");}}
 // RFC-style quoted fields, escaped quotes, CRLF/LF and quoted newlines.
 static List<string[]> Records(string text){var records=new List<string[]>();var fields=new List<string>();var field=new StringBuilder();bool quoted=false,closed=false;
  for(int i=0;i<text.Length;i++){char c=text[i];if(quoted){if(c=='"'){if(i+1<text.Length&&text[i+1]=='"'){field.Append('"');i++;}else{quoted=false;closed=true;}}else field.Append(c);continue;}
   if(c=='"'){if(field.Length!=0||closed)throw new InvalidDataException("CSV contains a quote inside an unquoted field.");quoted=true;continue;}
   if(c==','||c=='\r'||c=='\n'){fields.Add(field.ToString());field.Clear();closed=false;if(c!=','){if(fields.Any(f=>!String.IsNullOrWhiteSpace(f)))records.Add(fields.ToArray());fields.Clear();if(c=='\r'&&i+1<text.Length&&text[i+1]=='\n')i++;}continue;}
   if(closed){if(c==' '||c=='\t')continue;throw new InvalidDataException("Unexpected text after a closing CSV quote.");}field.Append(c);
  }if(quoted)throw new InvalidDataException("CSV contains an unclosed quoted field.");if(field.Length>0||fields.Count>0||closed){fields.Add(field.ToString());if(fields.Any(f=>!String.IsNullOrWhiteSpace(f)))records.Add(fields.ToArray());}return records;
 }
}
}



using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Media;
using System.Windows.Threading;

namespace ClassAlerts {
public class SoundChoice { public string Id {get;set;} public string Label {get;set;} public override string ToString(){return Label;} }
public class Sounds : IDisposable {
 readonly string folder; SoundPlayer player; MediaPlayer media; DispatcherTimer openTimeout;
 Dictionary<string,string> names;
 public event Action<string> PlaybackFailed;
 public static readonly string[] BuiltIns={"Silent","Chime","Soft bell","Double bell"};
 public static readonly string[] Extensions={".wav",".mp3",".wma",".m4a",".aac"};
 string IndexPath {get{return Path.Combine(folder,"library.json");}}
 public Sounds(string root){
  folder=Path.Combine(root,"Sounds");Directory.CreateDirectory(folder);
  names=File.Exists(IndexPath)?new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(File.ReadAllText(IndexPath)):new Dictionary<string,string>();
  for(int i=1;i<BuiltIns.Length;i++){string p=Path.Combine(folder,BuiltIns[i]+".wav");if(!File.Exists(p))Tone(p,i);}
 }
 public List<SoundChoice> Choices(){return BuiltIns.Select(s=>new SoundChoice{Id=s,Label=s}).Concat(Directory.GetFiles(folder,"custom-*").Where(f=>Extensions.Contains(Path.GetExtension(f).ToLowerInvariant())).Select(f=>new SoundChoice{Id=Path.GetFileName(f),Label=LabelFor(Path.GetFileName(f))}).OrderBy(c=>c.Label,StringComparer.CurrentCultureIgnoreCase)).ToList();}
 string LabelFor(string id){string label;if(names.TryGetValue(id,out label))return label;return System.Text.RegularExpressions.Regex.Replace(Path.GetFileNameWithoutExtension(id).Substring(7),@"-[0-9a-f]{6}$","");}
 public bool IsCustom(string id){return id!=null&&id.StartsWith("custom-",StringComparison.Ordinal)&&Path.GetFileName(id)==id&&id.IndexOfAny(Path.GetInvalidFileNameChars())<0&&Extensions.Contains(Path.GetExtension(id).ToLowerInvariant());}
 public string FileFor(string id){if(id=="Silent")return null;if(BuiltIns.Contains(id))return Path.Combine(folder,id+".wav");if(IsCustom(id))return Path.Combine(folder,id);throw new InvalidDataException("Choose a sound from the library.");}
 public void ValidateChoice(string id){var path=FileFor(id);if(path!=null&&!File.Exists(path))throw new FileNotFoundException("The selected sound is missing. Import it again or choose another sound.");}
 void SaveNames(){string temp=IndexPath+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(names),Encoding.UTF8);if(File.Exists(IndexPath))File.Replace(temp,IndexPath,null);else File.Move(temp,IndexPath);}
 public void Rename(string id,string label){if(!IsCustom(id))throw new InvalidDataException("Built-in sounds cannot be renamed.");ValidateChoice(id);label=(label??"").Trim();if(label.Length==0||label.Length>80)throw new InvalidDataException("Enter a sound name of 1–80 characters.");string old;bool had=names.TryGetValue(id,out old);names[id]=label;try{SaveNames();}catch{if(had)names[id]=old;else names.Remove(id);throw;}}
 public void Delete(string id){if(!IsCustom(id))throw new InvalidDataException("Built-in sounds cannot be deleted.");Stop();File.Delete(FileFor(id));names.Remove(id);SaveNames();}
 string CopyIn(string path){
  string extension=Path.GetExtension(path).ToLowerInvariant();if(!Extensions.Contains(extension))throw new InvalidDataException("Choose WAV, MP3, WMA, M4A or AAC audio.");
  string id="custom-"+Guid.NewGuid().ToString("N")+extension;string destination=FileFor(id);
  try{using(var source=File.OpenRead(path)){if(source.Length==0||source.Length>20*1024*1024)throw new InvalidDataException("Choose a non-empty audio file no larger than 20 MB.");using(var output=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None))source.CopyTo(output);}return id;}
  catch{if(File.Exists(destination))File.Delete(destination);throw;}
 }
 string FinishImport(string id,string original){string label=Path.GetFileNameWithoutExtension(original).Replace('_',' ').Trim();if(label.Length==0)label="Imported sound";if(label.Length>70)label=label.Substring(0,70);string baseName=label;int suffix=2;var used=Choices().Where(c=>c.Id!=id).Select(c=>c.Label).ToList();while(used.Contains(label,StringComparer.CurrentCultureIgnoreCase))label=baseName+" ("+(suffix++)+")";Rename(id,label);return id;}
 // The PCM path remains available without a Windows codec or media initialization.
 public string Import(string path){string id=CopyIn(path);try{ValidateWave(FileFor(id));return FinishImport(id,path);}catch{File.Delete(FileFor(id));throw;}}
 public async Task<string> ImportAsync(string path){string id=CopyIn(path);try{bool pcm=false;if(Path.GetExtension(id)==".wav"){try{ValidateWave(FileFor(id));pcm=true;}catch(InvalidDataException){}catch(EndOfStreamException){}}if(!pcm)await ProbeAsync(FileFor(id));return FinishImport(id,path);}catch{File.Delete(FileFor(id));throw;}}
 static async Task ProbeAsync(string path){
  var probe=new MediaPlayer();var done=new TaskCompletionSource<bool>();var timeout=new DispatcherTimer{Interval=TimeSpan.FromSeconds(12)};
  probe.MediaOpened+=delegate{if(probe.HasAudio)done.TrySetResult(true);else done.TrySetException(new InvalidDataException("This file contains no playable audio."));};
  probe.MediaFailed+=delegate{done.TrySetException(new InvalidDataException("Windows could not decode this audio. Try another file or export it as PCM WAV or MP3."));};
  timeout.Tick+=delegate{done.TrySetException(new InvalidDataException("Audio validation timed out. Try another file or export it as PCM WAV."));};
  try{probe.Volume=0;timeout.Start();probe.Open(new Uri(Path.GetFullPath(path)));await done.Task;}finally{timeout.Stop();probe.Close();}
 }
 public void Play(string id){
  Stop();ValidateChoice(id);string path=FileFor(id);if(path==null)return;
  if(Path.GetExtension(path).Equals(".wav",StringComparison.OrdinalIgnoreCase)){bool pcm=false;try{ValidateWave(path);pcm=true;}catch(InvalidDataException){}catch(EndOfStreamException){}if(pcm){player=new SoundPlayer(path);player.Load();player.Play();return;}}
  var current=new MediaPlayer();media=current;openTimeout=new DispatcherTimer{Interval=TimeSpan.FromSeconds(12)};openTimeout.Tick+=delegate{Fail(current,"Opening the sound timed out.");};
  current.MediaOpened+=delegate{if(media!=current)return;openTimeout.Stop();if(!current.HasAudio){Fail(current,"This file contains no playable audio.");return;}current.Play();};
  current.MediaFailed+=delegate{Fail(current,"Windows could not play this sound. Import a different file or use PCM WAV or MP3.");};
  current.MediaEnded+=delegate{if(media==current)Stop();};current.Volume=1;openTimeout.Start();current.Open(new Uri(Path.GetFullPath(path)));
 }
 void Fail(MediaPlayer current,string message){if(media!=current)return;Stop();var handler=PlaybackFailed;if(handler!=null)handler(message);}
 public void Stop(){if(openTimeout!=null){openTimeout.Stop();openTimeout=null;}if(player!=null){player.Stop();player.Dispose();player=null;}if(media!=null){var current=media;media=null;current.Close();}}
 public void Dispose(){Stop();}
 public static void ValidateWave(string path){
  if(new FileInfo(path).Length>20*1024*1024)throw new InvalidDataException("Please choose a WAV file smaller than 20 MB.");
  using(var input=File.OpenRead(path))using(var r=new BinaryReader(input)){
   if(new string(r.ReadChars(4))!="RIFF")throw new InvalidDataException("Choose an uncompressed PCM WAV file.");r.ReadUInt32();if(new string(r.ReadChars(4))!="WAVE")throw new InvalidDataException("This file is not WAV audio.");
   bool format=false,data=false;while(input.Position+8<=input.Length){string tag=new string(r.ReadChars(4));uint len=r.ReadUInt32();long stop=input.Position+len;if(stop>input.Length)throw new InvalidDataException("The WAV file is truncated.");if(tag=="fmt "){if(len<16||r.ReadUInt16()!=1)throw new InvalidDataException("Use uncompressed PCM WAV audio.");ushort channels=r.ReadUInt16();uint hz=r.ReadUInt32();r.ReadUInt32();r.ReadUInt16();ushort bits=r.ReadUInt16();if(channels<1||channels>2||hz<8000||hz>192000||(bits!=8&&bits!=16))throw new InvalidDataException("Use mono/stereo 8-bit or 16-bit PCM WAV audio.");format=true;}if(tag=="data"&&len>0)data=true;input.Position=stop+(len%2);}
   if(!format||!data)throw new InvalidDataException("The WAV file has no supported audio data.");
  }
  using(var sound=new SoundPlayer(path))sound.Load();
 }
 static void Tone(string path,int kind){int rate=22050;double duration=kind==3?1.05:0.7;int count=(int)(rate*duration);using(var w=new BinaryWriter(File.Create(path))){w.Write(Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(Encoding.ASCII.GetBytes("data"));w.Write(count*2);for(int i=0;i<count;i++){double t=(double)i/rate;double phase=kind==3&&t>=0.45?t-0.45:t;double hz=kind==2?660:kind==1?880:740;double envelope=Math.Min(1,phase*100)*Math.Exp(-phase*6);double wave=(Math.Sin(2*Math.PI*hz*phase)+0.25*Math.Sin(2*Math.PI*hz*2*phase))*envelope;w.Write((short)(wave*6500));}}}
}
}

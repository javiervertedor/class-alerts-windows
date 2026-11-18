using System;
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace ClassAlerts {
public static class Uninstall {
 public const string StopEvent="Local\\ClassAlerts.Uninstall.Stop";
 public static void Run(){
  // Finish shutdown before removing data so a running timer cannot recreate it.
  using(var mutex=new Mutex(false,"Local\\ClassAlerts.Desktop.v2")){
   bool acquired=false;
   try{
    try{acquired=mutex.WaitOne(0);}catch(AbandonedMutexException){acquired=true;}
    if(!acquired){try{using(var signal=EventWaitHandle.OpenExisting(StopEvent))signal.Set();}catch(WaitHandleCannotBeOpenedException){throw new IOException("A previous version is running. Quit Class Alerts from its tray icon first.");}
     try{acquired=mutex.WaitOne(TimeSpan.FromSeconds(15));}catch(AbandonedMutexException){acquired=true;}
     if(!acquired)throw new IOException("The timer is still running.");
    }
    Startup.SetEnabled(false);
    RemoveData(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
    using(var parent=Registry.CurrentUser.OpenSubKey(@"Software\FranciscoJavierVertedorPostigo",true)){
     if(parent!=null)parent.DeleteSubKeyTree("ClassAlerts",false);
    }
    using(var software=Registry.CurrentUser.OpenSubKey("Software",true)){
     using(var parent=software.OpenSubKey("FranciscoJavierVertedorPostigo")){
      if(parent!=null&&(parent.SubKeyCount!=0||parent.ValueCount!=0))return;
     }
     software.DeleteSubKey("FranciscoJavierVertedorPostigo",false);
    }
   }finally{if(acquired)mutex.ReleaseMutex();}
  }
 }
 internal static void RemoveData(string localAppData){
  if(String.IsNullOrWhiteSpace(localAppData))throw new IOException("User data location is unavailable.");
  string parent=Path.GetFullPath(localAppData).TrimEnd(Path.DirectorySeparatorChar);
  string target=Path.GetFullPath(Path.Combine(parent,"ClassAlerts"));
  if(!String.Equals(Path.GetDirectoryName(target),parent,StringComparison.OrdinalIgnoreCase)||Path.GetFileName(target)!="ClassAlerts")throw new IOException("Unexpected user data location.");
  if(Directory.Exists(target))RemoveTree(target);
 }
 static void RemoveTree(string path){
  // Never follow junctions/symbolic links outside the app's own data directory.
  if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0){Directory.Delete(path);return;}
  foreach(string file in Directory.GetFiles(path)){File.Delete(file);}
  foreach(string directory in Directory.GetDirectories(path))RemoveTree(directory);
  Directory.Delete(path);
 }
}
}

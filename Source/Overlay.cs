using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace ClassAlerts {
public class Dial : FrameworkElement {
 public string Mode="Round",Caption="Manual timer",Time="05:00",Footer="Ends 10:04";public double Fraction=1;
 public Preferences Theme=new Preferences();
 public bool IsWarning;
 public static Brush Brush(string hex){return (Brush)new BrushConverter().ConvertFromString(hex);}
 public static Size DesignSize(string mode){return mode=="Round"?new Size(166,166):mode=="Bar"?new Size(278,66):new Size(170,112);}
 readonly System.Diagnostics.Stopwatch scrollClock=System.Diagnostics.Stopwatch.StartNew();string scrollKey;bool scrolling;
 public Dial(){Loaded+=delegate{CompositionTarget.Rendering+=Animate;};Unloaded+=delegate{CompositionTarget.Rendering-=Animate;};}
 void Animate(object sender,EventArgs args){if(scrolling&&IsVisible)InvalidateVisual();}
 public static string LimitCaption(string text){text=text??"";var info=new StringInfo(text);return info.LengthInTextElements<=50?text:info.SubstringByTextElements(0,50);}
 FormattedText LabelText(string text,double size,Brush colour){return new FormattedText(text??"",CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface(new FontFamily(Theme.FontName+", Microsoft YaHei, Microsoft JhengHei, Global User Interface"),FontStyles.Normal,Theme.Bold?FontWeights.Bold:FontWeights.Normal,FontStretches.Normal),size,colour,1.0);}
 internal static double ScrollOffset(double elapsed,double overflow){if(overflow<=0)return 0;double travel=overflow/24.0;double phase=elapsed%(travel*2+3);if(phase<1.5)return 0;if(phase<1.5+travel)return (phase-1.5)*24;if(phase<3+travel)return overflow;return Math.Max(0,overflow-(phase-3-travel)*24);}
 string WrapCaption(string text,double size,double width,Brush colour){var lines=new System.Text.StringBuilder();string line="";var elements=StringInfo.GetTextElementEnumerator(text);while(elements.MoveNext()){string next=elements.GetTextElement();if(line.Length>0&&LabelText(line+next,size,colour).WidthIncludingTrailingWhitespace>width){int space=line.LastIndexOf(' ');if(space>0){lines.AppendLine(line.Substring(0,space));line=line.Substring(space+1);}else{lines.AppendLine(line);line="";}}line+=next;}lines.Append(line);return lines.ToString();}
 void CaptionText(DrawingContext dc,string text,Brush colour,Rect box,bool wrap){
  text=LimitCaption(text);var f=LabelText(text,Theme.LabelSize,colour);
  if(wrap){double size=Theme.LabelSize;do{f=LabelText(WrapCaption(text,size,box.Width-4,colour),size,colour);f.MaxTextWidth=box.Width;f.TextAlignment=TextAlignment.Center;if(f.Height<=box.Height-3||size<=6)break;size-=0.5;}while(true);dc.PushClip(new RectangleGeometry(box));var geometry=f.BuildGeometry(new Point(box.Left,box.Top+1));dc.DrawGeometry(null,new Pen(Brush(Theme.BorderColour),2.4){LineJoin=PenLineJoin.Round},geometry);dc.DrawGeometry(colour,null,geometry);dc.Pop();return;}
  if(f.Height>box.Height)f.SetFontSize(Theme.LabelSize*box.Height/f.Height);
  double overflow=Math.Max(0,f.WidthIncludingTrailingWhitespace-box.Width);scrolling|=overflow>0;
  double x=overflow>0?box.Left-ScrollOffset(scrollClock.Elapsed.TotalSeconds,overflow):box.Left+(box.Width-f.WidthIncludingTrailingWhitespace)/2;
  dc.PushClip(new RectangleGeometry(box));dc.DrawText(f,new Point(x,box.Top));dc.Pop();
 }
 void Text(DrawingContext dc,string text,double size,Brush color,Rect box,bool outline){
  var face=new Typeface(new FontFamily(Theme.FontName+", Microsoft YaHei, Microsoft JhengHei, Global User Interface"),FontStyles.Normal,Theme.Bold?FontWeights.Bold:FontWeights.Normal,FontStretches.Normal);
  bool digits=text==Time;size=digits?Theme.CountdownSize:Theme.LabelSize;
  var f=new FormattedText(text??"",CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,face,size,color,1.0);
  double fit=Math.Min(1,box.Height/Math.Max(1,f.Height));if(digits)fit=Math.Min(fit,box.Width/Math.Max(1,f.WidthIncludingTrailingWhitespace));if(fit<1)f.SetFontSize(size*fit);
  f.MaxTextWidth=box.Width;f.MaxLineCount=1;f.TextAlignment=TextAlignment.Center;f.Trimming=TextTrimming.CharacterEllipsis;
  if(outline){var geometry=f.BuildGeometry(box.TopLeft);dc.DrawGeometry(null,new Pen(Brush(Theme.BorderColour),2.4){LineJoin=PenLineJoin.Round},geometry);dc.DrawGeometry(color,null,geometry);}else dc.DrawText(f,box.TopLeft);
 }
 protected override void OnRender(DrawingContext dc){
  string key=Mode+"|"+Caption+"|"+Footer+"|"+Theme.FontName+"|"+Theme.LabelSize+"|"+Theme.Bold;if(key!=scrollKey){scrollKey=key;scrollClock.Restart();}scrolling=false;
  double w=ActualWidth,h=ActualHeight;Brush accent=Brush(IsWarning?Theme.WarningColour:Theme.BorderColour),white=Brush(Theme.TextColour),muted=white,digits=Brush(IsWarning?Theme.WarningColour:Theme.TextColour);var track=accent.Clone();track.Opacity=0.25;
  if(Mode=="Round"){
   var c=new Point(w/2,h/2);double r=w/2-5;dc.DrawEllipse(Brush(Theme.FillColour),null,c,r,r);dc.DrawEllipse(null,new Pen(track,4),c,r,r);
   if(Fraction>=0.99999)dc.DrawEllipse(null,new Pen(accent,8),c,r,r);else if(Fraction>0){double a=Fraction*Math.PI*2;var g=new StreamGeometry();using(var ctx=g.Open()){ctx.BeginFigure(new Point(c.X,c.Y-r),false,false);ctx.ArcTo(new Point(c.X+r*Math.Sin(a),c.Y-r*Math.Cos(a)),new Size(r,r),0,Fraction>0.5,SweepDirection.Clockwise,true,false);}dc.DrawGeometry(null,new Pen(accent,8){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},g);}
   CaptionText(dc,Caption,muted,new Rect(25,35,w-50,29),false);
   Text(dc,Time,Time.Length>5?28:35,digits,new Rect(12,68,w-24,44),false);
   CaptionText(dc,Footer,muted,new Rect(23,116,w-46,20),false);
  }else if(Mode=="Bar"){
   var shape=new RectangleGeometry(new Rect(2,2,w-4,h-4),13,13);dc.DrawGeometry(Brush(Theme.FillColour),null,shape);
   dc.PushClip(shape);dc.DrawRectangle(track,null,new Rect(2,2,(w-4)*Math.Max(0,Math.Min(1,Fraction)),h-4));dc.Pop();dc.DrawGeometry(null,new Pen(accent,1),shape);
   Text(dc,Time,Time.Length>5?23:30,digits,new Rect(10,14,102,40),false);
   CaptionText(dc,Caption,white,new Rect(116,13,w-128,27),false);CaptionText(dc,Footer,muted,new Rect(116,43,w-128,16),false);
  }else{
   Text(dc,Time,Time.Length>5?33:40,digits,new Rect(3,0,w-6,50),true);
   CaptionText(dc,Caption,white,new Rect(3,51,w-6,h-54),true);
  }
 }
}
public class Overlay : Window {
 public Dial Dial=new Dial();public bool ClickThrough;IntPtr handle;
 public event Action ToggleRequested;public event Action Recovered;
 [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr h,int i);
 [DllImport("user32.dll")]static extern int SetWindowLong(IntPtr h,int i,int value);
 [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int z,uint flags);
 [DllImport("user32.dll")]static extern bool RegisterHotKey(IntPtr h,int id,uint mods,uint key);
 [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr h,int id);
 [StructLayout(LayoutKind.Sequential)]struct NativeRect {public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)]struct MonitorInfo {public int Size;public NativeRect Monitor,Work;public uint Flags;}
 [DllImport("user32.dll")]static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
 [DllImport("user32.dll",CharSet=CharSet.Auto)]static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
 Rect WorkArea(){if(handle==IntPtr.Zero)return SystemParameters.WorkArea;var info=new MonitorInfo{Size=Marshal.SizeOf(typeof(MonitorInfo))};if(!GetMonitorInfo(MonitorFromWindow(handle,2),ref info))return SystemParameters.WorkArea;var source=HwndSource.FromHwnd(handle);var transform=source!=null&&source.CompositionTarget!=null?source.CompositionTarget.TransformFromDevice:Matrix.Identity;var first=transform.Transform(new Point(info.Work.Left,info.Work.Top));var last=transform.Transform(new Point(info.Work.Right,info.Work.Bottom));return new Rect(first,last);}
 public static Rect FitToWorkArea(Rect desired,Rect area){double fit=Math.Min(1,Math.Min(area.Width/desired.Width,area.Height/desired.Height));double width=desired.Width*fit,height=desired.Height*fit;return new Rect(Math.Max(area.Left,Math.Min(desired.Left,area.Right-width)),Math.Max(area.Top,Math.Min(desired.Top,area.Bottom-height)),width,height);}
 void KeepOnScreen(Rect area){double x=Double.IsNaN(Left)?area.Right-Width-20:Left,y=Double.IsNaN(Top)?area.Top+20:Top;var bounds=FitToWorkArea(new Rect(x,y,Width,Height),area);Left=bounds.Left;Top=bounds.Top;Width=bounds.Width;Height=bounds.Height;}
 public Overlay(){Title="Class Alerts • Timer";WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowActivated=false;ShowInTaskbar=false;
  Content=new Viewbox{Child=Dial};SetMode("Round",1);Left=SystemParameters.WorkArea.Right-Width-20;Top=SystemParameters.WorkArea.Top+20;
  MouseLeftButtonDown+=delegate{if(!ClickThrough)DragMove();};
  SourceInitialized+=delegate{handle=new WindowInteropHelper(this).Handle;ApplyStyle();KeepOnScreen(WorkArea());HwndSource.FromHwnd(handle).AddHook(Hook);RegisterHotKey(handle,1,0x4003,0x54);RegisterHotKey(handle,2,0x4003,0x4F);};
  Closed+=delegate{UnregisterHotKey(handle,1);UnregisterHotKey(handle,2);};
 }
 IntPtr Hook(IntPtr h,int message,IntPtr w,IntPtr l,ref bool handled){if(message==0x312){if(w.ToInt32()==1&&ToggleRequested!=null)ToggleRequested();if(w.ToInt32()==2&&Recovered!=null)Recovered();handled=true;}return IntPtr.Zero;}
 public void SetMode(string mode,double scale){var area=WorkArea();Dial.Mode=mode;var s=Dial.DesignSize(mode);Dial.Width=s.Width;Dial.Height=s.Height;Width=s.Width*scale;Height=s.Height*scale;KeepOnScreen(area);Dial.InvalidateVisual();}
 public void ApplyStyle(){if(handle==IntPtr.Zero)return;int style=GetWindowLong(handle,-20)|0x08000000|0x80;style=ClickThrough?style|0x20:style&~0x20;SetWindowLong(handle,-20,style);}
 public void Raise(){if(IsVisible&&handle!=IntPtr.Zero&&(ContextMenu==null||!ContextMenu.IsOpen))SetWindowPos(handle,new IntPtr(-1),0,0,0,0,0x13);}
 public void Recover(){ClickThrough=false;ApplyStyle();Left=SystemParameters.WorkArea.Right-Width-20;Top=SystemParameters.WorkArea.Top+20;Show();Raise();}
}
}




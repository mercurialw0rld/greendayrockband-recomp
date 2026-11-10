using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GreenDayLauncher {
 public static class Aero {
  public static readonly Color Ink=Color.FromArgb(245,239,231),Muted=Color.FromArgb(183,176,166),
   Canvas=Color.FromArgb(24,22,22),Surface=Color.FromArgb(35,32,32),Raised=Color.FromArgb(46,41,40),
   Edge=Color.FromArgb(83,72,68),Accent=Color.FromArgb(255,180,83),
   Ready=Color.FromArgb(153,218,159),Warning=Color.FromArgb(255,157,133);
  public static readonly Color[] FretColors={Color.FromArgb(120,211,111),Color.FromArgb(248,125,114),Color.FromArgb(248,211,105),Color.FromArgb(117,185,242),Color.FromArgb(250,167,99)};
  public static Font Display(float size){return UI(size,FontStyle.Bold);}
  public static Font UI(float size,FontStyle style=FontStyle.Regular){return new Font("Segoe UI",size*96f/72,style,GraphicsUnit.Pixel);}
  [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)]
  internal static extern int SetWindowTheme(IntPtr window,string theme,string classes);
  public static GraphicsPath Round(RectangleF r,float radius) {
   var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));
   p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
  }
  public static Color Mix(Color a,Color b,float t){return Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));}
  public static void Text(Graphics g,string value,Font font,Color color,RectangleF bounds,StringAlignment align=StringAlignment.Near) {
   using(var brush=new SolidBrush(color))using(var format=new StringFormat {Alignment=align,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter})g.DrawString(value,font,brush,bounds,format);
  }
  public static Label Label(Control parent,string value,Rectangle box,Font font,Color color) {
   var result=new AeroLabel {Text=value,Bounds=box,Font=font,ForeColor=color,AutoSize=false};parent.Controls.Add(result);return result;
  }
  static Point Origin(Control control,Control root){var point=Point.Empty;for(var p=control;p!=null&&p!=root;p=p.Parent)point.Offset(p.Left,p.Top);return point;}
  // Paint only the background layers. Asking WinForms to simulate transparency
  // on a Button/CheckBox replays sibling controls on the live Windows surface.
  public static void PaintBackground(Control control,Graphics graphics) {
   var state=graphics.Save();
   try {
    var window=control.FindForm() as AeroWindow;
    if(window==null){graphics.Clear(Canvas);return;}
    var origin=Origin(control,window);graphics.TranslateTransform(-origin.X,-origin.Y);window.DrawBackdrop(graphics);
    var parents=new System.Collections.Generic.List<Card>();
    for(var p=control.Parent;p!=null&&p!=window;p=p.Parent){var card=p as Card;if(card!=null)parents.Add(card);}
    parents.Reverse();foreach(var card in parents){var local=graphics.Save();var offset=Origin(card,window);graphics.TranslateTransform(offset.X,offset.Y);card.DrawSurface(graphics);graphics.Restore(local);}
   } finally {graphics.Restore(state);}
  }
  public static AeroButton Button(string value,Rectangle box,bool primary=false){return new AeroButton {Text=value,Bounds=box,Primary=primary,Font=UI(10,FontStyle.Bold)};}
  public static void DrawPanel(Graphics g,RectangleF bounds,Color fill,Color edge,float radius=6) {
   if(bounds.Width<=0||bounds.Height<=0)return;
   using(var path=Round(bounds,radius))
   using(var brush=new SolidBrush(fill))
   using(var pen=new Pen(edge)){g.FillPath(brush,path);g.DrawPath(pen,path);}
  }
 }
 public class AeroWindow : Form {
  [DllImport("user32.dll")]static extern bool ReleaseCapture();
  [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int message,IntPtr w,IntPtr l);
  public AeroWindow(){SetStyle(ControlStyles.ResizeRedraw|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);DoubleBuffered=true;BackColor=Aero.Canvas;ForeColor=Aero.Ink;Font=Aero.UI(10);FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;}
  protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ClassStyle|=0x20000;return p;}}
  public void WindowButtons(bool minimize) {
   var close=new AeroButton {Bounds=new Rectangle(ClientSize.Width-48,14,30,28),Chrome=2,TabStop=false,AccessibleName="Close",Anchor=AnchorStyles.Top|AnchorStyles.Right};close.Click+=delegate{Close();};Controls.Add(close);
   if(minimize){var min=new AeroButton {Bounds=new Rectangle(ClientSize.Width-83,14,30,28),Chrome=1,TabStop=false,AccessibleName="Minimize",Anchor=AnchorStyles.Top|AnchorStyles.Right};min.Click+=delegate{WindowState=FormWindowState.Minimized;};Controls.Add(min);}
  }
  public void DragWindow(){ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left&&e.Y<53)DragWindow();}
  protected override void OnResize(EventArgs e){base.OnResize(e);if(Width>1&&Height>1)using(var path=Aero.Round(new RectangleF(0,0,Width,Height),12)){var old=Region;Region=new Region(path);if(old!=null)old.Dispose();}}
  internal void DrawBackdrop(Graphics g){using(var brush=new SolidBrush(Aero.Canvas))g.FillRectangle(brush,ClientRectangle);}
  protected override void OnPaintBackground(PaintEventArgs e){DrawBackdrop(e.Graphics);}
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e);
   if(Width<=1||Height<=1)return;
   e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
   using(var path=Aero.Round(new RectangleF(.5f,.5f,Width-1,Height-1),12))
   using(var pen=new Pen(Aero.Edge))e.Graphics.DrawPath(pen,path);
  }
 }
 public sealed class AeroLabel : Label {
  public AeroLabel(){SetStyle(ControlStyles.UserPaint|ControlStyles.Opaque|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);BackColor=Aero.Canvas;}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnPaint(PaintEventArgs e){Aero.PaintBackground(this,e.Graphics);var align=TextAlign;using(var format=new StringFormat {Trimming=StringTrimming.EllipsisCharacter,Alignment=align==ContentAlignment.TopRight||align==ContentAlignment.MiddleRight||align==ContentAlignment.BottomRight?StringAlignment.Far:align==ContentAlignment.TopCenter||align==ContentAlignment.MiddleCenter||align==ContentAlignment.BottomCenter?StringAlignment.Center:StringAlignment.Near,LineAlignment=align==ContentAlignment.BottomLeft||align==ContentAlignment.BottomCenter||align==ContentAlignment.BottomRight?StringAlignment.Far:align==ContentAlignment.MiddleLeft||align==ContentAlignment.MiddleCenter||align==ContentAlignment.MiddleRight?StringAlignment.Center:StringAlignment.Near})using(var brush=new SolidBrush(ForeColor))e.Graphics.DrawString(Text,Font,brush,ClientRectangle,format);}
 }
 public sealed class AeroButton : Button {
  public bool Primary,PlayIcon,Link,Highlight;public int Chrome;
  bool hover,pressed;
  public AeroButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.Opaque|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);BackColor=Aero.Canvas;ForeColor=Aero.Ink;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;UseVisualStyleBackColor=false;Cursor=Cursors.Hand;}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}
  protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
  protected override void OnEnabledChanged(EventArgs e){Invalidate();base.OnEnabledChanged(e);}
  protected override void OnPaint(PaintEventArgs e) {
   var g=e.Graphics;
   Aero.PaintBackground(this,g);
   g.SmoothingMode=SmoothingMode.AntiAlias;
   g.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
   if(Chrome!=0){
    if(hover)Aero.DrawPanel(g,new RectangleF(1,1,Width-3,Height-3),Chrome==2?Color.FromArgb(103,42,41):Aero.Raised,Aero.Edge);
    float x=Width/2f,y=Height/2f,d=Height*.14f;
    using(var pen=new Pen(Aero.Ink,1.5f)){
     if(Chrome==1)g.DrawLine(pen,x-d,y+2,x+d,y+2);
     else{g.DrawLine(pen,x-d,y-d,x+d,y+d);g.DrawLine(pen,x+d,y-d,x-d,y+d);}
    }
    return;
   }
   var bounds=new RectangleF(1,1,Width-3,Height-3);
   Color fill=!Enabled?Aero.Surface:Primary?Aero.Accent:Highlight?Color.FromArgb(77,54,33):Aero.Raised;
   if(Enabled&&hover)fill=Aero.Mix(fill,Aero.Ink,.12f);
   if(Enabled&&pressed)fill=Aero.Mix(fill,Aero.Canvas,.2f);
   if(!Link)Aero.DrawPanel(g,bounds,fill,Enabled&&(Primary||Highlight)?Aero.Accent:Aero.Edge);
   Color color=Primary&&Enabled?Aero.Canvas:Link&&hover?Aero.Accent:!Enabled||Link?Aero.Muted:Aero.Ink;
   var textRect=new RectangleF(4,0,Width-8,Height);
   if(PlayIcon){
    float cy=Height/2f,margin=Height*.48f,size=Height*.13f;
    using(var brush=new SolidBrush(color))g.FillPolygon(brush,new[]{new PointF(margin,cy-size),new PointF(margin,cy+size),new PointF(margin+size*1.5f,cy)});
   }
   Aero.Text(g,Text,Font,color,textRect,StringAlignment.Center);
   if(Focused&&ShowFocusCues&&Width>10&&Height>10){
    using(var pen=new Pen(Primary&&Enabled?Aero.Canvas:Aero.Accent))
    using(var path=Aero.Round(new RectangleF(4,4,Width-9,Height-9),4)){
     pen.DashStyle=DashStyle.Dot;g.DrawPath(pen,path);
    }
   }
  }
 }
 public sealed class Card : Panel {
  public Card(){SetStyle(ControlStyles.Opaque|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);BackColor=Aero.Canvas;}
  internal void DrawSurface(Graphics g){
   g.SmoothingMode=SmoothingMode.AntiAlias;
   Aero.DrawPanel(g,new RectangleF(1,1,Width-3,Height-3),Aero.Surface,Aero.Edge,8);
  }
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnPaint(PaintEventArgs e){Aero.PaintBackground(this,e.Graphics);DrawSurface(e.Graphics);}
 }
 public sealed class GlassSlider : Control {
  int value=100;bool dragging;public event EventHandler ValueChanged;
  public int Value{get{return value;}set{int next=Math.Max(0,Math.Min(100,value));if(this.value==next)return;this.value=next;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
  public GlassSlider(){SetStyle(ControlStyles.UserPaint|ControlStyles.Opaque|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);BackColor=Aero.Canvas;Cursor=Cursors.Hand;TabStop=true;AccessibleRole=AccessibleRole.Slider;AccessibleName="Volume";}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnPaint(PaintEventArgs e){
   Aero.PaintBackground(this,e.Graphics);
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   float y=Height/2f,x=10+(Width-20)*Value/100f;
   using(var pen=new Pen(Aero.Edge,4)){pen.StartCap=pen.EndCap=LineCap.Round;g.DrawLine(pen,10,y,Width-10,y);}
   if(x>10)using(var pen=new Pen(Aero.Accent,4)){pen.StartCap=pen.EndCap=LineCap.Round;g.DrawLine(pen,10,y,x,y);}
   using(var brush=new SolidBrush(Aero.Accent))g.FillEllipse(brush,x-7,y-7,14,14);
   if(Focused&&ShowFocusCues)using(var pen=new Pen(Aero.Ink)){pen.DashStyle=DashStyle.Dot;g.DrawEllipse(pen,x-10,y-10,20,20);}
  }
  void FromMouse(int x){Value=(int)Math.Round((x-10)*100.0/Math.Max(1,Width-20));}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();dragging=true;Capture=true;FromMouse(e.X);}}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging)FromMouse(e.X);}
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
  protected override bool IsInputKey(Keys key){return key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||base.IsInputKey(key);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down)Value--;else if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Up)Value++;else if(e.KeyCode==Keys.Home)Value=0;else if(e.KeyCode==Keys.End)Value=100;else return;e.Handled=true;base.OnKeyDown(e);}
  protected override void OnMouseWheel(MouseEventArgs e){Value+=e.Delta>0?5:-5;base.OnMouseWheel(e);}
 }
 public sealed class AeroCheckBox : CheckBox {
  public AeroCheckBox(){SetStyle(ControlStyles.UserPaint|ControlStyles.Opaque|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);BackColor=Aero.Canvas;Cursor=Cursors.Hand;Font=Aero.UI(10);}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;Aero.PaintBackground(this,g);g.SmoothingMode=SmoothingMode.AntiAlias;float s=Font.Size/(10*96f/72),y=(Height-19*s)/2;
   Aero.DrawPanel(g,new RectangleF(2*s,y,34*s,19*s),Checked?Aero.Accent:Aero.Raised,Checked?Aero.Accent:Aero.Edge,9*s);
   float x=(Checked?19:3)*s;using(var b=new SolidBrush(Checked?Aero.Canvas:Aero.Muted))g.FillEllipse(b,x+s,y+3*s,13*s,13*s);Aero.Text(g,Text,Font,Aero.Ink,new RectangleF(45*s,0,Math.Max(1,Width-45*s),Height));if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle((int)(43*s),3,Math.Max(1,Width-(int)(46*s)),Height-6));}

 }
 public sealed class AeroCombo : ComboBox {
  public AeroCombo(){DropDownStyle=ComboBoxStyle.DropDownList;DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=24;BackColor=Aero.Surface;ForeColor=Aero.Ink;Font=Aero.UI(10);}
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);Aero.SetWindowTheme(Handle,"DarkMode_CFD",null);}
  protected override void OnFontChanged(EventArgs e){base.OnFontChanged(e);ItemHeight=Math.Max(24,Font.Height+8);}
  protected override void OnDrawItem(DrawItemEventArgs e){if(e.Index<0)return;using(var b=new SolidBrush((e.State&DrawItemState.Selected)!=0&&(e.State&DrawItemState.ComboBoxEdit)==0?Color.FromArgb(91,63,36):BackColor))e.Graphics.FillRectangle(b,e.Bounds);Aero.Text(e.Graphics,Items[e.Index].ToString(),Font,ForeColor,new RectangleF(e.Bounds.X+9,e.Bounds.Y,e.Bounds.Width-12,e.Bounds.Height));}
 }
 public sealed class FileDisplay : Control {
  readonly ToolTip tip=new ToolTip {AutoPopDelay=15000};string value="";public string Value{get{return value;}set{this.value=value??"";AccessibleDescription=this.value;tip.SetToolTip(this,this.value);Invalidate();}}
  public FileDisplay(){SetStyle(ControlStyles.UserPaint|ControlStyles.Opaque|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);BackColor=Aero.Canvas;AccessibleName="Game file";}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;Aero.PaintBackground(this,g);g.SmoothingMode=SmoothingMode.AntiAlias;
   Aero.DrawPanel(g,new RectangleF(1,1,Width-3,Height-3),Aero.Surface,Aero.Edge);
   float scale=Height/62f;g.ScaleTransform(scale,scale);float logicalWidth=Width/scale;
   using(var pen=new Pen(Aero.Accent,1.5f)){
    g.DrawPolygon(pen,new[]{new PointF(17,19),new PointF(29,19),new PointF(36,26),new PointF(36,43),new PointF(17,43)});
    g.DrawLines(pen,new[]{new PointF(29,19),new PointF(29,26),new PointF(36,26)});
   }
   using(var font=Aero.UI(11,FontStyle.Bold))Aero.Text(g,String.IsNullOrEmpty(value)?"Choose your game":Path.GetFileName(value),font,Aero.Ink,new RectangleF(49,7,logicalWidth-59,25));
   using(var font=Aero.UI(9.5f))Aero.Text(g,String.IsNullOrEmpty(value)?"default.xex from your extracted copy":Path.GetFileName(Path.GetDirectoryName(value)),font,Aero.Muted,new RectangleF(49,31,logicalWidth-59,22));
  }
  protected override void Dispose(bool disposing){if(disposing)tip.Dispose();base.Dispose(disposing);}
 }
 public sealed class Frets : Control {
  public string[] KeyLabels={"1","2","3","4","5"};public int[] ColorOrder={0,1,2,3,4};
  public Frets(){SetStyle(ControlStyles.UserPaint|ControlStyles.Opaque|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);BackColor=Aero.Canvas;}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;Aero.PaintBackground(this,g);g.SmoothingMode=SmoothingMode.AntiAlias;
   g.ScaleTransform(Width/325f,Height/55f);
   for(int i=0;i<5;i++){
    Color color=Aero.FretColors[ColorOrder[i]];
    var bounds=new RectangleF(i*63+1,3,51,44);
    Aero.DrawPanel(g,bounds,Aero.Mix(Aero.Surface,color,.12f),Aero.Mix(Aero.Edge,color,.55f));
    using(var brush=new SolidBrush(color))g.FillRectangle(brush,bounds.X+8,bounds.Y+5,bounds.Width-16,3);
    bounds.Y+=4;
    using(var font=Aero.UI(KeyLabels[i].Length>5?7:KeyLabels[i].Length>2?9:13,FontStyle.Bold))
     Aero.Text(g,KeyLabels[i],font,color,bounds,StringAlignment.Center);
   }
  }
 }
 public sealed class SleeveArt : Control {
  readonly Bitmap cover;
  [DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr icon);
  public SleeveArt(){
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("GreenDayLauncher.Cover.jpg"))
   using(var image=Image.FromStream(stream))cover=new Bitmap(image);
   AccessibleName="Original Green Day: Rock Band Xbox 360 cover";
   SetStyle(ControlStyles.Opaque|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);BackColor=Aero.Canvas;}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);var window=FindForm() as AeroWindow;if(e.Button==MouseButtons.Left&&window!=null)window.DragWindow();}
  static void Pick(Graphics g, RectangleF bounds) {
   var saved=g.Save();g.TranslateTransform(bounds.X,bounds.Y);g.ScaleTransform(bounds.Width/220f,bounds.Height/220f);
   using(var shape=new GraphicsPath()){
    shape.AddBezier(24,26,61,3,159,3,196,26);
    shape.AddBezier(196,26,236,56,184,140,128,198);
    shape.AddBezier(128,198,117,209,103,209,92,198);
    shape.AddBezier(92,198,36,140,-16,56,24,26);
    shape.CloseFigure();
    using(var fill=new LinearGradientBrush(new Rectangle(0,0,220,220),Color.FromArgb(255,190,100),Color.FromArgb(230,80,57),115f))g.FillPath(fill,shape);
    using(var edge=new Pen(Color.FromArgb(255,205,136),2))g.DrawPath(edge,shape);
   }
   var bolt=new[]{new PointF(126,44),new PointF(67,120),new PointF(108,120),new PointF(93,167),new PointF(155,91),new PointF(116,91)};
   using(var ink=new SolidBrush(Aero.Canvas))g.FillPolygon(ink,bolt);
   g.Restore(saved);
  }
  public static void SaveIcon(string path){
   using(var bitmap=new Bitmap(256,256)){
    using(var g=Graphics.FromImage(bitmap)){g.SmoothingMode=SmoothingMode.AntiAlias;Pick(g,new RectangleF(10,10,236,236));}
    var handle=bitmap.GetHicon();
    try{using(var icon=(Icon)Icon.FromHandle(handle).Clone())using(var stream=File.Create(path))icon.Save(stream);}
    finally{DestroyIcon(handle);}
   }
  }
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;Aero.PaintBackground(this,g);
   g.SmoothingMode=SmoothingMode.AntiAlias;
   g.InterpolationMode=InterpolationMode.HighQualityBicubic;
   g.PixelOffsetMode=PixelOffsetMode.HighQuality;
   float scale=Width/348f,inset=10*scale,caption=47*scale;
   var panel=new RectangleF(.5f,.5f,Width-1,Height-1);
   Aero.DrawPanel(g,panel,Aero.Surface,Aero.Edge,8*scale);
   float ratio=Math.Min((Width-inset*2)/cover.Width,(Height-inset-caption)/cover.Height);
   if(ratio<=0)return;
   float w=cover.Width*ratio,h=cover.Height*ratio;
   var image=new RectangleF((Width-w)/2,inset,w,h);
   g.DrawImage(cover,image);
   float y=image.Bottom+12*scale;
   using(var pen=new Pen(Aero.Edge))g.DrawLine(pen,inset,y,Width-inset,y);
   using(var font=Aero.UI(8.5f*scale,FontStyle.Bold)){
    Aero.Text(g,"ORIGINAL COVER",font,Aero.Accent,new RectangleF(inset+3*scale,y,Width-inset*2,30*scale));
    Aero.Text(g,"XBOX 360  /  2010",font,Aero.Muted,new RectangleF(inset,y,Width-inset*2-3*scale,30*scale),StringAlignment.Far);
   }
  }
  protected override void Dispose(bool disposing){if(disposing&&cover!=null)cover.Dispose();base.Dispose(disposing);}
 }
}



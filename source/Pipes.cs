using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace RetroPipes {
public class Settings {
    public int Speed = 5;
    public int Count = 5;
    public int Palette = 0;
    public bool Rotate = false;
    public bool RandomizeEachRun = false;
    public const int MaxSpeed = 1000, MaxCount = 500;
    public static string FilePath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RetroPipes", "settings.xml"); } }
    public static Settings Load() { try { using(var s = File.OpenRead(FilePath)) return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(s); } catch { return new Settings(); } }
    public void Save() { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); using(var s = File.Create(FilePath)) new XmlSerializer(typeof(Settings)).Serialize(s,this); }
    public void Validate() { Speed=Math.Max(1,Math.Min(MaxSpeed,Speed)); Count=Math.Max(1,Math.Min(MaxCount,Count)); Palette=Math.Max(0,Math.Min(2,Palette)); }
    public Settings ForRun(Random random) {
        var result=new Settings { Speed=Speed,Count=Count,Palette=Palette,Rotate=Rotate };
        result.Validate();
        if(RandomizeEachRun) {
            result.Speed=random.Next(1,result.Speed+1); result.Count=random.Next(1,result.Count+1);
            result.Palette=random.Next(3); result.Rotate=random.Next(2)==1;
        }
        return result;
    }
}

static class Program {
    [STAThread] static void Main(string[] args) {
        Native.SetProcessDPIAware();
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        try {
            var settings=Settings.Load(); settings.Validate();
            string mode=args.Length==0 ? "" : args[0].ToLowerInvariant();
            if(mode=="--self-test") { Tests.Run(args.Length>1?args[1]:"."); return; }
            if(mode=="/s" || mode=="--screensaver") {
                settings=settings.ForRun(new Random());
                foreach(var screen in Screen.AllScreens) new PipesWindow(settings,"saver",screen.Bounds,IntPtr.Zero).Show();
                Application.Run(); return;
            }
            if(mode.StartsWith("/p")) {
                long id; string value=mode.Contains(":")?mode.Substring(mode.IndexOf(':')+1):(args.Length>1?args[1]:"");
                if(long.TryParse(value,out id) && Native.IsWindow(new IntPtr(id))) {
                    var parent=new IntPtr(id); Native.RECT r; Native.GetClientRect(parent,out r);
                    Application.Run(new PipesWindow(settings.ForRun(new Random()),"preview",new Rectangle(0,0,r.Right,r.Bottom),parent));
                }
                return;
            }
            var launcher=new Launcher(settings);
            if(mode=="--wallpaper") launcher.Shown += delegate { launcher.StartWallpaper(); };
            Application.Run(launcher);
        } catch(Exception e) {
            if(args.Length>0 && args[0]=="--self-test") { File.WriteAllText(Path.Combine(args[1],"test-error.txt"),e.ToString()); Environment.ExitCode=1; }
            else MessageBox.Show(e.Message,"Retro Pipes",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
    }
}

class Launcher : Form {
    Settings settings; TrackBar speed,count; internal NumericUpDown speedNumber,countNumber; ComboBox palette; CheckBox rotate,randomize; NotifyIcon tray;
    bool persistSettings; Random random=new Random();
    List<PipesWindow> wallpapers=new List<PipesWindow>(); Timer watch=new Timer(); Label status;
    System.Threading.Mutex wallpaperLock; bool ownsWallpaperLock;
    public Launcher(Settings value) : this(value,true) { }
    internal Launcher(Settings value,bool persist) {
        settings=value; settings.Validate(); persistSettings=persist; Text="Retro Pipes"; ClientSize=new Size(540,590); FormBorderStyle=FormBorderStyle.FixedDialog;
        MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen; BackColor=Color.FromArgb(17,20,29); ForeColor=Color.White;
        Font=new Font("Segoe UI",10); Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        AddLabel("RETRO PIPES",28,23,480,38,24,FontStyle.Bold,Color.White);
        AddLabel("A little piece of the old desktop.",30,70,480,25,11,FontStyle.Regular,Color.FromArgb(161,176,201));
        AddLabel("COLOUR",30,119,110,24,9,FontStyle.Bold,Color.LightSteelBlue);
        palette=new ComboBox { Left=155,Top=113,Width=348,DropDownStyle=ComboBoxStyle.DropDownList };
        palette.Items.AddRange(new object[]{"Classic — bright & glossy","Electric — cyan, pink & violet","Chrome — polished silver"}); palette.SelectedIndex=settings.Palette; Controls.Add(palette);
        AddLabel("GROWTH SPEED",30,166,125,24,9,FontStyle.Bold,Color.LightSteelBlue);
        speed=new TrackBar { Left=150,Top=155,Width=264,Minimum=1,Maximum=10,Value=Math.Min(10,settings.Speed),TickStyle=TickStyle.None }; Controls.Add(speed);
        speedNumber=AddNumber(speed,settings.Speed,Settings.MaxSpeed,159,"SpeedValue");
        AddLabel("PIPE COUNT",30,213,125,24,9,FontStyle.Bold,Color.LightSteelBlue);
        count=new TrackBar { Left=150,Top=202,Width=264,Minimum=1,Maximum=9,Value=Math.Min(9,settings.Count),TickStyle=TickStyle.None }; Controls.Add(count);
        countNumber=AddNumber(count,settings.Count,Settings.MaxCount,206,"CountValue");
        AddLabel("Type larger values: speed up to 1000, pipes up to 500.",155,243,355,20,9,FontStyle.Regular,Color.LightSteelBlue);
        rotate=new CheckBox { Left=155,Top=273,Width=350,Text="Slowly rotate the scene",Checked=settings.Rotate }; Controls.Add(rotate);
        randomize=new CheckBox { Left=30,Top=310,Width=480,Text="Randomize settings each run",Checked=settings.RandomizeEachRun }; Controls.Add(randomize);
        AddLabel("Each start rolls speed and count from 1 to your values,\nplus colour and rotation. Your saved values stay as set.",30,338,480,40,9,FontStyle.Regular,Color.LightSteelBlue);
        AddButton("Preview in a window",30,388,232,delegate { Save(); new PipesWindow(settings.ForRun(random),"window",new Rectangle(0,0,960,600),IntPtr.Zero).Show(); });
        AddButton("Try screensaver",278,388,232,delegate { Save(); Process.Start(Application.ExecutablePath,"/s"); });
        AddButton("Start live wallpaper",30,443,232,delegate { StartWallpaper(); });
        AddButton("Stop live wallpaper",278,443,232,delegate { StopWallpaper(); });
        status=AddLabel("Wallpaper is stopped. Esc exits a preview or screensaver.",30,503,480,42,9,FontStyle.Regular,Color.LightSteelBlue);
        AddLabel("Wallpaper controls live in the system tray while running.",30,557,485,20,9,FontStyle.Regular,Color.FromArgb(138,151,173));
        var menu=new ContextMenuStrip(); menu.Items.Add("Open controls",null,delegate { Show(); WindowState=FormWindowState.Normal; Activate(); });
        menu.Items.Add("New pipe layout",null,delegate { foreach(var w in wallpapers) w.ResetScene(); });
        menu.Items.Add("Stop wallpaper",null,delegate { StopWallpaper(); Show(); });
        menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("Exit Retro Pipes",null,delegate { StopWallpaper(); Close(); });
        tray=new NotifyIcon { Icon=Icon,Text="Retro Pipes — live wallpaper",ContextMenuStrip=menu,Visible=false };
        tray.DoubleClick+=delegate { Show(); Activate(); };
        watch.Interval=2000; watch.Tick+=delegate {
            if(wallpapers.Count>0 && (!Native.IsWindow(wallpapers[0].DesktopParent) || wallpapers.Count!=Screen.AllScreens.Length)) {
                StopWallpaper(); Show(); status.Text="The desktop changed. Start the wallpaper again when ready.";
            }
        }; watch.Start();
    }
    Label AddLabel(string s,int x,int y,int w,int h,int size,FontStyle style,Color color) { var l=new Label { Text=s,Left=x,Top=y,Width=w,Height=h,Font=new Font("Segoe UI",size,style),ForeColor=color }; Controls.Add(l); return l; }
    NumericUpDown AddNumber(TrackBar slider,int value,int max,int y,string name) {
        var number=new NumericUpDown { Name=name,Left=420,Top=y,Width=83,Minimum=1,Maximum=max,Value=value,TextAlign=HorizontalAlignment.Right,BackColor=Color.FromArgb(36,47,66),ForeColor=Color.White };
        bool syncing=false;
        number.ValueChanged+=delegate { if(syncing) return; syncing=true; slider.Value=Math.Min(slider.Maximum,(int)number.Value); syncing=false; };
        slider.ValueChanged+=delegate { if(syncing) return; syncing=true; number.Value=slider.Value; syncing=false; };
        Controls.Add(number); return number;
    }
    void AddButton(string s,int x,int y,int w,EventHandler action) { var b=new Button { Text=s,Left=x,Top=y,Width=w,Height=42,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(36,47,66),ForeColor=Color.White }; b.FlatAppearance.BorderColor=Color.FromArgb(65,84,112); b.Click+=action; Controls.Add(b); }
    void Save() { settings.Speed=(int)speedNumber.Value; settings.Count=(int)countNumber.Value; settings.Palette=palette.SelectedIndex; settings.Rotate=rotate.Checked; settings.RandomizeEachRun=randomize.Checked; if(persistSettings) settings.Save(); }
    public void StartWallpaper() {
        try {
            Save(); StopWallpaper();
            wallpaperLock=new System.Threading.Mutex(false,"Local\\RetroPipes.Wallpaper");
            try { ownsWallpaperLock=wallpaperLock.WaitOne(0); } catch(System.Threading.AbandonedMutexException) { ownsWallpaperLock=true; }
            if(!ownsWallpaperLock) throw new Exception("Retro Pipes wallpaper is already running. Open its system tray icon to control it.");
            IntPtr host=Native.FindWallpaperHost();
            if(host==IntPtr.Zero) throw new Exception("Windows did not expose a wallpaper surface. Try again after closing Task View. Window preview and screensaver are still available.");
            var run=settings.ForRun(random);
            foreach(var screen in Screen.AllScreens) { var w=new PipesWindow(run,"wallpaper",screen.Bounds,host); wallpapers.Add(w); w.Show(); }
            status.Text="Wallpaper running: speed "+run.Speed+", "+run.Count+" pipes. Use the tray icon for controls.";
            tray.Visible=true; Hide(); tray.ShowBalloonTip(2500,"Retro Pipes is running","Right-click the Retro Pipes tray icon for controls or to stop.",ToolTipIcon.Info);
        } catch(Exception e) { StopWallpaper(); MessageBox.Show(this,e.Message,"Wallpaper",MessageBoxButtons.OK,MessageBoxIcon.Information); }
    }
    void StopWallpaper() {
        foreach(var w in wallpapers) w.Close(); wallpapers.Clear();
        if(wallpaperLock!=null) { if(ownsWallpaperLock) wallpaperLock.ReleaseMutex(); wallpaperLock.Dispose(); wallpaperLock=null; ownsWallpaperLock=false; }
        tray.Visible=false; status.Text="Wallpaper is stopped. Your original background is unchanged.";
    }
    protected override void OnFormClosing(FormClosingEventArgs e) { Save(); StopWallpaper(); watch.Dispose(); tray.Dispose(); base.OnFormClosing(e); }
}

struct Cell : IEquatable<Cell> {
    public int X,Y,Z;
    public Cell(int x,int y,int z) { X=x;Y=y;Z=z; }
    public static Cell operator+(Cell a,Cell b) { return new Cell(a.X+b.X,a.Y+b.Y,a.Z+b.Z); }
    public bool Equals(Cell b) { return X==b.X&&Y==b.Y&&Z==b.Z; }
    public override bool Equals(object o) { return o is Cell && Equals((Cell)o); }
    public override int GetHashCode() { return (X+32)*4096+(Y+32)*64+Z+32; }
}
class Segment { public Cell A,B; public Color Color; public bool Joint; }
class Pipe { public Cell At,Direction; public Color Color; public Segment Growing; public double Progress; public bool Dead; }
class Scene {
    public List<Segment> Segments=new List<Segment>(); public List<Pipe> Pipes=new List<Pipe>();
    public HashSet<Cell> Occupied=new HashSet<Cell>(); public int Resets; public double Age;
    Random rng; Settings settings; int halfX=10; int target=460; double hold;
    static readonly Cell[] directions={new Cell(1,0,0),new Cell(-1,0,0),new Cell(0,1,0),new Cell(0,-1,0),new Cell(0,0,1),new Cell(0,0,-1)};
    static readonly Color[][] palettes={ new[]{Color.FromArgb(230,36,53),Color.FromArgb(30,186,70),Color.FromArgb(35,95,240),Color.FromArgb(246,189,30),Color.FromArgb(190,42,225),Color.FromArgb(14,196,206),Color.FromArgb(245,108,24)}, new[]{Color.FromArgb(0,217,233),Color.FromArgb(248,32,143),Color.FromArgb(135,64,240),Color.FromArgb(62,238,191)},new[]{Color.FromArgb(178,196,215),Color.FromArgb(210,216,224),Color.FromArgb(136,163,195)} };
    public Scene(Settings s,int seed,double aspect) { settings=s; rng=new Random(seed); halfX=Math.Max(5,Math.Min(20,(int)(aspect*6.4))); target=halfX*40; Reset(); }
    public void Reset() {
        Segments.Clear(); Pipes.Clear(); Occupied.Clear(); Age=0; hold=0; Resets++;
        var pal=palettes[settings.Palette];
        for(int i=0;i<settings.Count;i++) {
            Cell start; do { start=new Cell(rng.Next(-halfX,halfX+1),rng.Next(-6,7),rng.Next(-4,5)); } while(Occupied.Contains(start));
            Occupied.Add(start); Pipes.Add(new Pipe { At=start,Color=pal[i%pal.Length] });
        }
    }
    bool Inside(Cell c) { return Math.Abs(c.X)<=halfX && Math.Abs(c.Y)<=6 && Math.Abs(c.Z)<=4; }
    void Grow(Pipe p) {
        var choices=new List<Cell>(); foreach(var d in directions) if(Inside(p.At+d)&&!Occupied.Contains(p.At+d)) choices.Add(d);
        if(choices.Count==0) { p.Dead=true; return; }
        Cell dir=choices[rng.Next(choices.Count)];
        if(choices.Contains(p.Direction)&&rng.NextDouble()<0.48) dir=p.Direction;
        Cell next=p.At+dir; Occupied.Add(next);
        p.Growing=new Segment { A=p.At,B=next,Color=p.Color,Joint=!dir.Equals(p.Direction) };
        p.Direction=dir; p.Progress=0;
    }
    public void Step(double dt) {
        Age+=dt;
        bool allDead=true;
        foreach(var p in Pipes) {
            if(p.Dead) continue; allDead=false;
            double remaining=dt*(0.7+settings.Speed*0.7);
            while(remaining>0 && !p.Dead) {
                if(p.Growing==null) { if(Segments.Count>=target) { p.Dead=true; break; } Grow(p); }
                if(p.Growing==null) break;
                double advance=Math.Min(remaining,1-p.Progress); p.Progress+=advance; remaining-=advance;
                if(p.Progress>=1) { Segments.Add(p.Growing); p.At=p.Growing.B; p.Growing=null; }
            }
        }
        if(allDead || Age>105) { hold+=dt; if(hold>3) Reset(); }
    }
}

class PipesWindow : Form {
    Settings settings; string mode; Rectangle bounds; public IntPtr DesktopParent;
    IntPtr dc,rc,quad; uint sphere,cylinder; Timer timer; Stopwatch clock=new Stopwatch(); double last; Point mouse; bool cursorHidden;
    public Scene Scene; public string Renderer; bool ready; bool paused;
    public PipesWindow(Settings s,string m,Rectangle b,IntPtr parent) {
        settings=new Settings {Speed=s.Speed,Count=s.Count,Palette=s.Palette,Rotate=s.Rotate}; mode=m; bounds=b; DesktopParent=parent;
        Text="Retro Pipes — Space: pause · R: new layout · Esc: close"; BackColor=Color.Black;
        SetStyle(ControlStyles.Opaque|ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint,true);
        if(m=="window" || m=="test") { ClientSize=b.Size; StartPosition=m=="test"?FormStartPosition.Manual:FormStartPosition.CenterScreen; if(m=="test") Location=new Point(-16000,-16000); }
        else { FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false; StartPosition=FormStartPosition.Manual; Bounds=b; }
        if(m=="saver") TopMost=true;
        KeyPreview=true; KeyDown+=delegate(object sender,KeyEventArgs e) {
            if(mode=="saver") Application.Exit();
            else if(e.KeyCode==Keys.Escape) Close(); else if(e.KeyCode==Keys.Space) paused=!paused; else if(e.KeyCode==Keys.R) ResetScene();
        };
        MouseDown+=delegate { if(mode=="saver") Application.Exit(); };
    }
    protected override CreateParams CreateParams { get { var cp=base.CreateParams; cp.ClassStyle|=0x20; if(mode=="wallpaper") cp.ExStyle|=0x08000080; return cp; } }
    protected override bool ShowWithoutActivation { get { return mode=="wallpaper" || mode=="preview" || mode=="test"; } }
    protected override void OnLoad(EventArgs e) {
        base.OnLoad(e);
        if(mode=="wallpaper" || mode=="preview") {
            int style=Native.GetWindowLong(Handle,-16); Native.SetWindowLong(Handle,-16,(style&~unchecked((int)0x80000000))|0x40000000);
            Native.SetParent(Handle,DesktopParent);
            if(Native.GetParent(Handle)!=DesktopParent) throw new Exception("Windows could not attach the pipes to the desktop.");
            var p=new Native.POINT { X=mode=="preview"?0:bounds.X,Y=mode=="preview"?0:bounds.Y };
            if(mode=="wallpaper") Native.ScreenToClient(DesktopParent,ref p);
            Native.SetWindowPos(Handle,IntPtr.Zero,p.X,p.Y,bounds.Width,bounds.Height,0x0014);
        }
        dc=Native.GetDC(Handle);
        var pfd=new Native.PFD { size=(ushort)Marshal.SizeOf(typeof(Native.PFD)),version=1,flags=0x25,colorBits=24,depthBits=24 };
        int format=Native.ChoosePixelFormat(dc,ref pfd);
        if(format==0 || !Native.SetPixelFormat(dc,format,ref pfd)) throw new Exception("Could not create an OpenGL drawing surface.");
        rc=GL.wglCreateContext(dc); if(rc==IntPtr.Zero || !GL.wglMakeCurrent(dc,rc)) throw new Exception("OpenGL could not start. Check your graphics driver.");
        Renderer=Marshal.PtrToStringAnsi(GL.glGetString(0x1F01));
        GL.glEnable(0x0B71); GL.glEnable(0x0B50); GL.glEnable(0x4000); GL.glEnable(0x0BA1); GL.glEnable(0x0B57);
        GL.glColorMaterial(0x0408,0x1602); GL.glShadeModel(0x1D01);
        GL.glLightfv(0x4000,0x1201,new float[]{1,1,1,1}); GL.glLightfv(0x4000,0x1202,new float[]{1,1,1,1});
        GL.glLightModelfv(0x0B53,new float[]{0.22f,0.22f,0.25f,1});
        GL.glMaterialfv(0x0408,0x1202,new float[]{0.95f,0.95f,0.95f,1}); GL.glMaterialf(0x0408,0x1601,76);
        quad=GL.gluNewQuadric(); if(quad==IntPtr.Zero) throw new Exception("OpenGL geometry creation failed."); GL.gluQuadricNormals(quad,100000);
        sphere=GL.glGenLists(2); if(sphere==0) throw new Exception("OpenGL geometry cache creation failed."); cylinder=sphere+1;
        GL.glNewList(sphere,0x1300); GL.gluSphere(quad,1,18,12); GL.glEndList();
        GL.glNewList(cylinder,0x1300); GL.gluCylinder(quad,1,1,1,18,1); GL.glEndList();
        Scene=new Scene(settings,Environment.TickCount^Handle.GetHashCode(),Math.Max(0.4,(double)ClientSize.Width/Math.Max(1,ClientSize.Height)));
        ready=true; mouse=Cursor.Position; clock.Start();
        if(mode=="saver") { Cursor.Hide(); cursorHidden=true; }
        timer=new Timer { Interval=mode=="wallpaper"||mode=="preview"?33:16 };
        timer.Tick+=delegate {
            double now=clock.Elapsed.TotalSeconds; double dt=Math.Min(0.1,now-last); last=now;
            if(mode=="saver"&&now>0.8&&(Math.Abs(Cursor.Position.X-mouse.X)>6||Math.Abs(Cursor.Position.Y-mouse.Y)>6)) { Application.Exit(); return; }
            if(mode=="preview"&&!Native.IsWindow(DesktopParent)) { Close(); return; }
            if(WindowState==FormWindowState.Minimized) return;
            if(!paused) Scene.Step(dt); Invalidate();
        };
        if(mode!="test") timer.Start();
    }
    public void ResetScene() { if(Scene!=null) Scene.Reset(); }
    protected override void OnPaintBackground(PaintEventArgs e) { }
    protected override void OnPaint(PaintEventArgs e) { if(ready) Draw(true); }
    void Ball(double x,double y,double z,double radius) { GL.glPushMatrix(); GL.glTranslated(x,y,z); GL.glScaled(radius,radius,radius); GL.glCallList(sphere); GL.glPopMatrix(); }
    void DrawSegment(Segment s,double progress) {
        const double r=0.18; double dx=s.B.X-s.A.X,dy=s.B.Y-s.A.Y,dz=s.B.Z-s.A.Z;
        GL.glColor3f(s.Color.R/255f,s.Color.G/255f,s.Color.B/255f);
        GL.glPushMatrix(); GL.glTranslated(s.A.X,s.A.Y,s.A.Z);
        if(dx!=0) GL.glRotated(dx*90,0,1,0); else if(dy!=0) GL.glRotated(-dy*90,1,0,0); else if(dz<0) GL.glRotated(180,1,0,0);
        GL.glScaled(r,r,Math.Max(0.001,progress)); GL.glCallList(cylinder); GL.glPopMatrix();
        Ball(s.A.X,s.A.Y,s.A.Z,s.Joint?r*1.15:r);
        Ball(s.A.X+dx*progress,s.A.Y+dy*progress,s.A.Z+dz*progress,r);
    }
    public void Draw(bool swap) {
        GL.wglMakeCurrent(dc,rc); int w=Math.Max(1,ClientSize.Width),h=Math.Max(1,ClientSize.Height); double aspect=(double)w/h;
        GL.glViewport(0,0,w,h); GL.glClearColor(0,0,0,1); GL.glClear(0x4100);
        GL.glMatrixMode(0x1701); GL.glLoadIdentity(); double top=0.41421356; GL.glFrustum(-top*aspect,top*aspect,-top,top,1,100);
        GL.glMatrixMode(0x1700); GL.glLoadIdentity(); GL.glLightfv(0x4000,0x1203,new float[]{-5,8,15,1});
        GL.glTranslated(0,0,-27); GL.glRotated(13,1,0,0); GL.glRotated(settings.Rotate?Scene.Age*2: -15,0,1,0);
        foreach(var s in Scene.Segments) DrawSegment(s,1);
        foreach(var p in Scene.Pipes) if(p.Growing!=null) DrawSegment(p.Growing,Math.Min(1,p.Progress));
        GL.glFlush(); if(swap) Native.SwapBuffers(dc);
    }
    public void SaveFrame(string path) {
        Draw(false); GL.glFinish(); int w=ClientSize.Width,h=ClientSize.Height; byte[] pixels=new byte[w*h*4];
        GL.glReadPixels(0,0,w,h,0x80E1,0x1401,pixels);
        using(var bitmap=new Bitmap(w,h,PixelFormat.Format32bppRgb)) {
            var data=bitmap.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppRgb);
            try { for(int y=0;y<h;y++) Marshal.Copy(pixels,(h-1-y)*w*4,IntPtr.Add(data.Scan0,y*data.Stride),w*4); }
            finally { bitmap.UnlockBits(data); }
            bitmap.Save(path,ImageFormat.Png);
        }
        uint error=GL.glGetError(); if(error!=0) throw new Exception("OpenGL error: "+error);
    }
    protected override void OnFormClosed(FormClosedEventArgs e) {
        ready=false; if(timer!=null) timer.Dispose(); if(cursorHidden) Cursor.Show();
        if(rc!=IntPtr.Zero) { GL.wglMakeCurrent(dc,rc); if(sphere!=0) GL.glDeleteLists(sphere,2); if(quad!=IntPtr.Zero) GL.gluDeleteQuadric(quad); GL.wglMakeCurrent(IntPtr.Zero,IntPtr.Zero); GL.wglDeleteContext(rc); }
        if(dc!=IntPtr.Zero) Native.ReleaseDC(Handle,dc); base.OnFormClosed(e);
        if(mode=="saver") Application.Exit();
    }
}

static class Tests {
    public static void Run(string dir) {
        Directory.CreateDirectory(dir); var log=new List<string>();
        var manual=new Settings { Speed=125,Count=60,Palette=2,Rotate=true };
        manual.Validate(); var fixedRun=manual.ForRun(new Random(1));
        if(fixedRun.Speed!=125||fixedRun.Count!=60||fixedRun.Palette!=2||!fixedRun.Rotate) throw new Exception("Extended manual values were lost");
        manual.RandomizeEachRun=true; var rolls=new HashSet<string>(); var random=new Random(17);
        for(int i=0;i<100;i++) {
            var run=manual.ForRun(random);
            if(run.Speed<1||run.Speed>125||run.Count<1||run.Count>60||run.Palette<0||run.Palette>2||run.RandomizeEachRun) throw new Exception("Invalid randomized run");
            rolls.Add(run.Speed+":"+run.Count+":"+run.Palette+":"+run.Rotate);
        }
        if(rolls.Count<90||manual.Speed!=125||manual.Count!=60||!manual.RandomizeEachRun) throw new Exception("Randomization changed stored settings or lacks variation");
        using(var buffer=new MemoryStream()) {
            var xml=new XmlSerializer(typeof(Settings)); xml.Serialize(buffer,manual); buffer.Position=0;
            var restored=(Settings)xml.Deserialize(buffer); restored.Validate();
            if(restored.Speed!=125||restored.Count!=60||!restored.RandomizeEachRun) throw new Exception("Settings round trip failed");
        }
        var normal=new Scene(new Settings {Speed=10,Count=1},12,1.6);
        var fast=new Scene(new Settings {Speed=200,Count=1},12,1.6);
        normal.Step(0.1); fast.Step(0.1);
        if(fast.Segments.Count<=normal.Segments.Count+1) throw new Exception("Extended speed is capped by frame rate");
        var dense=new Scene(new Settings {Speed=Settings.MaxSpeed,Count=Settings.MaxCount},4,0.5);
        if(dense.Pipes.Count!=500) throw new Exception("Extended count was lost");
        for(int i=0;i<1500;i++) dense.Step(0.04);
        if(dense.Resets<2) throw new Exception("Maximum settings cannot cycle");
        log.Add("PASS: extended values, randomization bounds/variation, settings round trip, above-frame-rate growth and maximum-density cycling.");
        for(int seed=0;seed<30;seed++) {
            var scene=new Scene(new Settings {Count=9,Speed=10},seed,1.6);
            for(int frame=0;frame<6000;frame++) {
                scene.Step(0.04);
                if(frame%40==0) {
                    var cells=new HashSet<Cell>(); foreach(var s in scene.Segments) {
                        if(Math.Abs(s.A.X-s.B.X)+Math.Abs(s.A.Y-s.B.Y)+Math.Abs(s.A.Z-s.B.Z)!=1) throw new Exception("Non-axis-aligned pipe");
                        if(!cells.Add(s.B)) throw new Exception("Pipe collision");
                        if(Math.Abs(s.B.X)>10||Math.Abs(s.B.Y)>6||Math.Abs(s.B.Z)>4) throw new Exception("Pipe outside bounds");
                    }
                }
            }
            if(scene.Resets<2) throw new Exception("Scene did not cycle");
        }
        log.Add("PASS: 180,000 simulation updates across 30 seeds; axis alignment, unique endpoints, bounds, automatic reset.");
        var sw=Stopwatch.StartNew();
        using(var w=new PipesWindow(new Settings(),"test",new Rectangle(0,0,1200,750),IntPtr.Zero)) {
            w.Show(); Application.DoEvents(); w.Scene=new Scene(new Settings(),21,1.6);
            for(int i=0;i<650;i++) w.Scene.Step(0.02);
            w.SaveFrame(Path.Combine(dir,"Pipes-preview.png"));
            log.Add("PASS: OpenGL rendered PNG without errors; renderer: "+w.Renderer);
            sw.Restart(); for(int i=0;i<60;i++) w.Draw(false); GL.glFinish(); sw.Stop();
            log.Add("Render benchmark: "+(sw.Elapsed.TotalMilliseconds/60).ToString("F2")+" ms/frame, "+w.Scene.Segments.Count+" finished pipe segments.");
            w.Close();
        }
        using(var parent=new Form { ClientSize=new Size(240,160),StartPosition=FormStartPosition.Manual,Location=new Point(-16000,-16000) }) {
            parent.Show();
            using(var preview=new PipesWindow(new Settings(),"preview",new Rectangle(0,0,240,160),parent.Handle)) {
                preview.Show(); Application.DoEvents();
                if(Native.GetParent(preview.Handle)!=parent.Handle) throw new Exception("Preview parenting failed");
                preview.Close();
            }
            parent.Close();
        }
        log.Add("PASS: embedded screensaver preview parent and teardown.");
        IntPtr host=Native.FindWallpaperHost(); log.Add(host!=IntPtr.Zero?"PASS: Windows desktop wallpaper surface found.":"NOTE: No Windows desktop wallpaper surface available.");
        if(host!=IntPtr.Zero) {
            using(var wall=new PipesWindow(new Settings(),"wallpaper",new Rectangle(-15000,-15000,320,200),host)) {
                wall.Show(); Application.DoEvents();
                if(Native.GetParent(wall.Handle)!=host) throw new Exception("Wallpaper parenting failed");
                wall.Draw(true); wall.Close();
            }
            log.Add("PASS: wallpaper attachment, rendering and teardown on an offscreen desktop child.");
        }
        var uiSettings=new Settings {Speed=42,Count=30};
        using(var launcher=new Launcher(uiSettings,false)) {
            launcher.StartPosition=FormStartPosition.Manual; launcher.Location=new Point(-16000,-16000);
            launcher.Show(); Application.DoEvents();
            launcher.speedNumber.Text="125"; launcher.countNumber.Text="60";
            if(launcher.speedNumber.Value!=125||launcher.countNumber.Value!=60) throw new Exception("Typed numbers were not accepted");
            launcher.speedNumber.Value=42; launcher.countNumber.Value=30;
            using(var bitmap=new Bitmap(launcher.Width,launcher.Height)) { launcher.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height)); bitmap.Save(Path.Combine(dir,"Launcher-preview.png"),ImageFormat.Png); }
            launcher.Close();
        }
        if(uiSettings.Speed!=42||uiSettings.Count!=30) throw new Exception("Closing controls lost above-slider values");
        log.Add("PASS: launcher controls render and close cleanly.");
        File.WriteAllLines(Path.Combine(dir,"test-results.txt"),log.ToArray());
    }
}

static class Native {
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct PFD {
        public ushort size,version; public uint flags; public byte pixelType,colorBits,redBits,redShift,greenBits,greenShift,blueBits,blueShift,alphaBits,alphaShift,accumBits,accumRedBits,accumGreenBits,accumBlueBits,accumAlphaBits,depthBits,stencilBits,auxBuffers,layerType,reserved; public uint layerMask,visibleMask,damageMask;
    }
    public delegate bool EnumProc(IntPtr h,IntPtr l);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr h,IntPtr dc);
    [DllImport("user32.dll")] public static extern IntPtr SetParent(IntPtr h,IntPtr parent);
    [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr h);
    [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h,int index);
    [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr h,int index,int value);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h,out RECT r);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ScreenToClient(IntPtr h,ref POINT p);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int height,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string cls,string name);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string cls,string name);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc,IntPtr l);
    [DllImport("user32.dll")] public static extern IntPtr SendMessageTimeout(IntPtr h,uint msg,IntPtr w,IntPtr l,uint flags,uint timeout,out IntPtr result);
    [DllImport("gdi32.dll")] public static extern int ChoosePixelFormat(IntPtr dc,ref PFD p);
    [DllImport("gdi32.dll")] public static extern bool SetPixelFormat(IntPtr dc,int format,ref PFD p);
    [DllImport("gdi32.dll")] public static extern bool SwapBuffers(IntPtr dc);
    public static IntPtr FindWallpaperHost() {
        IntPtr prog=FindWindow("Progman",null),result;
        if(prog==IntPtr.Zero) return IntPtr.Zero;
        // Ask Explorer to create its separate wallpaper WorkerW surface.
        SendMessageTimeout(prog,0x052C,IntPtr.Zero,IntPtr.Zero,2,1000,out result);
        IntPtr host=IntPtr.Zero;
        EnumWindows(delegate(IntPtr top,IntPtr unused) {
            if(FindWindowEx(top,IntPtr.Zero,"SHELLDLL_DefView",null)!=IntPtr.Zero) {
                var next=FindWindowEx(IntPtr.Zero,top,"WorkerW",null);
                if(next!=IntPtr.Zero) { host=next; return false; }
            }
            return true;
        },IntPtr.Zero);
        return host;
    }
}

static class GL {
    [DllImport("opengl32.dll")] public static extern IntPtr wglCreateContext(IntPtr dc);
    [DllImport("opengl32.dll")] public static extern bool wglMakeCurrent(IntPtr dc,IntPtr rc);
    [DllImport("opengl32.dll")] public static extern bool wglDeleteContext(IntPtr rc);
    [DllImport("opengl32.dll")] public static extern IntPtr glGetString(uint name);
    [DllImport("opengl32.dll")] public static extern uint glGetError();
    [DllImport("opengl32.dll")] public static extern void glEnable(uint cap);
    [DllImport("opengl32.dll")] public static extern void glShadeModel(uint mode);
    [DllImport("opengl32.dll")] public static extern void glColorMaterial(uint face,uint mode);
    [DllImport("opengl32.dll")] public static extern void glLightfv(uint light,uint name,float[] values);
    [DllImport("opengl32.dll")] public static extern void glLightModelfv(uint name,float[] values);
    [DllImport("opengl32.dll")] public static extern void glMaterialfv(uint face,uint name,float[] values);
    [DllImport("opengl32.dll")] public static extern void glMaterialf(uint face,uint name,float value);
    [DllImport("opengl32.dll")] public static extern void glViewport(int x,int y,int w,int h);
    [DllImport("opengl32.dll")] public static extern void glClearColor(float r,float g,float b,float a);
    [DllImport("opengl32.dll")] public static extern void glClear(uint mask);
    [DllImport("opengl32.dll")] public static extern void glMatrixMode(uint mode);
    [DllImport("opengl32.dll")] public static extern void glLoadIdentity();
    [DllImport("opengl32.dll")] public static extern void glFrustum(double left,double right,double bottom,double top,double near,double far);
    [DllImport("opengl32.dll")] public static extern void glTranslated(double x,double y,double z);
    [DllImport("opengl32.dll")] public static extern void glRotated(double angle,double x,double y,double z);
    [DllImport("opengl32.dll")] public static extern void glScaled(double x,double y,double z);
    [DllImport("opengl32.dll")] public static extern void glColor3f(float r,float g,float b);
    [DllImport("opengl32.dll")] public static extern void glPushMatrix();
    [DllImport("opengl32.dll")] public static extern void glPopMatrix();
    [DllImport("opengl32.dll")] public static extern uint glGenLists(int range);
    [DllImport("opengl32.dll")] public static extern void glNewList(uint list,uint mode);
    [DllImport("opengl32.dll")] public static extern void glEndList();
    [DllImport("opengl32.dll")] public static extern void glCallList(uint list);
    [DllImport("opengl32.dll")] public static extern void glDeleteLists(uint list,int range);
    [DllImport("opengl32.dll")] public static extern void glFlush();
    [DllImport("opengl32.dll")] public static extern void glFinish();
    [DllImport("opengl32.dll")] public static extern void glReadPixels(int x,int y,int width,int height,uint format,uint type,byte[] data);
    [DllImport("glu32.dll")] public static extern IntPtr gluNewQuadric();
    [DllImport("glu32.dll")] public static extern void gluDeleteQuadric(IntPtr q);
    [DllImport("glu32.dll")] public static extern void gluQuadricNormals(IntPtr q,uint normals);
    [DllImport("glu32.dll")] public static extern void gluSphere(IntPtr q,double radius,int slices,int stacks);
    [DllImport("glu32.dll")] public static extern void gluCylinder(IntPtr q,double baseRadius,double topRadius,double height,int slices,int stacks);
}
}

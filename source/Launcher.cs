using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace RetroPipes {
class Launcher : Form {
    Settings settings; TrackBar speed,count; internal NumericUpDown speedNumber,countNumber;
    ComboBox palette; CheckBox rotate,randomize,pipes,fireworks,bubbles,teapots; internal CheckBox spanScreens;
    CheckBox randSpeed,randCount,randPalette,randRotation,randPipes,randFireworks,randBubbles,randDensity;
    NumericUpDown rotationChance,pipesChance,fireworksChance,bubblesChance,teapotChance,fireworkRate,bubbleCount;
    bool persistSettings,activeSpan; Rectangle[] activeDisplays; NotifyIcon tray; Label status; Timer watch=new Timer();
    List<PipesWindow> wallpapers=new List<PipesWindow>();
    System.Threading.Mutex wallpaperLock; bool ownsWallpaperLock;
    public Launcher(Settings value) : this(value,true) { }
    internal Launcher(Settings value,bool persist) {
        settings=value; settings.Validate(); persistSettings=persist;
        Text="Retro Pipes"; ClientSize=new Size(790,717); FormBorderStyle=FormBorderStyle.FixedDialog;
        MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen;
        BackColor=Color.FromArgb(17,20,29); ForeColor=Color.White; Font=new Font("Segoe UI",10);
        Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Label("RETRO PIPES",26,20,650,42,25,Color.White);
        var credits=new LinkLabel {Text="Credits",Left=700,Top=34,Width=60,Height=22,LinkColor=Color.LightSteelBlue};
        credits.LinkClicked+=delegate {
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("RetroPipes.ThirdPartyNotices"))
            using(var reader=new StreamReader(stream)) MessageBox.Show(reader.ReadToEnd(),"Retro Pipes — third-party credits");
        }; Controls.Add(credits); credits.BringToFront();
        Label("Pipes, sparks & soap bubbles. Separate screens or one shared canvas.",29,69,710,25,11,Color.LightSteelBlue);
        Button("Pipes only",30,106,165,delegate { Layers(true,false,false); });
        Button("Fireworks only",218,106,165,delegate { Layers(false,true,false); });
        Button("Bubbles only",406,106,165,delegate { Layers(false,false,true); });
        Button("All three",594,106,165,delegate { Layers(true,true,true); });
        pipes=Check("Pipes",30,158,125,settings.Pipes); fireworks=Check("Fireworks",218,158,140,settings.Fireworks); bubbles=Check("Bubbles",406,158,140,settings.Bubbles);
        Label("Tick multiple layers to overlap.",594,161,170,36,9,Color.LightSteelBlue);
        Label("MANUAL SETTINGS",30,206,335,23,10,Color.LightSteelBlue);
        var cycleHeading=Label("RANDOM CYCLE · EACH SCREEN",410,206,350,23,10,Color.LightSteelBlue);
        Label("Pipe colour",30,248,110,24,10,Color.White);
        palette=new ComboBox {Left=147,Top=244,Width=225,DropDownStyle=ComboBoxStyle.DropDownList};
        palette.Items.AddRange(new object[]{"Classic colours","Electric neon","Polished chrome"}); palette.SelectedIndex=settings.Palette; Controls.Add(palette);
        Label("Pipe speed",30,289,110,24,10,Color.White);
        speed=Slider(278,10,settings.Speed); speedNumber=Number(303,284,69,1,Settings.MaxSpeed,settings.Speed,0); Bind(speed,speedNumber);
        Label("Pipe count",30,331,110,24,10,Color.White);
        count=Slider(320,9,settings.Count); countNumber=Number(303,326,69,1,Settings.MaxCount,settings.Count,0); Bind(count,countNumber);
        rotate=Check("Rotate pipes slowly",30,367,330,settings.Rotate);
        teapots=Check("Teapot easter egg",30,401,215,settings.Teapots);
        teapotChance=Number(266,400,75,0,100,(decimal)settings.TeapotChance,2); Label("%",346,403,25,23,10,Color.LightSteelBlue);
        Label("Chance per pipe bend. Default: 0.5%.",30,432,345,23,9,Color.LightSteelBlue);
        Label("Firework intensity",30,470,220,24,10,Color.White); fireworkRate=Number(291,466,81,1,30,settings.FireworkRate,0);
        Label("Bubble count",30,508,210,24,10,Color.White); bubbleCount=Number(291,504,81,1,100,settings.BubbleCount,0);
        randomize=Check("Enable random cycle",410,244,349,settings.RandomizeEachRun);
        randSpeed=Check("Speed",410,279,108,settings.RandomSpeed); randCount=Check("Count",528,279,110,settings.RandomCount); randPalette=Check("Colour",651,279,107,settings.RandomPalette);
        randRotation=Check("Rotation chance",410,315,234,settings.RandomRotation); rotationChance=Chance(315,settings.RotationChance);
        randPipes=Check("Pipes chance",410,353,234,settings.RandomPipes); pipesChance=Chance(353,settings.PipesChance);
        randFireworks=Check("Fireworks chance",410,391,234,settings.RandomFireworks); fireworksChance=Chance(391,settings.FireworksChance);
        randBubbles=Check("Bubbles chance",410,429,234,settings.RandomBubbles); bubblesChance=Chance(429,settings.BubblesChance);
        randDensity=Check("Firework intensity + bubble count",410,470,349,settings.RandomEffectDensity);
        Button("Pipes random",410,505,110,delegate { RandomPreset(1); });
        Button("Mix random",529,505,110,delegate { RandomPreset(2); });
        Button("All random",649,505,110,delegate { RandomPreset(3); });
        spanScreens=Check("Span all screens — one continuous scene, same growth speed",30,548,729,settings.SpanAllScreens);
        var cycleHelp=Label("",30,585,729,42,9,Color.LightSteelBlue);
        EventHandler updateLayoutText=delegate {
            cycleHeading.Text=spanScreens.Checked?"RANDOM CYCLE · SHARED SCENE":"RANDOM CYCLE · EACH SCREEN";
            cycleHelp.Text="Tick only what should change. Numbers are random upper bounds.\n"+(spanScreens.Checked?"One scene and one settings roll cover all screens. Extra space fills naturally.":"Each monitor rolls separately at start, at reset and when you press R.");
        }; spanScreens.CheckedChanged+=updateLayoutText; updateLayoutText(null,EventArgs.Empty);
        Button("Window preview",30,639,165,delegate { Save(); new PipesWindow(settings,"window",DisplayLayout.PreviewBounds(settings.SpanAllScreens),IntPtr.Zero).Show(); });
        Button("Try screensaver",218,639,165,delegate { Save(); Process.Start(Application.ExecutablePath,"/s"); });
        Button("Start wallpaper",406,639,165,delegate { StartWallpaper(); });
        Button("Stop wallpaper",594,639,165,delegate { StopWallpaper(); });
        status=Label("Ready. Wallpaper controls appear in the system tray while running.",30,684,729,24,9,Color.LightSteelBlue);
        var menu=new ContextMenuStrip(); menu.Items.Add("Open controls",null,delegate { Show(); WindowState=FormWindowState.Normal; Activate(); });
        menu.Items.Add("Roll new scenes on every screen",null,delegate { foreach(var w in wallpapers) w.ResetScene(); });
        menu.Items.Add("Current screen settings",null,delegate {
            var lines=new List<string>(); for(int i=0;i<wallpapers.Count;i++) lines.Add((activeSpan?"All screens":"Screen "+(i+1))+": "+wallpapers[i].Description);
            MessageBox.Show(string.Join("\n\n",lines.ToArray()),"Each screen's current settings");
        });
        menu.Items.Add("Stop wallpaper",null,delegate { StopWallpaper(); Show(); });
        menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("Exit Retro Pipes",null,delegate { Close(); });
        tray=new NotifyIcon {Icon=Icon,Text="Retro Pipes — live wallpaper",ContextMenuStrip=menu,Visible=false};
        tray.DoubleClick+=delegate { Show(); Activate(); };
        watch.Interval=2000; watch.Tick+=delegate {
            if(wallpapers.Count>0&&(!Native.IsWindow(wallpapers[0].DesktopParent)||!DisplayLayout.Same(activeDisplays,DisplayLayout.Screens()))) { StopWallpaper(); Show(); status.Text="The desktop changed. Start the wallpaper again when ready."; }
        }; watch.Start();
    }
    void Layers(bool p,bool f,bool b) { pipes.Checked=p; fireworks.Checked=f; bubbles.Checked=b; randPipes.Checked=randFireworks.Checked=randBubbles.Checked=false; }
    void RandomPreset(int preset) {
        randomize.Checked=true;
        randSpeed.Checked=randCount.Checked=randPalette.Checked=randRotation.Checked=preset!=2;
        randPipes.Checked=randFireworks.Checked=randBubbles.Checked=preset!=1;
        randDensity.Checked=preset==3;
    }
    Label Label(string text,int x,int y,int width,int height,int size,Color color) {
        var label=new Label {Text=text,Left=x,Top=y,Width=width,Height=height,Font=new Font("Segoe UI",size),ForeColor=color,UseMnemonic=false}; Controls.Add(label); return label;
    }
    CheckBox Check(string text,int x,int y,int width,bool value) { var c=new CheckBox {Text=text,Left=x,Top=y,Width=width,Height=25,Checked=value}; Controls.Add(c); return c; }
    TrackBar Slider(int y,int max,int value) { var s=new TrackBar {Left=142,Top=y,Width=155,Minimum=1,Maximum=max,Value=Math.Min(max,value),TickStyle=TickStyle.None}; Controls.Add(s); return s; }
    NumericUpDown Number(int x,int y,int width,int min,int max,decimal value,int decimals) {
        var n=new NumericUpDown {Left=x,Top=y,Width=width,Minimum=min,Maximum=max,Value=value,DecimalPlaces=decimals,Increment=decimals>0?0.1m:1m,TextAlign=HorizontalAlignment.Right,BackColor=Color.FromArgb(36,47,66),ForeColor=Color.White}; Controls.Add(n); return n;
    }
    NumericUpDown Chance(int y,int value) { var n=Number(665,y,67,0,100,value,0); Label("%",737,y+3,25,23,10,Color.LightSteelBlue); return n; }
    void Bind(TrackBar slider,NumericUpDown number) {
        bool syncing=false;
        number.ValueChanged+=delegate { if(syncing) return; syncing=true; slider.Value=Math.Min(slider.Maximum,(int)number.Value); syncing=false; };
        slider.ValueChanged+=delegate { if(syncing) return; syncing=true; number.Value=slider.Value; syncing=false; };
    }
    void Button(string text,int x,int y,int width,EventHandler action) { var b=new Button {Text=text,Left=x,Top=y,Width=width,Height=35,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(36,47,66),ForeColor=Color.White}; b.FlatAppearance.BorderColor=Color.FromArgb(65,84,112); b.Click+=action; Controls.Add(b); }
    void Save() {
        settings.Speed=(int)speedNumber.Value; settings.Count=(int)countNumber.Value; settings.Palette=palette.SelectedIndex; settings.Rotate=rotate.Checked;
        settings.Pipes=pipes.Checked; settings.Fireworks=fireworks.Checked; settings.Bubbles=bubbles.Checked; settings.Teapots=teapots.Checked; settings.TeapotChance=(double)teapotChance.Value;
        settings.FireworkRate=(int)fireworkRate.Value; settings.BubbleCount=(int)bubbleCount.Value;
        settings.RandomizeEachRun=randomize.Checked; settings.RandomSpeed=randSpeed.Checked; settings.RandomCount=randCount.Checked; settings.RandomPalette=randPalette.Checked;
        settings.RandomRotation=randRotation.Checked; settings.RandomPipes=randPipes.Checked; settings.RandomFireworks=randFireworks.Checked; settings.RandomBubbles=randBubbles.Checked;
        settings.RandomEffectDensity=randDensity.Checked; settings.RotationChance=(int)rotationChance.Value; settings.PipesChance=(int)pipesChance.Value;
        settings.FireworksChance=(int)fireworksChance.Value; settings.BubblesChance=(int)bubblesChance.Value;
        settings.SpanAllScreens=spanScreens.Checked;
        settings.Validate(); pipes.Checked=settings.Pipes;
        if(persistSettings) settings.Save();
    }
    public void StartWallpaper() {
        try {
            Save(); StopWallpaper(); wallpaperLock=new System.Threading.Mutex(false,"Local\\RetroPipes.Wallpaper");
            try { ownsWallpaperLock=wallpaperLock.WaitOne(0); } catch(System.Threading.AbandonedMutexException) { ownsWallpaperLock=true; }
            if(!ownsWallpaperLock) throw new Exception("Retro Pipes wallpaper is already running. Open its system tray icon to control it.");
            IntPtr host=Native.FindWallpaperHost(); if(host==IntPtr.Zero) throw new Exception("Windows did not expose a wallpaper surface. Try again after closing Task View.");
            activeDisplays=DisplayLayout.Screens(); activeSpan=settings.SpanAllScreens;
            foreach(var area in DisplayLayout.RenderBounds(activeSpan,activeDisplays)) { var w=new PipesWindow(settings,"wallpaper",area,host); wallpapers.Add(w); w.Show(); }
            status.Text=activeSpan?"One continuous scene across "+activeDisplays.Length+" screens. Growth speed is unchanged.":"Running on "+wallpapers.Count+" screens. Each screen has its own random cycle.";
            tray.Visible=true; Hide(); tray.ShowBalloonTip(2500,"Retro Pipes is running","Right-click the tray icon for controls, new scenes or current screen settings.",ToolTipIcon.Info);
        } catch(Exception e) { StopWallpaper(); MessageBox.Show(this,e.Message,"Wallpaper",MessageBoxButtons.OK,MessageBoxIcon.Information); }
    }
    void StopWallpaper() {
        foreach(var w in wallpapers) w.Close(); wallpapers.Clear();
        if(wallpaperLock!=null) { if(ownsWallpaperLock) wallpaperLock.ReleaseMutex(); wallpaperLock.Dispose(); wallpaperLock=null; ownsWallpaperLock=false; }
        tray.Visible=false; status.Text="Wallpaper is stopped. Your original background is unchanged.";
    }
    protected override void OnFormClosing(FormClosingEventArgs e) { Save(); StopWallpaper(); watch.Dispose(); tray.Dispose(); base.OnFormClosing(e); }
}
}

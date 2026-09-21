using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace RetroPipes {
class Launcher : XpForm {
    Settings settings,activeTemplate; XpSlider speed,count; internal NumericUpDown speedNumber,countNumber;
    ComboBox palette; internal ComboBox modeSelector; CheckBox rotate,pipes,fireworks,bubbles,teapots; internal CheckBox spanScreens;
    CheckBox randSpeed,randCount,randPalette,randRotation,randPipes,randFireworks,randBubbles,randDensity,randTeapots;
    NumericUpDown rotationChance,pipesChance,fireworksChance,bubblesChance,teapotChance,fireworkRate,bubbleCount;
    bool persistSettings,activeSpan,wallpaperRequested; Rectangle[] activeDisplays; NotifyIcon tray; Label status,displayStatus; Timer watch=new Timer(); Image wordmark;
    List<PipesWindow> wallpapers=new List<PipesWindow>();
    Control fieldParent; Label speedLabel,countLabel,teapotLabel,fireworkLabel,bubbleLabel,modeHelp; XpButton stopButton; readonly List<Control> randomControls=new List<Control>(); readonly Random uiRandom=new Random(); ToolTip tips=new ToolTip();
    System.Threading.Mutex wallpaperLock; bool ownsWallpaperLock;
    public Launcher(Settings value) : this(value,true) { }
    internal Launcher(Settings value,bool persist) {
        settings=value; settings.Validate(); persistSettings=persist;
        SuspendLayout(); fieldParent=this;
        Text="Retro Pipes"; ClientSize=new Size(760,794); AutoScaleMode=AutoScaleMode.None;
        MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen;
        BackColor=XpTheme.Cream; ForeColor=XpTheme.Ink; Font=new Font("Tahoma",9);
        Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Label("Retro Pipes",26,51,390,39,22,Color.White);
        displayStatus=Label("",28,97,700,24,9,Color.LightSteelBlue); UpdateDisplayStatus();
        Label("Settings mode",28,140,120,25,10,Color.White);
        modeSelector=new ComboBox {Left=157,Top=136,Width=240,DropDownStyle=ComboBoxStyle.DropDownList};
        modeSelector.Items.AddRange(new object[]{"Manual","Randomized"}); Controls.Add(modeSelector);
        var fullRandom=Button("Full Random",560,131,172,delegate { FullRandom(); });
        tips.SetToolTip(fullRandom,"Pick fresh values for every animation setting and enable all shuffle options. Keeps your screen layout choice.");
        var frame=new GroupBox {Text="Scene settings",Left=22,Top=182,Width=716,Height=420,BackColor=XpTheme.Cream,ForeColor=XpTheme.Blue};
        Controls.Add(frame); fieldParent=frame;
        pipes=Check("Pipes",18,28,180,settings.Pipes); fireworks=Check("Fireworks",251,28,180,settings.Fireworks); bubbles=Check("Bubbles",484,28,180,settings.Bubbles);
        randPipes=Check("Shuffle",18,62,88,settings.RandomPipes); pipesChance=Number(108,63,62,0,100,settings.PipesChance,0);
        randFireworks=Check("Shuffle",251,62,88,settings.RandomFireworks); fireworksChance=Number(341,63,62,0,100,settings.FireworksChance,0);
        randBubbles=Check("Shuffle",484,62,88,settings.RandomBubbles); bubblesChance=Number(574,63,62,0,100,settings.BubblesChance,0);
        randomControls.AddRange(new Control[]{randPipes,pipesChance,randFireworks,fireworksChance,randBubbles,bubblesChance,
            Label("% on",174,65,54,24,9,Color.LightSteelBlue),Label("% on",407,65,54,24,9,Color.LightSteelBlue),Label("% on",640,65,58,24,9,Color.LightSteelBlue)});
        speedLabel=Label("Pipe speed",18,110,170,24,10,Color.White);
        speed=Slider(100,10,settings.Speed); speedNumber=Number(422,106,80,1,Settings.MaxSpeed,settings.Speed,0); Bind(speed,speedNumber);
        randSpeed=Check("Shuffle",534,105,160,settings.RandomSpeed);
        countLabel=Label("Pipe count",18,150,170,24,10,Color.White);
        count=Slider(140,9,settings.Count); countNumber=Number(422,146,80,1,Settings.MaxCount,settings.Count,0); Bind(count,countNumber);
        randCount=Check("Shuffle",534,145,160,settings.RandomCount);
        Label("Pipe colour",18,190,170,24,10,Color.White);
        palette=new ComboBox {Left=192,Top=186,Width=310,DropDownStyle=ComboBoxStyle.DropDownList};
        palette.Items.AddRange(new object[]{"Classic colours","Electric neon","Polished chrome"}); palette.SelectedIndex=settings.Palette; frame.Controls.Add(palette);
        randPalette=Check("Shuffle",534,185,160,settings.RandomPalette);
        rotate=Check("Slow rotation",18,225,180,settings.Rotate);
        rotationChance=Number(422,226,80,0,100,settings.RotationChance,0);
        randomControls.Add(Label("Chance on (%)",220,230,195,24,9,Color.LightSteelBlue)); randomControls.Add(rotationChance);
        randRotation=Check("Shuffle",534,225,160,settings.RandomRotation);
        teapots=Check("Teapot easter egg",18,265,200,settings.Teapots);
        teapotLabel=Label("Chance per bend (%)",220,270,195,24,9,Color.LightSteelBlue);
        teapotChance=Number(422,266,80,0,100,(decimal)settings.TeapotChance,2);
        randTeapots=Check("Shuffle",534,265,160,settings.RandomTeapots);
        fireworkLabel=Label("Firework intensity",18,310,220,24,10,Color.White); fireworkRate=Number(422,306,80,1,30,settings.FireworkRate,0);
        bubbleLabel=Label("Bubble count",18,350,220,24,10,Color.White); bubbleCount=Number(422,346,80,1,100,settings.BubbleCount,0);
        randDensity=Check("Shuffle both",534,325,165,settings.RandomEffectDensity);
        Label("Type a number beside a slider to go beyond its range.",18,385,670,23,9,Color.LightSteelBlue);
        randomControls.AddRange(new Control[]{randSpeed,randCount,randPalette,randRotation,randTeapots,randDensity});
        fieldParent=this;
        spanScreens=Check("One continuous scene across all detected screens",28,616,704,settings.SpanAllScreens);
        modeHelp=Label("",28,651,704,43,9,Color.LightSteelBlue);
        modeSelector.SelectedIndexChanged+=delegate { UpdateMode(); };
        spanScreens.CheckedChanged+=delegate { UpdateMode(); };
        foreach(var control in randomControls) {
            var check=control as CheckBox;
            if(check!=null) { check.CheckedChanged+=delegate { UpdateMode(); }; tips.SetToolTip(check,"Checked: choose a new value each scene. Unchecked: use the manual value."); }
        }
        tips.SetToolTip(randTeapots,"Randomize teapots on/off (50% chance), and the per-bend probability from zero to your entered maximum.");
        tips.SetToolTip(speedNumber,"Type 1–1000. The slider covers 1–10; larger typed values are retained.");
        tips.SetToolTip(countNumber,"Type 1–500. The slider covers 1–9; larger typed values are retained.");
        modeSelector.SelectedIndex=settings.RandomizeEachRun?1:0;
        Button("Window preview",28,703,164,delegate { Save(); new PipesWindow(settings,"window",DisplayLayout.PreviewBounds(settings.SpanAllScreens),IntPtr.Zero).Show(); });
        Button("Try screensaver",208,703,164,delegate { Save(); Process.Start(Application.ExecutablePath,"/s"); });
        Button("Start wallpaper",388,703,164,delegate { StartWallpaper(); });
        stopButton=Button("Stop wallpaper",568,703,164,delegate { StopWallpaper(); }); stopButton.Enabled=false;
        status=Label("Ready. Choose a preview, screensaver or wallpaper.",28,754,632,25,9,Color.LightSteelBlue);
        var credits=new LinkLabel {Text="Credits",Left=675,Top=754,Width=58,Height=23,LinkColor=XpTheme.Blue};
        credits.LinkClicked+=delegate {
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("RetroPipes.ThirdPartyNotices"))
            using(var reader=new StreamReader(stream)) MessageBox.Show(reader.ReadToEnd(),"Retro Pipes — third-party credits");
        }; Controls.Add(credits);
        var menu=new ContextMenuStrip(); menu.Items.Add("Open controls",null,delegate { Show(); WindowState=FormWindowState.Normal; Activate(); });
        menu.Items.Add("Roll new scenes on every screen",null,delegate { var owners=new HashSet<PipesWindow>(); foreach(var w in wallpapers) if(owners.Add(w.SceneOwner)) w.ResetScene(); });
        menu.Items.Add("Current screen settings",null,delegate {
            var lines=new List<string>(); for(int i=0;i<wallpapers.Count;i++) { if(activeSpan&&i>0) break; lines.Add((activeSpan?"Shared across "+wallpapers.Count+" screens":"Screen "+(i+1))+": "+wallpapers[i].Description); }
            MessageBox.Show(string.Join("\n\n",lines.ToArray()),"Each screen's current settings");
        });
        menu.Items.Add("Stop wallpaper",null,delegate { StopWallpaper(); Show(); });
        menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("Exit Retro Pipes",null,delegate { Close(); });
        tray=new NotifyIcon {Icon=Icon,Text="Retro Pipes — live wallpaper",ContextMenuStrip=menu,Visible=false};
        tray.DoubleClick+=delegate { Show(); Activate(); };
        menu.Renderer=new ToolStripSystemRenderer();
        watch.Interval=2000; watch.Tick+=delegate {
            UpdateDisplayStatus();
            if(wallpaperRequested&&(wallpapers.Count==0||!Native.IsWindow(wallpapers[0].DesktopParent)||!DisplayLayout.Same(activeDisplays,DisplayLayout.Screens()))) {
                try { RebuildWallpaperViews(); } catch { status.Text="Waiting for the Windows desktop. Wallpaper will resume automatically."; }
            }
        }; watch.Start();
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("RetroPipes.WindowsXPWordmark"))
        using(var original=Image.FromStream(stream)) wordmark=new Bitmap(original);
        var logo=new PictureBox {Left=544,Top=49,Width=186,Height=42,SizeMode=PictureBoxSizeMode.Zoom,Image=wordmark,BackColor=XpTheme.Cream}; Controls.Add(logo);
        var flag=new Panel {Left=486,Top=47,Width=51,Height=46,BackColor=XpTheme.Cream}; flag.Paint+=delegate(object sender,PaintEventArgs e) { e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias; XpTheme.Flag(e.Graphics,flag.ClientRectangle); }; Controls.Add(flag);
        ResumeLayout(false);
    }
    void UpdateMode() {
        bool random=modeSelector.SelectedIndex==1;
        SuspendLayout(); foreach(var control in randomControls) control.Visible=random;
        speedLabel.Text=random&&randSpeed.Checked?"Pipe speed (max)":"Pipe speed"; countLabel.Text=random&&randCount.Checked?"Pipe count (max)":"Pipe count";
        fireworkLabel.Text=random&&randDensity.Checked?"Firework intensity (max)":"Firework intensity";
        bubbleLabel.Text=random&&randDensity.Checked?"Bubble count (max)":"Bubble count";
        teapotLabel.Text=random&&randTeapots.Checked?"Max chance per bend (%)":"Chance per bend (%)";
        pipes.Enabled=!(random&&randPipes.Checked); fireworks.Enabled=!(random&&randFireworks.Checked); bubbles.Enabled=!(random&&randBubbles.Checked);
        rotate.Enabled=!(random&&randRotation.Checked); palette.Enabled=!(random&&randPalette.Checked); teapots.Enabled=!(random&&randTeapots.Checked);
        pipesChance.Enabled=randPipes.Checked; fireworksChance.Enabled=randFireworks.Checked; bubblesChance.Enabled=randBubbles.Checked; rotationChance.Enabled=randRotation.Checked;
        modeHelp.Text=random?"Checked Shuffle options change each scene. Max values limit their range; % on sets the chance.\n"+(spanScreens.Checked?"All screens share one roll. Growth speed stays the same.":"Each screen rolls its own settings independently."):
            "Choose one or more effects to overlap, then adjust their settings.\n"+(spanScreens.Checked?"One scene fills the whole desktop naturally, at the same growth speed.":"Each display generates its own scene using these settings.");
        ResumeLayout(false); Invalidate(true);
    }
    internal void FullRandom() {
        speedNumber.Value=uiRandom.Next(1,11); countNumber.Value=uiRandom.Next(1,10); palette.SelectedIndex=uiRandom.Next(3);
        rotate.Checked=uiRandom.Next(2)==1; teapots.Checked=uiRandom.Next(2)==1;
        int layers=uiRandom.Next(1,8); pipes.Checked=(layers&1)!=0; fireworks.Checked=(layers&2)!=0; bubbles.Checked=(layers&4)!=0;
        fireworkRate.Value=uiRandom.Next(1,31); bubbleCount.Value=uiRandom.Next(1,101);
        teapotChance.Value=uiRandom.Next(1,10001)/100m;
        rotationChance.Value=uiRandom.Next(101); pipesChance.Value=uiRandom.Next(101); fireworksChance.Value=uiRandom.Next(101); bubblesChance.Value=uiRandom.Next(101);
        randSpeed.Checked=randCount.Checked=randPalette.Checked=randRotation.Checked=true;
        randPipes.Checked=randFireworks.Checked=randBubbles.Checked=randDensity.Checked=randTeapots.Checked=true;
        modeSelector.SelectedIndex=1;
        status.Text="All animation settings shuffled. Preview or start to apply.";
    }
    Label Label(string text,int x,int y,int width,int height,int size,Color color) {
        var label=new Label {Text=text,Left=x,Top=y,Width=width,Height=height,Font=new Font("Tahoma",size,size>=18?FontStyle.Bold:FontStyle.Regular),ForeColor=color==Color.White?XpTheme.Ink:XpTheme.Blue,BackColor=XpTheme.Cream,UseMnemonic=false}; fieldParent.Controls.Add(label); return label;
    }
    CheckBox Check(string text,int x,int y,int width,bool value) { var c=new XpCheckBox {Text=text,Left=x,Top=y,Width=width,Height=25,Checked=value}; fieldParent.Controls.Add(c); return c; }
    XpSlider Slider(int y,int max,int value) { var s=new XpSlider {Left=192,Top=y,Width=220,Minimum=1,Maximum=max,Value=Math.Min(max,value)}; fieldParent.Controls.Add(s); return s; }
    NumericUpDown Number(int x,int y,int width,int min,int max,decimal value,int decimals) {
        var n=new NumericUpDown {Left=x,Top=y,Width=width,Minimum=min,Maximum=max,Value=value,DecimalPlaces=decimals,Increment=decimals>0?0.1m:1m,TextAlign=HorizontalAlignment.Right,BackColor=Color.White,ForeColor=XpTheme.Ink}; fieldParent.Controls.Add(n); return n;
    }
    void Bind(XpSlider slider,NumericUpDown number) {
        bool syncing=false;
        number.ValueChanged+=delegate { if(syncing) return; syncing=true; slider.Value=Math.Min(slider.Maximum,(int)number.Value); syncing=false; };
        slider.ValueChanged+=delegate { if(syncing) return; syncing=true; number.Value=slider.Value; syncing=false; };
    }
    XpButton Button(string text,int x,int y,int width,EventHandler action) { var b=new XpButton {Text=text,Left=x,Top=y,Width=width,Height=35,BackColor=XpTheme.Cream,ForeColor=XpTheme.Ink}; b.Click+=action; fieldParent.Controls.Add(b); return b; }
    void UpdateDisplayStatus() { var displays=DisplayLayout.Screens(); var area=DisplayLayout.Union(displays); displayStatus.Text=displays.Length+" display"+(displays.Length==1?"":"s")+" detected  |  Desktop: "+area.Width+" x "+area.Height+"  |  Automatic layout updates"; }
    void Save() {
        settings.Speed=(int)speedNumber.Value; settings.Count=(int)countNumber.Value; settings.Palette=palette.SelectedIndex; settings.Rotate=rotate.Checked;
        settings.Pipes=pipes.Checked; settings.Fireworks=fireworks.Checked; settings.Bubbles=bubbles.Checked; settings.Teapots=teapots.Checked; settings.TeapotChance=(double)teapotChance.Value;
        settings.FireworkRate=(int)fireworkRate.Value; settings.BubbleCount=(int)bubbleCount.Value;
        settings.RandomizeEachRun=modeSelector.SelectedIndex==1; settings.RandomTeapots=randTeapots.Checked; settings.RandomSpeed=randSpeed.Checked; settings.RandomCount=randCount.Checked; settings.RandomPalette=randPalette.Checked;
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
            activeTemplate=settings.Copy(); wallpaperRequested=true; RebuildWallpaperViews();
            stopButton.Enabled=true; tray.Visible=true; Hide(); tray.ShowBalloonTip(2500,"Retro Pipes is running","Right-click the tray icon for controls, new scenes or current screen settings.",ToolTipIcon.Info);
        } catch(Exception e) { StopWallpaper(); MessageBox.Show(this,e.Message,"Wallpaper",MessageBoxButtons.OK,MessageBoxIcon.Information); }
    }
    void RebuildWallpaperViews() {
        foreach(var view in wallpapers) view.CloseForReconfigure(); wallpapers.Clear();
        IntPtr host=Native.FindWallpaperHost(); if(host==IntPtr.Zero) throw new Exception("Windows did not expose a wallpaper surface. Try again after closing Task View.");
        activeDisplays=DisplayLayout.Screens(); activeSpan=activeTemplate.SpanAllScreens;
        wallpapers=PipesWindow.CreateViews(activeTemplate,"wallpaper",activeDisplays,host);
        status.Text=activeSpan?"One continuous scene across "+activeDisplays.Length+" screens. Growth speed is unchanged.":"Running on "+wallpapers.Count+" screens. Each screen has its own random cycle.";
    }
    void StopWallpaper() {
        wallpaperRequested=false;
        foreach(var w in wallpapers) w.Close(); wallpapers.Clear();
        if(wallpaperLock!=null) { if(ownsWallpaperLock) wallpaperLock.ReleaseMutex(); wallpaperLock.Dispose(); wallpaperLock=null; ownsWallpaperLock=false; }
        stopButton.Enabled=false; tray.Visible=false; status.Text="Wallpaper is stopped. Your original background is unchanged.";
    }
    protected override void OnFormClosing(FormClosingEventArgs e) { Save(); StopWallpaper(); watch.Dispose(); tray.Dispose(); tips.Dispose(); base.OnFormClosing(e); }
    protected override void OnFormClosed(FormClosedEventArgs e) { if(wordmark!=null) wordmark.Dispose(); base.OnFormClosed(e); }
}
}

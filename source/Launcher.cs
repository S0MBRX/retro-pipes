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
    ComboBox palette; internal ComboBox modeSelector; CheckBox rotate,pipes,fireworks,bubbles,dvd,teapots; internal CheckBox spanScreens;
    CheckBox randSpeed,randCount,randPalette,randRotation,randPipes,randFireworks,randBubbles,randDensity,randTeapots,randDvd;
    NumericUpDown rotationChance,pipesChance,fireworksChance,bubblesChance,teapotChance,fireworkRate,bubbleCount,dvdChance;
    bool persistSettings,activeSpan,wallpaperRequested; Rectangle[] activeDisplays; NotifyIcon tray; Label status,displayStatus; Timer watch=new Timer(); Image wordmark;
    List<PipesWindow> wallpapers=new List<PipesWindow>();
    Control fieldParent; Label speedLabel,countLabel,teapotLabel,fireworkLabel,bubbleLabel,modeHelp; XpButton stopButton,fullRandomButton; Panel advancedPanel; Label advancedInfo; readonly List<Control> randomControls=new List<Control>(); ToolTip tips=new ToolTip();
    System.Threading.Mutex wallpaperLock; bool ownsWallpaperLock;
    public Launcher(Settings value) : this(value,true) { }
    internal Launcher(Settings value,bool persist) {
        settings=value; settings.Validate(); persistSettings=persist;
        SuspendLayout(); fieldParent=this;
        Text="Retro Pipes"; ClientSize=new Size(760,646); AutoScaleMode=AutoScaleMode.None;
        MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen;
        BackColor=XpTheme.Cream; ForeColor=XpTheme.Ink; Font=new Font("Tahoma",9);
        Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Label("Retro Pipes",26,51,390,39,22,Color.White);
        displayStatus=Label("",28,97,700,24,9,Color.LightSteelBlue); UpdateDisplayStatus();
        Label("Settings mode",28,140,120,25,10,Color.White);
        modeSelector=new ComboBox {Left=157,Top=136,Width=240,DropDownStyle=ComboBoxStyle.DropDownList};
        modeSelector.Items.AddRange(new object[]{"Manual","Randomized"}); Controls.Add(modeSelector);
        Button("Advanced...",414,131,130,delegate { using(var dialog=CreateAdvancedWindow()) dialog.ShowDialog(this); });
        fullRandomButton=Button("Full Random: Off",560,131,172,delegate { FullRandom(); });
        tips.SetToolTip(fullRandomButton,"Turn every randomization option on. Click again to restore basic defaults. Keeps your screen layout choice.");
        var frame=new GroupBox {Text="Scene settings",Left=22,Top=182,Width=716,Height=265,BackColor=XpTheme.Cream,ForeColor=XpTheme.Blue};
        Controls.Add(frame); fieldParent=frame;
        pipes=Check("Pipes",18,28,150,settings.Pipes); fireworks=Check("Fireworks",192,28,150,settings.Fireworks); bubbles=Check("Bubbles",366,28,150,settings.Bubbles); dvd=Check("DVD logo",540,28,150,settings.Dvd);
        speedLabel=Label("Pipe speed",18,70,170,24,10,Color.White);
        speed=Slider(60,25,settings.Speed); speedNumber=Number(422,66,80,1,Settings.MaxSpeed,settings.Speed,0); Bind(speed,speedNumber);
        countLabel=Label("Pipe count",18,110,170,24,10,Color.White);
        count=Slider(100,10,settings.Count); countNumber=Number(422,106,80,1,Settings.MaxCount,settings.Count,0); Bind(count,countNumber);
        Label("Colour preset",18,150,170,24,10,Color.White);
        palette=new ComboBox {Left=192,Top=146,Width=310,DropDownStyle=ComboBoxStyle.DropDownList};
        palette.Items.AddRange(new object[]{"Classic colours","Electric neon","Polished chrome"}); palette.SelectedIndex=settings.Palette; frame.Controls.Add(palette);
        rotate=Check("Drifting camera orbit",18,185,260,settings.Rotate);
        Label("Type beside a slider for larger values. More options are in Advanced.",18,226,680,23,9,Color.LightSteelBlue);

        advancedPanel=new Panel {Left=18,Top=48,Width=680,Height=430,BackColor=XpTheme.Cream}; fieldParent=advancedPanel;
        teapots=Check("Teapot easter egg",18,12,200,settings.Teapots);
        teapotLabel=Label("Chance per bend (%)",228,17,182,24,9,Color.LightSteelBlue);
        teapotChance=Number(414,13,80,0,100,(decimal)settings.TeapotChance,2);
        randTeapots=Check("Shuffle",518,12,144,settings.RandomTeapots);
        fireworkLabel=Label("Firework intensity",18,58,270,24,10,Color.White); fireworkRate=Number(414,54,80,1,30,settings.FireworkRate,0);
        bubbleLabel=Label("Bubble count",18,98,270,24,10,Color.White); bubbleCount=Number(414,94,80,1,100,settings.BubbleCount,0);
        randDensity=Check("Shuffle both",518,74,144,settings.RandomEffectDensity);
        advancedInfo=Label("",18,143,644,40,9,Color.LightSteelBlue);
        randSpeed=Check("Shuffle speed",18,188,196,settings.RandomSpeed); randCount=Check("Shuffle count",236,188,196,settings.RandomCount); randPalette=Check("Shuffle colour",454,188,204,settings.RandomPalette);
        randomControls.Add(Label("Rotation chance (%)",18,232,320,24,10,Color.White)); rotationChance=Number(414,228,80,0,100,settings.RotationChance,0); randRotation=Check("Shuffle",518,228,144,settings.RandomRotation);
        randomControls.Add(Label("Pipes chance (%)",18,274,320,24,10,Color.White)); pipesChance=Number(414,270,80,0,100,settings.PipesChance,0); randPipes=Check("Shuffle",518,270,144,settings.RandomPipes);
        randomControls.Add(Label("Fireworks chance (%)",18,316,320,24,10,Color.White)); fireworksChance=Number(414,312,80,0,100,settings.FireworksChance,0); randFireworks=Check("Shuffle",518,312,144,settings.RandomFireworks);
        randomControls.Add(Label("Bubbles chance (%)",18,358,320,24,10,Color.White)); bubblesChance=Number(414,354,80,0,100,settings.BubblesChance,0); randBubbles=Check("Shuffle",518,354,144,settings.RandomBubbles);
        randomControls.Add(Label("DVD chance (%)",18,400,320,24,10,Color.White)); dvdChance=Number(414,396,80,0,100,settings.DvdChance,0); randDvd=Check("Shuffle",518,396,144,settings.RandomDvd);
        randomControls.AddRange(new Control[]{randDvd,dvdChance,randSpeed,randCount,randPalette,randRotation,randTeapots,randDensity,randPipes,randFireworks,randBubbles,rotationChance,pipesChance,fireworksChance,bubblesChance});
        randomControls.Add(Label("Untick Shuffle to use a fixed manual value. Chances apply at each scene reset.",18,442,644,25,9,Color.LightSteelBlue));
        fieldParent=this;
        spanScreens=Check("One continuous scene across all detected screens",28,462,704,settings.SpanAllScreens);
        modeHelp=Label("",28,500,704,43,9,Color.LightSteelBlue);
        modeSelector.SelectedIndexChanged+=delegate { UpdateMode(); };
        spanScreens.CheckedChanged+=delegate { UpdateMode(); };
        foreach(var control in randomControls) {
            var check=control as CheckBox;
            if(check!=null) { check.CheckedChanged+=delegate { UpdateMode(); }; tips.SetToolTip(check,"Checked: choose a new value each scene. Unchecked: use the manual value."); }
        }
        tips.SetToolTip(randTeapots,"Randomize teapots on/off (50% chance), and the per-bend probability from zero to your entered maximum.");
        tips.SetToolTip(speedNumber,"Type 1–1000. The slider covers 1–25; larger typed values are retained.");
        tips.SetToolTip(countNumber,"Type 1–500. The slider covers 1–10; larger typed values are retained.");
        modeSelector.SelectedIndex=settings.RandomizeEachRun?1:0;
        Button("Window preview",28,555,164,delegate { Save(); new PipesWindow(settings,"window",DisplayLayout.PreviewBounds(settings.SpanAllScreens),IntPtr.Zero).Show(); });
        Button("Try screensaver",208,555,164,delegate { Save(); Process.Start(Application.ExecutablePath,"/s"); });
        Button("Start wallpaper",388,555,164,delegate { StartWallpaper(); });
        stopButton=Button("Stop wallpaper",568,555,164,delegate { StopWallpaper(); }); stopButton.Enabled=false;
        status=Label("Ready. Choose a preview, screensaver or wallpaper.",28,606,632,25,9,Color.LightSteelBlue);
        var credits=new LinkLabel {Text="Credits",Left=675,Top=606,Width=58,Height=23,LinkColor=XpTheme.Blue};
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
        pipes.Enabled=!(random&&randPipes.Checked); fireworks.Enabled=!(random&&randFireworks.Checked); bubbles.Enabled=!(random&&randBubbles.Checked); dvd.Enabled=!(random&&randDvd.Checked);
        rotate.Enabled=!(random&&randRotation.Checked); palette.Enabled=!(random&&randPalette.Checked); teapots.Enabled=!(random&&randTeapots.Checked);
        pipesChance.Enabled=randPipes.Checked; fireworksChance.Enabled=randFireworks.Checked; bubblesChance.Enabled=randBubbles.Checked; rotationChance.Enabled=randRotation.Checked; dvdChance.Enabled=randDvd.Checked;
        fullRandomButton.Text=FullRandomEnabled?"Full Random: On":"Full Random: Off";
        advancedInfo.Text=random?"Choose which settings change each scene. Max values limit their range; percentages set the chance.":"Choose Randomized in the main window to adjust individual shuffle options and chances.";
        modeHelp.Text=random?"Settings shuffle each scene. Use Advanced to choose what changes and adjust its chances.\n"+(spanScreens.Checked?"All screens share one roll. Growth speed stays the same.":"Each screen rolls its own settings independently."):
            "Choose one or more effects to overlap, then adjust their settings.\n"+(spanScreens.Checked?"One scene fills the whole desktop naturally, at the same growth speed.":"Each display generates its own scene using these settings.");
        ResumeLayout(false); Invalidate(true);
    }
    internal bool FullRandomEnabled { get { return modeSelector.SelectedIndex==1&&randSpeed.Checked&&randCount.Checked&&randPalette.Checked&&randRotation.Checked&&randPipes.Checked&&randFireworks.Checked&&randBubbles.Checked&&randDensity.Checked&&randTeapots.Checked&&randDvd.Checked; } }
    internal XpForm CreateAdvancedWindow() {
        bool random=modeSelector.SelectedIndex==1;
        var dialog=new XpForm {Text="Retro Pipes - Advanced Settings",ClientSize=new Size(716,random?592:302),StartPosition=FormStartPosition.CenterParent,Icon=Icon};
        advancedPanel.Height=random?472:188; dialog.Controls.Add(advancedPanel);
        var done=new XpButton {Text="Done",Left=568,Top=dialog.ClientSize.Height-52,Width=126,Height=35,BackColor=XpTheme.Cream,DialogResult=DialogResult.OK};
        dialog.Controls.Add(done); dialog.AcceptButton=done; dialog.CancelButton=done;
        // Keep the same live controls and values when the temporary dialog is disposed.
        dialog.FormClosed+=delegate { dialog.Controls.Remove(advancedPanel); };
        return dialog;
    }
    internal void FullRandom() {
        if(FullRandomEnabled) {
            var defaults=new Settings();
            modeSelector.SelectedIndex=0;
            speedNumber.Value=defaults.Speed; countNumber.Value=defaults.Count; palette.SelectedIndex=defaults.Palette;
            rotate.Checked=defaults.Rotate; pipes.Checked=defaults.Pipes; fireworks.Checked=defaults.Fireworks; bubbles.Checked=defaults.Bubbles; dvd.Checked=defaults.Dvd;
            teapots.Checked=defaults.Teapots; teapotChance.Value=(decimal)defaults.TeapotChance;
            fireworkRate.Value=defaults.FireworkRate; bubbleCount.Value=defaults.BubbleCount;
            rotationChance.Value=defaults.RotationChance; pipesChance.Value=defaults.PipesChance; fireworksChance.Value=defaults.FireworksChance; bubblesChance.Value=defaults.BubblesChance; dvdChance.Value=defaults.DvdChance;
            randSpeed.Checked=defaults.RandomSpeed; randCount.Checked=defaults.RandomCount; randPalette.Checked=defaults.RandomPalette; randRotation.Checked=defaults.RandomRotation;
            randPipes.Checked=defaults.RandomPipes; randFireworks.Checked=defaults.RandomFireworks; randBubbles.Checked=defaults.RandomBubbles; randDvd.Checked=defaults.RandomDvd;
            randDensity.Checked=defaults.RandomEffectDensity; randTeapots.Checked=defaults.RandomTeapots;
            status.Text="Basic defaults restored. Preview or start to apply.";
        } else {
            pipes.Checked=fireworks.Checked=bubbles.Checked=dvd.Checked=rotate.Checked=teapots.Checked=true;
            randSpeed.Checked=randCount.Checked=randPalette.Checked=randRotation.Checked=true;
            randPipes.Checked=randFireworks.Checked=randBubbles.Checked=randDvd.Checked=randDensity.Checked=randTeapots.Checked=true;
            modeSelector.SelectedIndex=1;
            status.Text="All randomization enabled. Click Full Random again for defaults.";
        }
        UpdateMode();
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
        settings.Pipes=pipes.Checked; settings.Fireworks=fireworks.Checked; settings.Bubbles=bubbles.Checked; settings.Dvd=dvd.Checked; settings.RandomDvd=randDvd.Checked; settings.DvdChance=(int)dvdChance.Value; settings.Teapots=teapots.Checked; settings.TeapotChance=(double)teapotChance.Value;
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
    protected override void OnFormClosed(FormClosedEventArgs e) { if(wordmark!=null) wordmark.Dispose(); advancedPanel.Dispose(); base.OnFormClosed(e); }
}
}

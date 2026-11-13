using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace RetroPipes {
static class VisualizerTests {
    // A generated 120 BPM test signal (kick, bass line, chords, melody, hi-hats).
    // It is analysed directly and never played through the speakers.
    sealed class TestTone {
        public const int Rate=48000; readonly Random noise; double kickPhase,bassPhase,leadPhase,hatLast; long position;
        static readonly double[] bassNotes={55,55,73.42,65.41,49,49,61.74,65.41},lead={440,523.25,587.33,659.26,783.99,659.26,587.33,523.25};
        public TestTone(int seed) { noise=new Random(seed); }
        public void Fill(float[] buffer,int count) {
            for(int i=0;i<count;i++,position++) {
                double t=(double)position/Rate; long inBeatSamples=position%(Rate/2); double inBeat=(double)inBeatSamples/Rate; int beat=(int)(position/(Rate/2));
                if(inBeatSamples==0) kickPhase=0;
                kickPhase+=2*Math.PI*(48+95*Math.Exp(-inBeat*28))/Rate;
                double kick=inBeat<0.3?0.95*Math.Exp(-inBeat*9)*Math.Sin(kickPhase):0;
                bassPhase+=2*Math.PI*bassNotes[(beat/2)%bassNotes.Length]/Rate;
                double bass=0.22*(Math.Sin(bassPhase)+0.35*Math.Sin(2*bassPhase))*(0.55+0.45*Math.Exp(-inBeat*5));
                double pad=0.045*(Math.Sin(2*Math.PI*220*t)+Math.Sin(2*Math.PI*277.18*t)+Math.Sin(2*Math.PI*329.63*t))*(0.7+0.3*Math.Sin(2*Math.PI*0.25*t));
                leadPhase+=2*Math.PI*lead[(int)(t/0.25)%lead.Length]/Rate;
                double melody=0.09*Math.Sin(leadPhase)*Math.Exp(-(t%0.25)*6),hat=0,offBeat=inBeat-0.25;
                if(offBeat>=0&&offBeat<0.08) { double n=noise.NextDouble()*2-1; hat=(n-hatLast)*0.22*Math.Exp(-offBeat*70); hatLast=n; }
                buffer[i]=(float)((kick+bass+pad+melody+hat)*0.6);
            }
        }
    }
    sealed class Listener {
        public readonly AudioAnalyzer Analyzer=new AudioAnalyzer(); readonly TestTone tone; readonly float[] buffer=new float[TestTone.Rate/30]; public bool Mute;
        public Listener(int seed) { tone=seed<0?null:new TestTone(seed); }
        public AudioFrame Heard() { return Analyzer.Latest; }
        public void Frame() { if(tone!=null&&!Mute) tone.Fill(buffer,buffer.Length); else Array.Clear(buffer,0,buffer.Length); Analyzer.Push(buffer,buffer.Length,TestTone.Rate); }
    }
    static AudioFrame Hear(Func<double,double> signal,double seconds,int rate) {
        var analyzer=new AudioAnalyzer(); var buffer=new float[480]; long n=0;
        while(n<seconds*rate) { for(int i=0;i<buffer.Length;i++,n++) buffer[i]=(float)signal((double)n/rate); analyzer.Push(buffer,buffer.Length,rate); }
        return analyzer.Latest;
    }
    static int Loudest(AudioFrame f) { int best=0; for(int i=1;i<f.Spectrum.Length;i++) if(f.Spectrum[i]>f.Spectrum[best]) best=i; return best; }
    static PipesWindow Window(Settings settings,Rectangle bounds,Visualizer replacement) {
        var window=new PipesWindow(settings,"test",bounds,IntPtr.Zero,61); window.Show(); Application.DoEvents();
        if(replacement!=null) { if(window.Visualizer!=null) window.Visualizer.Dispose(); window.Visualizer=replacement; }
        return window;
    }
    static void Play(PipesWindow window,Listener listener,int frames) {
        for(int i=0;i<frames;i++) { listener.Frame(); window.Advance(1/30.0); window.Draw(false); }
    }
    static double Brightness(string path,out double lit) {
        using(var image=new Bitmap(path)) {
            double sum=0; int count=0,on=0;
            for(int y=0;y<image.Height;y+=3) for(int x=0;x<image.Width;x+=3) { var c=image.GetPixel(x,y); int v=c.R+c.G+c.B; sum+=v/3.0; count++; if(v>45) on++; }
            lit=(double)on/count; return sum/count;
        }
    }
    public static void Run(string dir,List<string> log) {
        // FFT bands, bars, scope trigger and beats from generated signals.
        if(Settings.VisualizerPresets!=Visualizer.Names.Length) throw new Exception("Visualizer preset count mismatch");
        var low=Hear(t=>0.5*Math.Sin(2*Math.PI*70*t),1,48000); var mid=Hear(t=>0.5*Math.Sin(2*Math.PI*1000*t),1,48000); var high=Hear(t=>0.5*Math.Sin(2*Math.PI*6000*t),1,48000);
        if(low.Bass<0.8||low.Mid>0.3||low.Treble>0.3) throw new Exception("A 70 Hz tone is not bass: "+low.Bass+"/"+low.Mid+"/"+low.Treble);
        if(mid.Mid<0.8||mid.Bass>0.3||mid.Treble>0.3) throw new Exception("A 1 kHz tone is not mid: "+mid.Bass+"/"+mid.Mid+"/"+mid.Treble);
        if(high.Treble<0.8||high.Bass>0.3||high.Mid>0.3) throw new Exception("A 6 kHz tone is not treble: "+high.Bass+"/"+high.Mid+"/"+high.Treble);
        if(Math.Abs(Loudest(low)-2)>1||Math.Abs(Loudest(mid)-16)>1||Math.Abs(Loudest(high)-26)>1) throw new Exception("Spectrum bars peak at the wrong band: "+Loudest(low)+"/"+Loudest(mid)+"/"+Loudest(high));
        if(!low.Live||!mid.Live||low.Level<0.8) throw new Exception("Playing tones were not heard as live audio");
        if(Math.Abs(mid.Wave[0])>0.4||mid.Wave[2]<=mid.Wave[0]) throw new Exception("Scope trace is not triggered on a rising zero crossing");
        var resampled=Hear(t=>0.5*Math.Sin(2*Math.PI*1000*t),1,192000);
        if(resampled.Mid<0.8||Math.Abs(Loudest(resampled)-16)>1) throw new Exception("192 kHz audio is analysed at the wrong rate");
        var silence=Hear(t=>0,2,48000);
        if(silence.Live||silence.Level!=0||silence.Bass!=0||silence.Beats!=0) throw new Exception("Silence was heard as sound");
        var steady=Hear(t=>0.5*Math.Sin(2*Math.PI*70*t),6,48000);
        if(steady.Beats>1) throw new Exception("A steady tone produced beats: "+steady.Beats);
        var music=new Listener(3); for(int i=0;i<240;i++) music.Frame();
        int beats=music.Analyzer.Latest.Beats;
        if(beats<12||beats>18) throw new Exception("Beat detection found "+beats+" kicks in 8 seconds of 120 BPM (16 expected)");
        log.Add("PASS: in-house FFT bass/mid/treble bands, 32 log bars, 48/192 kHz input, triggered scope, silence, and "+beats+"/16 beats in a 120 BPM test signal.");

        // Reacting, idling and cycling.
        var listener=new Listener(5); var heard=new Visualizer(0,5,listener.Heard); double bassLow=1,bassHigh=0;
        for(int i=0;i<300;i++) { listener.Frame(); heard.Step(1/30.0); if(i>60) { bassLow=Math.Min(bassLow,heard.Bass); bassHigh=Math.Max(bassHigh,heard.Bass); } }
        if(heard.Energy<0.9||heard.BeatCount<12||bassHigh-bassLow<0.3) throw new Exception("Visualizer did not react: energy "+heard.Energy+", beats "+heard.BeatCount+", bass "+bassLow+"-"+bassHigh);
        var fading=new Listener(6); var stopped=new Visualizer(0,5,fading.Heard);
        for(int i=0;i<150;i++) { fading.Frame(); stopped.Step(1/30.0); }
        if(stopped.Energy<0.9) throw new Exception("Visualizer did not hear the test signal");
        fading.Mute=true; for(int i=0;i<180;i++) { fading.Frame(); stopped.Step(1/30.0); }
        if(stopped.Energy>0.1||double.IsNaN(stopped.Bass)||stopped.Bass<0.1||stopped.Motion<=0) throw new Exception("Visualizer did not settle into its idle animation");
        var cycling=new Visualizer(-1,9,delegate { return AudioFrame.Silent; }); var presets=new HashSet<int>(); int last=cycling.Current.Preset;
        for(int i=0;i<40;i++) { cycling.NewScene(); for(int f=0;f<90;f++) cycling.Step(1/30.0); if(cycling.Current.Preset==last) throw new Exception("Random preset repeated"); last=cycling.Current.Preset; presets.Add(last); }
        if(presets.Count!=Visualizer.Names.Length) throw new Exception("Random cycling used only "+presets.Count+" presets");
        var timed=new Visualizer(-1,10,delegate { return AudioFrame.Silent; }); int first=timed.Current.Preset;
        for(int i=0;i<65*30;i++) timed.Step(1/30.0);
        if(timed.Current.Preset==first) throw new Exception("Presets do not cycle on their own");
        var pinned=new Visualizer(3,11,delegate { return AudioFrame.Silent; });
        for(int i=0;i<10;i++) { pinned.NewScene(); pinned.Step(3); if(pinned.Current.Preset!=3) throw new Exception("A chosen preset changed"); }
        log.Add("PASS: visualizer reacts (bass "+bassLow.ToString("F2")+"-"+bassHigh.ToString("F2")+", "+heard.BeatCount+" beat pulses), fades to a calm idle when audio stops, cycles all "+presets.Count+" presets without repeats and keeps a chosen preset.");

        // Settings: opt-in, saved, shuffled only when asked.
        var defaults=new Settings();
        if(defaults.Visualizer||defaults.RandomVisualizer||defaults.VisualizerPreset!=-1) throw new Exception("Visualizer is not opt-in by default");
        using(var oldXml=new StringReader("<Settings><Pipes>false</Pipes><Fireworks>true</Fireworks><SpanAllScreens>true</SpanAllScreens></Settings>")) {
            var migrated=(Settings)new XmlSerializer(typeof(Settings)).Deserialize(oldXml); migrated.Validate();
            if(migrated.Visualizer||migrated.RandomVisualizer||!migrated.Fireworks||migrated.Pipes) throw new Exception("Older settings files changed layers");
        }
        var chosen=new Settings {Pipes=false,Visualizer=true,RandomVisualizer=true,VisualizerChance=61,VisualizerPreset=4}; chosen.Validate();
        if(chosen.Pipes) throw new Exception("Visualizer-only settings force pipes back on");
        using(var saved=new MemoryStream()) {
            var xml=new XmlSerializer(typeof(Settings)); xml.Serialize(saved,chosen); saved.Position=0; var restored=(Settings)xml.Deserialize(saved);
            if(!restored.Visualizer||!restored.RandomVisualizer||restored.VisualizerChance!=61||restored.VisualizerPreset!=4) throw new Exception("Visualizer settings lost after save");
        }
        var clamp=new Settings {VisualizerPreset=99,VisualizerChance=300}; clamp.Validate();
        if(clamp.VisualizerPreset!=Visualizer.Names.Length-1||clamp.VisualizerChance!=100) throw new Exception("Visualizer values are not clamped");
        var everything=new Settings {RandomizeEachRun=true,RandomSpeed=true,RandomCount=true,RandomPalette=true,RandomRotation=true,RandomPipes=true,RandomFireworks=true,RandomBubbles=true,RandomDvd=true,RandomEffectDensity=true,RandomTeapots=true};
        var rng=new Random(8); for(int i=0;i<300;i++) if(everything.ForRun(rng).Visualizer) throw new Exception("Visualizer joined random scenes without being enabled");
        var never=new Settings {RandomizeEachRun=true,RandomVisualizer=true,VisualizerChance=0}; var always=new Settings {RandomizeEachRun=true,RandomVisualizer=true,VisualizerChance=100};
        if(never.ForRun(new Random(2)).Visualizer||!always.ForRun(new Random(2)).Visualizer) throw new Exception("Visualizer chance endpoints failed");
        var keep=new Settings {RandomizeEachRun=true,Pipes=false,Visualizer=true,RandomPipes=true,PipesChance=0,RandomVisualizer=true,VisualizerChance=0}.ForRun(new Random(2));
        if(!keep.Visualizer||keep.Pipes) throw new Exception("Empty random scene did not keep the manual visualizer");
        log.Add("PASS: visualizer settings are opt-in, saved, clamped, migrate from older files, join random scenes only when shuffled, and survive the empty-scene fallback.");

        // Rendering every preset, idle and layered, in windows and shared views.
        var lights=new List<string>();
        for(int preset=0;preset<Visualizer.Names.Length;preset++) foreach(bool idle in new[]{false,true}) {
            var source=new Listener(idle?-1:preset+20);
            using(var window=Window(new Settings {Pipes=false,Visualizer=true,VisualizerPreset=preset},new Rectangle(0,0,640,400),new Visualizer(preset,30+preset,source.Heard))) {
                Play(window,source,idle?150:120);
                string path=Path.Combine(dir,"Visualizer-"+preset+(idle?"-idle":"")+".png"); window.SaveFrame(path);
                double lit; double brightness=Brightness(path,out lit);
                if(brightness<4||lit<0.02) throw new Exception(Visualizer.Names[preset]+(idle?" idle":"")+" rendered an almost black screen ("+brightness.ToString("F1")+", "+lit.ToString("P1")+" lit)");
                if(!idle&&!window.Description.Contains("Visualizer ("+Visualizer.Names[preset]+")")) throw new Exception("Screen settings omit the visualizer");
                if(idle) lights.Add(Visualizer.Names[preset]+" "+lit.ToString("P0"));
                window.Close();
            }
        }
        var layered=new Listener(7);
        using(var window=Window(new Settings {Pipes=true,Fireworks=true,Bubbles=true,Dvd=true,Visualizer=true,Rotate=true},new Rectangle(0,0,800,500),new Visualizer(-1,12,layered.Heard))) {
            for(int i=0;i<6;i++) { Play(window,layered,40); window.Visualizer.NewScene(); }
            window.SaveFrame(Path.Combine(dir,"Visualizer-layered.png")); window.Close();
        }
        using(var tiny=Window(new Settings {Pipes=false,Visualizer=true},new Rectangle(0,0,152,112),new Visualizer(1,13,layered.Heard))) { Play(tiny,layered,30); tiny.SaveFrame(Path.Combine(dir,"Visualizer-tiny.png")); tiny.Close(); }
        log.Add("PASS: all "+Visualizer.Names.Length+" presets render while reacting and while idle (never black; idle lit area "+string.Join(", ",lights.ToArray())+"), crossfade between presets, layer under pipes/fireworks/bubbles/DVD and fit a 152x112 preview.");
        var layout=new Rectangle[3]; for(int i=0;i<3;i++) layout[i]=new Rectangle(i*320-640,0,320,200);
        var spanning=new Settings {SpanAllScreens=true,Pipes=false,Visualizer=true,VisualizerPreset=1};
        var views=PipesWindow.CreateViews(spanning,"test",layout,IntPtr.Zero); var canvas=DisplayLayout.Union(layout); PipesWindow reference=null;
        try {
            var shared=new Listener(4); views[0].Visualizer.Dispose(); views[0].Visualizer=new Visualizer(1,14,shared.Heard);
            reference=new PipesWindow(spanning,"test",canvas,IntPtr.Zero,37,null,canvas); reference.Show(); Application.DoEvents();
            reference.Visualizer.Dispose(); reference.Visualizer=views[0].Visualizer;
            for(int frame=0;frame<90;frame++) { shared.Frame(); views[0].Advance(1/30.0); foreach(var view in views) view.Draw(false); reference.Draw(false); }
            for(int i=0;i<views.Count;i++) { if(views[i].SceneOwner!=views[0]) throw new Exception("Spanning views use separate visualizers"); views[i].SaveFrame(Path.Combine(dir,"Visualizer-view-"+i+".png")); }
            reference.SaveFrame(Path.Combine(dir,"Visualizer-reference.png"));
            int different=0,total=0;
            using(var full=new Bitmap(Path.Combine(dir,"Visualizer-reference.png")))
            for(int i=0;i<views.Count;i++) using(var slice=new Bitmap(Path.Combine(dir,"Visualizer-view-"+i+".png"))) {
                for(int y=0;y<slice.Height;y+=2) for(int x=0;x<slice.Width;x+=2) {
                    var a=full.GetPixel(i*320+x,y); var b=slice.GetPixel(x,y); total++;
                    if(Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B)>24) different++;
                }
            }
            if(different>total/50) throw new Exception("Spanned visualizer slices do not match one continuous render: "+different+" / "+total);
            reference.Visualizer=null;
        } finally { if(reference!=null) reference.Close(); foreach(var view in views) view.CloseForReconfigure(); }
        log.Add("PASS: one visualizer spans 3 screens as a seamless mirrored panorama; each screen's slice matches a single full-desktop render.");

        // Audio is only captured while a visualizer is showing.
        using(var plain=Window(new Settings {Fireworks=true,Dvd=true},new Rectangle(0,0,320,200),null)) {
            if(plain.Visualizer!=null||AudioCapture.Running) throw new Exception("Audio capture started without the visualizer");
            plain.Close();
        }
        using(var live=Window(new Settings {Pipes=false,Visualizer=true},new Rectangle(0,0,320,200),null)) {
            if(live.Visualizer==null||!AudioCapture.Running) throw new Exception("Visualizer did not start listening");
            var clock=Stopwatch.StartNew(); bool heardLive=false;
            while(clock.Elapsed.TotalSeconds<3&&!AudioCapture.Status.StartsWith("Listening")) { Thread.Sleep(50); Application.DoEvents(); }
            string status=AudioCapture.Status; clock.Restart();
            while(clock.Elapsed.TotalSeconds<1.5) { live.Advance(1/30.0); live.Draw(false); heardLive|=AudioCapture.Latest().Live; Thread.Sleep(33); }
            live.SaveFrame(Path.Combine(dir,"Visualizer-live.png")); live.Close();
            if(AudioCapture.Running) throw new Exception("Audio capture kept running after the visualizer closed");
            log.Add((status.StartsWith("Listening")?"PASS: WASAPI loopback capture opened. ":"NOTE: ")+status+" Sound playing during the test: "+(heardLive?"yes":"no")+".");
        }
        var timer=Stopwatch.StartNew(); var bench=new Listener(9);
        using(var window=Window(new Settings {Pipes=false,Visualizer=true},new Rectangle(0,0,1200,750),new Visualizer(1,15,bench.Heard))) {
            Play(window,bench,30); GL.glFinish(); timer.Restart();
            for(int i=0;i<60;i++) { bench.Frame(); window.Advance(1/30.0); window.Draw(false); } GL.glFinish(); timer.Stop();
            log.Add("Visualizer benchmark: "+(timer.Elapsed.TotalMilliseconds/60).ToString("F2")+" ms/frame at 1200x750 including audio analysis (Kaleidoscope).");
            window.Close();
        }

        // Settings window: checkbox, preset list, chance and Full Random behaviour.
        var ui=new Settings {SpanAllScreens=true};
        using(var launcher=new Launcher(ui,false)) {
            launcher.StartPosition=FormStartPosition.Manual; launcher.Location=new Point(-16000,-16000); launcher.Show(); Application.DoEvents();
            if(launcher.visualizer.Checked||launcher.visualizerPreset.SelectedIndex!=0||launcher.visualizerPreset.Items.Count!=Visualizer.Names.Length+1) throw new Exception("Visualizer controls do not start at their defaults");
            launcher.FullRandom();
            if(launcher.visualizer.Checked||launcher.randVisualizer.Checked) throw new Exception("Full Random switched the visualizer on");
            launcher.FullRandom();
            launcher.visualizer.Checked=true; launcher.visualizerPreset.SelectedIndex=3; launcher.modeSelector.SelectedIndex=1; launcher.randVisualizer.Checked=true; launcher.visualizerChance.Value=64;
            if(launcher.visualizer.Enabled||!launcher.visualizerChance.Enabled) throw new Exception("Visualizer shuffle does not dim the manual switch");
            launcher.Close();
        }
        if(!ui.Visualizer||!ui.RandomVisualizer||ui.VisualizerChance!=64||ui.VisualizerPreset!=2||!ui.SpanAllScreens) throw new Exception("Visualizer controls were not saved");
        var reopened=ui.Copy();
        using(var launcher=new Launcher(reopened,false)) {
            launcher.StartPosition=FormStartPosition.Manual; launcher.Location=new Point(-16000,-16000); launcher.Show(); Application.DoEvents();
            if(!launcher.visualizer.Checked||launcher.visualizerPreset.SelectedIndex!=3) throw new Exception("Reopened controls lost the visualizer choice");
            launcher.FullRandom(); launcher.FullRandom(); launcher.Close();
        }
        if(reopened.Visualizer||reopened.RandomVisualizer||reopened.VisualizerPreset!=-1||reopened.VisualizerChance!=35) throw new Exception("Full Random defaults did not reset the visualizer");
        log.Add("PASS: settings window visualizer switch, preset list, chance and Shuffle save and reopen; Full Random leaves the visualizer alone and its defaults switch it off.");
        Preview(dir,"Visualizer-preview.png",false);
    }
    // README preview: every preset reacting to the generated test signal (or idle).
    internal static void Preview(string dir,string name,bool idle) {
        const int tileWidth=480,tileHeight=270;
        using(var sheet=new Bitmap(tileWidth*4,tileHeight*2)) {
            using(var g=Graphics.FromImage(sheet)) {
                g.Clear(Color.Black); g.InterpolationMode=InterpolationMode.HighQualityBicubic; g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using(var font=new Font("Tahoma",11,FontStyle.Bold))
                for(int preset=0;preset<Visualizer.Names.Length;preset++) {
                    var source=new Listener(idle?-1:100+preset); string path=Path.Combine(dir,"Visualizer-sheet-"+preset+".png");
                    using(var window=Window(new Settings {Pipes=false,Visualizer=true,VisualizerPreset=preset},new Rectangle(0,0,960,540),new Visualizer(preset,200+preset,source.Heard))) {
                        Play(window,source,preset==4?240:150); window.SaveFrame(path); window.Close();
                    }
                    int x=(preset%4)*tileWidth,y=(preset/4)*tileHeight;
                    using(var frame=new Bitmap(path)) g.DrawImage(frame,new Rectangle(x,y,tileWidth,tileHeight));
                    g.DrawString(Visualizer.Names[preset],font,Brushes.Black,x+11,y+tileHeight-27); g.DrawString(Visualizer.Names[preset],font,Brushes.White,x+10,y+tileHeight-28);
                }
                using(var pen=new Pen(Color.Black,2)) { for(int i=1;i<4;i++) g.DrawLine(pen,i*tileWidth,0,i*tileWidth,sheet.Height); g.DrawLine(pen,0,tileHeight,sheet.Width,tileHeight); }
            }
            sheet.Save(Path.Combine(dir,name),ImageFormat.Png);
        }
    }
}
}

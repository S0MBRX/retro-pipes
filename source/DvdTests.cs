using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RetroPipes {
static class DvdTests {
    public static void Run(string dir) {
        foreach(double aspect in new[]{0.4,1.777,5.333,10.666}) {
            var logo=new DvdLogo(new Random(4),aspect);
            for(int i=0;i<24000;i++) {
                Color before=logo.Colour; int hits=logo.Bounces; logo.Step(0.02);
                if(Math.Abs(logo.X)+logo.HalfWidth>aspect+1e-9||Math.Abs(logo.Y)+logo.HalfHeight>1+1e-9) throw new Exception("DVD crossed its screen bounds");
                if(logo.Bounces>hits&&logo.Colour==before) throw new Exception("DVD did not change colour at an edge");
            }
            if(logo.Bounces<10) throw new Exception("DVD stopped bouncing");
            var delayed=new DvdLogo(new Random(4),aspect); var stepped=new DvdLogo(new Random(4),aspect);
            delayed.Step(125); for(int i=0;i<12500;i++) stepped.Step(0.01);
            if(Math.Abs(delayed.X-stepped.X)>1e-8||Math.Abs(delayed.Y-stepped.Y)>1e-8||delayed.VX!=stepped.VX||delayed.VY!=stepped.VY) throw new Exception("DVD loses travel on delayed frames");
        }
        var corner=new DvdLogo(new Random(4),1.6); corner.X=1.6-corner.HalfWidth-0.01; corner.Y=1-corner.HalfHeight-0.01; corner.VX=corner.VY=0.1;
        corner.Step(0.2); if(corner.VX>=0||corner.VY>=0||corner.Bounces!=1) throw new Exception("DVD corner hit failed");
        var standalone=new Settings {Pipes=false,Dvd=true}; standalone.Validate();
        if(standalone.Pipes) throw new Exception("DVD-only settings force pipes back on");
        using(var saved=new MemoryStream()) {
            var xml=new System.Xml.Serialization.XmlSerializer(typeof(Settings)); standalone.RandomDvd=true; standalone.DvdChance=73;
            xml.Serialize(saved,standalone); saved.Position=0; var restored=(Settings)xml.Deserialize(saved);
            if(!restored.Dvd||!restored.RandomDvd||restored.DvdChance!=73) throw new Exception("DVD settings lost after save");
        }
        var chances=new Settings {RandomizeEachRun=true,RandomDvd=true,DvdChance=0};
        if(chances.ForRun(new Random(4)).Dvd) throw new Exception("0% DVD chance failed");
        chances.DvdChance=100; if(!chances.ForRun(new Random(4)).Dvd) throw new Exception("100% DVD chance failed");
        using(var window=new PipesWindow(new Settings {Pipes=false,Dvd=true},"test",new Rectangle(0,0,640,400),IntPtr.Zero,4)) {
            window.Show(); Application.DoEvents(); var original=window.Effects.Dvd;
            for(int i=0;i<6000;i++) window.Advance(0.02);
            if(!object.ReferenceEquals(original,window.Effects.Dvd)||window.Scene.Age<119) throw new Exception("Standalone DVD teleports on an effects reset");
            window.Close();
        }
        var scene=new Scene(new Settings(),42,1.6); double low=100,high=-100;
        for(int i=0;i<6000;i++) {
            double time=i*0.05; var a=scene.CameraAt(time,true); var b=scene.CameraAt(time+0.05,true);
            low=Math.Min(low,a.Pitch); high=Math.Max(high,a.Pitch);
            if(Math.Abs(a.Pitch-b.Pitch)>0.1||Math.Abs(a.Yaw-b.Yaw)>0.12||Math.Abs(a.Roll-b.Roll)>0.01) throw new Exception("Orbit has a camera jump");
        }
        if(low>=0||high<=20) throw new Exception("Orbit remains at the same downward angle");
        var fixedCamera=scene.CameraAt(200,false);
        if(fixedCamera.Pitch!=13||fixedCamera.Yaw!=-15||fixedCamera.Roll!=0||fixedCamera.AimX!=0||fixedCamera.AimY!=0) throw new Exception("Disabled orbit still moves");
        using(var window=new PipesWindow(new Settings {Rotate=true,Speed=5,Count=5},"test",new Rectangle(0,0,960,600),IntPtr.Zero,42)) {
            window.Show(); Application.DoEvents(); for(int i=0;i<400;i++) window.Advance(0.02);
            foreach(int time in new[]{0,60,120}) { window.Scene.Age=time; window.SaveFrame(Path.Combine(dir,"Orbit-"+time+".png")); }
            window.Close();
        }
        if(new Settings().Speed!=25||new Settings().Count!=10) throw new Exception("Default speed/count limits are wrong");
    }
}
}

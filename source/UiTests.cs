using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;
using System.IO;

namespace RetroPipes {
static class UiTests {
    static void Paint(Control control,Graphics graphics,Rectangle clip) {
        control.GetType().GetMethod("OnPaint",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(control,new object[]{new PaintEventArgs(graphics,clip)});
    }
    // GDI text must obey both the Graphics origin and the small dirty rectangle.
    // Otherwise a partial repaint can leave text/black rectangles outside its control.
    static void CheckClipping(Control control) {
        using(control) using(var bitmap=new Bitmap(control.Width+40,control.Height+40)) {
            var clip=new Rectangle(30,8,40,12); var screenClip=new Rectangle(47,27,40,12);
            using(var g=Graphics.FromImage(bitmap)) {
                g.Clear(Color.Magenta); g.TranslateTransform(17,19); g.SetClip(clip); Paint(control,g,clip);
            }
            for(int y=0;y<bitmap.Height;y++) for(int x=0;x<bitmap.Width;x++)
                if(!screenClip.Contains(x,y)&&bitmap.GetPixel(x,y).ToArgb()!=Color.Magenta.ToArgb())
                    throw new Exception(control.GetType().Name+" painted outside its dirty rectangle at "+x+","+y);
        }
    }
    static void SamePixels(Bitmap a,Bitmap b,string message) {
        for(int y=0;y<a.Height;y++) for(int x=0;x<a.Width;x++)
            if(a.GetPixel(x,y)!=b.GetPixel(x,y)) throw new Exception(message+" at "+x+","+y);
    }
    static void Repaint(Control control,Action change) {
        using(control) using(var reused=new Bitmap(control.Width,control.Height)) using(var fresh=new Bitmap(control.Width,control.Height)) {
            using(var g=Graphics.FromImage(reused)) Paint(control,g,control.ClientRectangle);
            change();
            using(var g=Graphics.FromImage(reused)) Paint(control,g,control.ClientRectangle);
            using(var g=Graphics.FromImage(fresh)) Paint(control,g,control.ClientRectangle);
            SamePixels(reused,fresh,control.GetType().Name+" retained old pixels");
        }
    }
    public static void Run(string dir) {
        CheckClipping(new XpCheckBox {Text="Long checkbox text",Size=new Size(200,25)});
        CheckClipping(new XpButton {Text="Long button text",Size=new Size(180,35)});
        CheckClipping(new XpSlider {Size=new Size(200,38)});
        CheckClipping(new XpForm {Size=new Size(760,794)});
        var check=new XpCheckBox {Text="Long checkbox text to erase",Checked=true,Size=new Size(220,25)};
        Repaint(check,delegate { check.Text="Short"; check.Checked=false; });
        var slider=new XpSlider {Size=new Size(200,38),Value=1}; Repaint(slider,delegate { slider.Value=9; });
        var button=new XpButton {Size=new Size(200,35),Text="Long button text to erase"}; Repaint(button,delegate {button.Text="Short";});
        var settings=new Settings {Speed=125,Count=60,TeapotChance=0.5,SpanAllScreens=true};
        using(var launcher=new Launcher(settings,false)) {
            launcher.StartPosition=FormStartPosition.Manual; launcher.Location=new Point(-16000,-16000); launcher.Show(); Application.DoEvents();
            using(var baseline=new Bitmap(launcher.Width,launcher.Height)) using(var after=new Bitmap(launcher.Width,launcher.Height)) {
                launcher.DrawToBitmap(baseline,launcher.ClientRectangle);
                for(int i=0;i<30;i++) { launcher.modeSelector.SelectedIndex=1; launcher.Refresh(); launcher.modeSelector.SelectedIndex=0; launcher.Refresh(); }
                launcher.DrawToBitmap(after,launcher.ClientRectangle);
                SamePixels(baseline,after,"Switching modes left stale controls");
            }
            if(launcher.speedNumber.Value!=125||launcher.countNumber.Value!=60) throw new Exception("Mode switch lost typed overrides");
            using(var advanced=launcher.CreateAdvancedWindow()) {
                advanced.StartPosition=FormStartPosition.Manual; advanced.Location=new Point(-16000,-16000); advanced.Show(); Application.DoEvents();
                using(var bitmap=new Bitmap(advanced.Width,advanced.Height)) { advanced.DrawToBitmap(bitmap,advanced.ClientRectangle); bitmap.Save(Path.Combine(dir,"Advanced-manual-preview.png"),ImageFormat.Png); }
                advanced.Close();
            }
            launcher.FullRandom();
            if(!launcher.FullRandomEnabled||launcher.speedNumber.Value!=125||launcher.countNumber.Value!=60) throw new Exception("Full Random did not enable all options or changed entered limits");
            using(var advanced=launcher.CreateAdvancedWindow()) {
                advanced.StartPosition=FormStartPosition.Manual; advanced.Location=new Point(-16000,-16000); advanced.Show(); Application.DoEvents();
                using(var bitmap=new Bitmap(advanced.Width,advanced.Height)) { advanced.DrawToBitmap(bitmap,advanced.ClientRectangle); bitmap.Save(Path.Combine(dir,"Advanced-preview.png"),ImageFormat.Png); }
                advanced.Close();
            }
            using(var bitmap=new Bitmap(launcher.Width,launcher.Height)) { launcher.DrawToBitmap(bitmap,launcher.ClientRectangle); bitmap.Save(Path.Combine(dir,"Randomized-preview.png"),ImageFormat.Png); }
            launcher.Close();
        }
        if(!settings.RandomizeEachRun||!settings.RandomSpeed||!settings.RandomCount||!settings.RandomPalette||!settings.RandomRotation||!settings.RandomPipes||!settings.RandomFireworks||!settings.RandomBubbles||!settings.RandomDvd||!settings.Dvd||!settings.RandomEffectDensity||!settings.RandomTeapots||!settings.SpanAllScreens)
            throw new Exception("Full Random omitted a category or changed the display layout");
        var toggled=settings.Copy();
        using(var launcher=new Launcher(toggled,false)) {
            launcher.StartPosition=FormStartPosition.Manual; launcher.Location=new Point(-16000,-16000); launcher.Show(); Application.DoEvents();
            if(!launcher.FullRandomEnabled) throw new Exception("Reopened controls lost Full Random state");
            launcher.FullRandom();
            if(launcher.FullRandomEnabled||launcher.modeSelector.SelectedIndex!=0) throw new Exception("Second click did not turn Full Random off");
            launcher.Close();
        }
        var defaults=new Settings {SpanAllScreens=true};
        using(var expected=new StringWriter()) using(var actual=new StringWriter()) {
            var serializer=new System.Xml.Serialization.XmlSerializer(typeof(Settings)); serializer.Serialize(expected,defaults); serializer.Serialize(actual,toggled);
            if(expected.ToString()!=actual.ToString()) throw new Exception("Full Random off did not restore basic defaults and preserve screen layout");
        }
        var rng=new Random(15); bool enabled=false,disabled=false; double first=-1; bool varied=false;
        for(int i=0;i<100;i++) {
            var roll=settings.ForRun(rng); enabled|=roll.Teapots; disabled|=!roll.Teapots;
            if(roll.TeapotChance<0||roll.TeapotChance>settings.TeapotChance) throw new Exception("Random teapot probability exceeds limit");
            if(i==0) first=roll.TeapotChance; else varied|=first!=roll.TeapotChance;
        }
        if(!enabled||!disabled||!varied) throw new Exception("Teapots do not fully randomize");
        settings.RandomizeEachRun=false;
        var manual=settings.ForRun(rng);
        if(manual.Teapots!=settings.Teapots||manual.TeapotChance!=settings.TeapotChance||manual.Speed!=settings.Speed) throw new Exception("Manual mode still shuffles");
        using(var saved=new MemoryStream()) {
            var serializer=new System.Xml.Serialization.XmlSerializer(typeof(Settings)); serializer.Serialize(saved,settings); saved.Position=0;
            if(!((Settings)serializer.Deserialize(saved)).RandomTeapots) throw new Exception("Random teapots setting not persisted");
        }
    }
}
}

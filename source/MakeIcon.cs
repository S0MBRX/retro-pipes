using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
class MakeIcon {
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);
    static void Main(string[] args) {
        using(var b=new Bitmap(64,64)) {
            using(var g=Graphics.FromImage(b)) {
                g.SmoothingMode=SmoothingMode.AntiAlias; g.Clear(Color.FromArgb(17,20,29));
                using(var p=new Pen(Color.FromArgb(53,166,255),9)) { p.StartCap=p.EndCap=LineCap.Round; p.LineJoin=LineJoin.Round; g.DrawLines(p,new[]{new Point(12,49),new Point(12,17),new Point(32,17),new Point(32,38),new Point(51,38)}); }
                using(var p=new Pen(Color.FromArgb(252,185,32),8)) { p.StartCap=p.EndCap=LineCap.Round; p.LineJoin=LineJoin.Round; g.DrawLines(p,new[]{new Point(27,51),new Point(48,51),new Point(48,14)}); }
                using(var p=new Pen(Color.FromArgb(155,222,255),2)) { g.DrawLine(p,10,44,10,18); g.DrawLine(p,13,14,30,14); }
            }
            IntPtr h=b.GetHicon(); try { using(var icon=Icon.FromHandle(h)) using(var f=File.Create(args[0])) icon.Save(f); } finally { DestroyIcon(h); }
        }
    }
}

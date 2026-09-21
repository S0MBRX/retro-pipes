using System;
using System.Drawing;
using System.Windows.Forms;

namespace RetroPipes {
static class DisplayLayout {
    public static Rectangle[] Screens() {
        var screens=Screen.AllScreens; var areas=new Rectangle[screens.Length];
        for(int i=0;i<screens.Length;i++) areas[i]=screens[i].Bounds;
        return areas;
    }
    public static Rectangle Union(Rectangle[] screens) {
        if(screens==null||screens.Length==0) throw new ArgumentException("No display surfaces are available.");
        Rectangle area=screens[0];
        for(int i=1;i<screens.Length;i++) area=Rectangle.Union(area,screens[i]);
        return area;
    }
    public static Rectangle[] RenderBounds(bool span,Rectangle[] screens) {
        return span?new[]{Union(screens)}:(Rectangle[])screens.Clone();
    }
    public static bool Same(Rectangle[] a,Rectangle[] b) {
        if(a==null||b==null||a.Length!=b.Length) return false;
        for(int i=0;i<a.Length;i++) if(a[i]!=b[i]) return false;
        return true;
    }
    public static Rectangle PreviewBounds(bool span) {
        if(!span) return new Rectangle(0,0,960,600);
        var area=Union(Screens()); double aspect=(double)area.Width/area.Height;
        int width=1100,height=(int)Math.Round(width/aspect);
        if(height>650) { height=650; width=(int)Math.Round(height*aspect); }
        return new Rectangle(0,0,Math.Max(200,width),Math.Max(150,height));
    }
}
}

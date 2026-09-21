using System;
using System.Drawing;
using System.Collections.Generic;
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
        // Native surfaces stay monitor-sized; spanning views share one simulation.
        return (Rectangle[])screens.Clone();
    }
    public static bool Same(Rectangle[] a,Rectangle[] b) {
        if(a==null||b==null||a.Length!=b.Length) return false;
        var remaining=new List<Rectangle>(b);
        foreach(var area in a) if(!remaining.Remove(area)) return false;
        return true;
    }
    public static double[] Slice(Rectangle canvas,Rectangle view,double halfHeight) {
        double units=2*halfHeight/canvas.Height;
        double left=((double)view.Left-canvas.Left-canvas.Width/2.0)*units;
        double top=(canvas.Height/2.0-((double)view.Top-canvas.Top))*units;
        return new[]{left,left+view.Width*units,top-view.Height*units,top};
    }
    public static Rectangle PreviewBounds(bool span) {
        if(!span) return new Rectangle(0,0,960,600);
        var area=Union(Screens()); double aspect=(double)area.Width/area.Height;
        int width=1100,height=(int)Math.Round(width/aspect);
        if(height>650) { height=650; width=(int)Math.Round(height*aspect); }
        return new Rectangle(0,0,Math.Max(200,width),Math.Max(150,height));
    }
}
sealed class ScreenSaverSession : ApplicationContext {
    Settings settings; Rectangle[] layout; List<PipesWindow> views=new List<PipesWindow>(); Timer watch=new Timer();
    public ScreenSaverSession(Settings value) {
        settings=value.Copy(); Rebuild();
        watch.Interval=2000; watch.Tick+=delegate { if(!DisplayLayout.Same(layout,DisplayLayout.Screens())) Rebuild(); }; watch.Start();
    }
    void Rebuild() {
        foreach(var view in views) view.CloseForReconfigure(); views.Clear(); layout=DisplayLayout.Screens();
        views=PipesWindow.CreateViews(settings,"saver",layout,IntPtr.Zero);
    }
    protected override void Dispose(bool disposing) {
        if(disposing) { watch.Dispose(); foreach(var view in views) if(!view.IsDisposed) view.CloseForReconfigure(); views.Clear(); }
        base.Dispose(disposing);
    }
}
}

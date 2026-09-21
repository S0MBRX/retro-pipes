using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RetroPipes {
static class XpTheme {
    public static readonly Color Cream=Color.FromArgb(236,233,216),Blue=Color.FromArgb(0,60,170),Ink=Color.FromArgb(25,25,25);
    public const TextFormatFlags PaintText=TextFormatFlags.PreserveGraphicsClipping|TextFormatFlags.PreserveGraphicsTranslateTransform;
    public static GraphicsPath Round(Rectangle r,int radius) {
        var p=new GraphicsPath(); int d=radius*2;
        p.AddArc(r.X,r.Y,d,d,180,90); p.AddArc(r.Right-d,r.Y,d,d,270,90);
        p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); p.AddArc(r.X,r.Bottom-d,d,d,90,90); p.CloseFigure(); return p;
    }
    public static void Flag(Graphics g,Rectangle bounds) {
        var saved=g.Save(); g.TranslateTransform(bounds.Left,bounds.Top); g.ScaleTransform(bounds.Width/100f,bounds.Height/85f);
        Color[] colours={Color.FromArgb(241,73,34),Color.FromArgb(123,187,37),Color.FromArgb(29,151,219),Color.FromArgb(255,200,27)};
        for(int row=0;row<2;row++) for(int col=0;col<2;col++) {
            float x=col==0?6:53, y=row==0?6:45;
            using(var p=new GraphicsPath()) {
                if(col==0) { p.AddBezier(x+6,y+2,x+23,y-6,x+34,y-1,x+44,y+4); p.AddLine(x+44,y+4,x+37,y+36); p.AddBezier(x+37,y+36,x+22,y+30,x+11,y+28,x-1,y+35); p.CloseFigure(); }
                else { p.AddBezier(x+1,y+5,x+17,y+11,x+29,y+9,x+43,y+1); p.AddLine(x+43,y+1,x+35,y+32); p.AddBezier(x+35,y+32,x+22,y+40,x+10,y+42,x-6,y+36); p.CloseFigure(); }
                using(var b=new LinearGradientBrush(new RectangleF(x-5,y-5,50,48),ControlPaint.Light(colours[row*2+col]),colours[row*2+col],45)) g.FillPath(b,p);
            }
        }
        g.Restore(saved);
    }
}
class XpForm : Form {
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr w,IntPtr l);
    public XpForm() {
        FormBorderStyle=FormBorderStyle.None; BackColor=XpTheme.Cream; ForeColor=XpTheme.Ink;
        Font=new Font("Tahoma",9); DoubleBuffered=true;
        var minimize=new XpCaptionButton(false) {Width=25,Height=23,Top=5,Anchor=AnchorStyles.Top|AnchorStyles.Right};
        var close=new XpCaptionButton(true) {Width=25,Height=23,Top=5,Anchor=AnchorStyles.Top|AnchorStyles.Right};
        minimize.Click+=delegate { WindowState=FormWindowState.Minimized; }; close.Click+=delegate { Close(); };
        Controls.Add(minimize); Controls.Add(close);
        SizeChanged+=delegate { close.Left=ClientSize.Width-31; minimize.Left=ClientSize.Width-60; };
    }
    protected override CreateParams CreateParams { get { var p=base.CreateParams; p.Style|=0x00080000|0x00020000; return p; } }
    protected override void OnMouseDown(MouseEventArgs e) { if(e.Button==MouseButtons.Left&&e.Y<32) { ReleaseCapture(); SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero); } base.OnMouseDown(e); }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e); using(var background=new SolidBrush(BackColor)) e.Graphics.FillRectangle(background,ClientRectangle); var g=e.Graphics; g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var b=new LinearGradientBrush(new Rectangle(0,0,Width,34),Color.FromArgb(71,151,255),Color.FromArgb(0,73,211),90)) g.FillRectangle(b,0,0,Width,34);
        using(var p=new Pen(Color.FromArgb(0,64,202),4)) g.DrawRectangle(p,2,2,Width-4,Height-4);
        using(var p=new Pen(Color.FromArgb(108,175,255))) g.DrawLine(p,5,2,Width-6,2);
        if(Icon!=null) g.DrawIcon(Icon,new Rectangle(10,8,17,17));
        using(var font=new Font("Trebuchet MS",11,FontStyle.Bold)) {
            TextRenderer.DrawText(g,Text=="Retro Pipes"?"Retro Pipes - Screen Saver Settings":Text,font,new Point(35,7),Color.FromArgb(0,37,121),XpTheme.PaintText);
            TextRenderer.DrawText(g,Text=="Retro Pipes"?"Retro Pipes - Screen Saver Settings":Text,font,new Point(34,6),Color.White,XpTheme.PaintText);
        }

    }
}
class XpCaptionButton : Button {
    bool close;
    public XpCaptionButton(bool isClose) { close=isClose; AccessibleName=isClose?"Close":"Minimize"; TabStop=false; SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true); }
    protected override void OnPaint(PaintEventArgs e) {
        using(var background=new SolidBrush(BackColor)) e.Graphics.FillRectangle(background,ClientRectangle);
        var g=e.Graphics; g.SmoothingMode=SmoothingMode.AntiAlias; var r=new Rectangle(0,0,Width-1,Height-1);
        using(var path=XpTheme.Round(r,3)) using(var b=new LinearGradientBrush(r,close?Color.FromArgb(247,163,140):Color.FromArgb(119,175,249),close?Color.FromArgb(197,41,14):Color.FromArgb(0,75,211),90)) { g.FillPath(b,path); using(var p=new Pen(Color.White)) g.DrawPath(p,path); }
        using(var p=new Pen(Color.White,2)) { if(close) { g.DrawLine(p,8,6,17,16); g.DrawLine(p,17,6,8,16); } else g.DrawLine(p,7,16,17,16); }
    }
}
class XpButton : Button {
    bool hover,pressed;
    public XpButton() { SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true); }
    protected override void OnMouseEnter(EventArgs e) { hover=true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover=false; pressed=false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { pressed=true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { pressed=false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e) {
        using(var background=new SolidBrush(BackColor)) e.Graphics.FillRectangle(background,ClientRectangle);
        var g=e.Graphics; g.SmoothingMode=SmoothingMode.AntiAlias; var r=new Rectangle(0,0,Width-1,Height-1);
        using(var path=XpTheme.Round(r,3)) {
            using(var b=new LinearGradientBrush(r,pressed?Color.FromArgb(218,216,200):Color.White,Color.FromArgb(223,220,204),90)) g.FillPath(b,path);
            using(var p=new Pen(Color.FromArgb(0,60,116))) g.DrawPath(p,path);
        }
        if(hover||Focused) using(var p=new Pen(hover?Color.FromArgb(243,190,92):Color.FromArgb(132,180,242),2)) g.DrawRectangle(p,3,3,Width-7,Height-7);
        TextRenderer.DrawText(g,Text,Font,new Rectangle(3,pressed?2:0,Width-6,Height),Enabled?XpTheme.Ink:SystemColors.GrayText,XpTheme.PaintText|TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        if(Focused&&ShowFocusCues) ControlPaint.DrawFocusRectangle(g,new Rectangle(5,5,Width-10,Height-10));
    }
}
class XpCheckBox : CheckBox {
    public XpCheckBox() { BackColor=XpTheme.Cream; ForeColor=XpTheme.Ink; SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true); }
    protected override void OnPaint(PaintEventArgs e) {
        using(var background=new SolidBrush(BackColor)) e.Graphics.FillRectangle(background,ClientRectangle);
        var g=e.Graphics; int y=(Height-13)/2; var box=new Rectangle(0,y,13,13);
        using(var b=new LinearGradientBrush(box,Color.FromArgb(222,230,232),Color.White,45)) g.FillRectangle(b,box);
        using(var p=new Pen(Color.FromArgb(29,82,129))) g.DrawRectangle(p,box);
        if(Checked) using(var p=new Pen(Color.FromArgb(33,161,33),2.5f)) g.DrawLines(p,new[]{new Point(3,y+6),new Point(6,y+9),new Point(11,y+3)});
        TextRenderer.DrawText(g,Text,Font,new Rectangle(20,0,Width-20,Height),Enabled?ForeColor:SystemColors.GrayText,XpTheme.PaintText|TextFormatFlags.Left|TextFormatFlags.VerticalCenter);
        if(Focused&&ShowFocusCues) ControlPaint.DrawFocusRectangle(g,new Rectangle(19,2,Width-21,Height-4));
    }
}
class XpSlider : Control {
    int min=1,max=10,value=1; bool dragging; public event EventHandler ValueChanged;
    public int Minimum { get { return min; } set { min=value; } }
    public int Maximum { get { return max; } set { max=value; } }
    public int Value { get { return value; } set { int next=Math.Max(min,Math.Min(max,value)); if(this.value==next) return; this.value=next; Invalidate(); if(ValueChanged!=null) ValueChanged(this,EventArgs.Empty); } }
    public XpSlider() { BackColor=XpTheme.Cream; Height=38; TabStop=true; SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true); }
    void MoveTo(int x) { Value=min+(int)Math.Round(Math.Max(0,Math.Min(1,(x-8.0)/Math.Max(1,Width-16)))*(max-min)); }
    protected override void OnMouseDown(MouseEventArgs e) { Focus(); dragging=true; Capture=true; MoveTo(e.X); base.OnMouseDown(e); }
    protected override void OnMouseMove(MouseEventArgs e) { if(dragging) MoveTo(e.X); base.OnMouseMove(e); }
    protected override void OnMouseUp(MouseEventArgs e) { dragging=false; Capture=false; base.OnMouseUp(e); }
    protected override bool IsInputKey(Keys keyData) { return keyData==Keys.Left||keyData==Keys.Right||base.IsInputKey(keyData); }
    protected override void OnKeyDown(KeyEventArgs e) { if(e.KeyCode==Keys.Left) Value--; if(e.KeyCode==Keys.Right) Value++; if(e.KeyCode==Keys.Home) Value=min; if(e.KeyCode==Keys.End) Value=max; base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e) {
        using(var background=new SolidBrush(BackColor)) e.Graphics.FillRectangle(background,ClientRectangle);
        var g=e.Graphics; int cy=Height/2,x=8+(int)((Width-16)*(value-min)/(double)Math.Max(1,max-min));
        ControlPaint.DrawBorder3D(g,7,cy-2,Width-14,4,Border3DStyle.Sunken);
        using(var path=new GraphicsPath()) { path.AddPolygon(new[]{new Point(x-5,cy-9),new Point(x+5,cy-9),new Point(x+5,cy+6),new Point(x,cy+11),new Point(x-5,cy+6)});
            using(var b=new LinearGradientBrush(new Rectangle(x-5,cy-9,11,21),Color.White,Color.FromArgb(183,219,174),0f)) g.FillPath(b,path);
            using(var p=new Pen(Color.FromArgb(48,111,78))) g.DrawPath(p,path);
        }
        if(Focused&&ShowFocusCues) ControlPaint.DrawFocusRectangle(g,ClientRectangle);
    }
}
}

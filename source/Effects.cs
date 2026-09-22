using System;
using System.Collections.Generic;
using System.Drawing;

namespace RetroPipes {
class Spark {
    public double X,Y,OldX,OldY,VX,VY,Life,MaxLife;
    public Color Colour;
}
class Rocket { public double X,Y,VX,VY,Target; public Color Colour; }
class Bubble { public double X,Y,VX,VY,Radius,Hue; }
class Effects {
    public readonly List<Spark> Sparks=new List<Spark>();
    public readonly List<Rocket> Rockets=new List<Rocket>();
    public readonly List<Bubble> Bubbles=new List<Bubble>();
    internal readonly DvdLogo Dvd;
    Random random; Settings settings; double aspect,nextLaunch,age;
    public Effects(Settings value,int seed,double ratio) {
        settings=value; random=new Random(seed); aspect=Math.Max(0.4,ratio); nextLaunch=0.15;
        if(settings.Dvd) Dvd=new DvdLogo(new Random(seed^0x53AF19),aspect);
        if(settings.Bubbles) for(int i=0;i<settings.BubbleCount;i++) {
            double radius=0.08+random.NextDouble()*0.14;
            Bubbles.Add(new Bubble {X=Between(-aspect+radius,aspect-radius),Y=Between(-1+radius,1-radius),VX=Between(-0.13,0.13),VY=Between(-0.1,0.16),Radius=radius,Hue=random.NextDouble()});
        }
    }
    double Between(double a,double b) { return a+random.NextDouble()*(b-a); }
    public static Color Hue(double hue) {
        double h=(hue-Math.Floor(hue))*6; int sector=(int)h; double f=h-sector;
        int up=(int)(255*f),down=255-up;
        switch(sector) { case 0:return Color.FromArgb(255,up,55); case 1:return Color.FromArgb(down,255,55); case 2:return Color.FromArgb(55,255,up); case 3:return Color.FromArgb(55,down,255); case 4:return Color.FromArgb(up,55,255); default:return Color.FromArgb(255,55,down); }
    }
    void Burst(Rocket rocket) {
        int amount=random.Next(85,155); double phase=Between(0,Math.PI*2); bool ring=random.Next(4)==0;
        for(int i=0;i<amount && Sparks.Count<3500;i++) {
            double angle=phase+i*Math.PI*2/amount; double speed=ring?Between(0.4,0.5):Math.Sqrt(random.NextDouble())*0.6;
            double life=Between(1.3,3.2);
            Sparks.Add(new Spark {X=rocket.X,Y=rocket.Y,OldX=rocket.X,OldY=rocket.Y,VX=Math.Cos(angle)*speed,VY=Math.Sin(angle)*speed,Life=life,MaxLife=life,Colour=rocket.Colour});
        }
    }
    public void Step(double dt) {
        age+=dt; if(Dvd!=null) Dvd.Step(dt);
        if(settings.Fireworks) {
            nextLaunch-=dt;
            if(nextLaunch<=0) {
                Rockets.Add(new Rocket {X=Between(-aspect*0.85,aspect*0.85),Y=-1.1,VX=Between(-0.12,0.12),VY=Between(1.05,1.4),Target=Between(0.0,0.75),Colour=Hue(random.NextDouble())});
                nextLaunch=Between(2.0,5.5)/settings.FireworkRate;
            }
            for(int i=Rockets.Count-1;i>=0;i--) {
                var r=Rockets[i]; r.X+=r.VX*dt; r.Y+=r.VY*dt;
                if(r.Y>=r.Target) { Burst(r); Rockets.RemoveAt(i); }
            }
            for(int i=Sparks.Count-1;i>=0;i--) {
                var p=Sparks[i]; p.Life-=dt;
                if(p.Life<=0) { Sparks.RemoveAt(i); continue; }
                p.OldX=p.X; p.OldY=p.Y; p.VX*=Math.Exp(-dt*0.42); p.VY-=dt*0.22; p.X+=p.VX*dt; p.Y+=p.VY*dt;
            }
        }
        foreach(var b in Bubbles) {
            b.X+=b.VX*dt; b.Y+=b.VY*dt;
            if(b.X-b.Radius< -aspect) { b.X=-aspect+b.Radius; b.VX=Math.Abs(b.VX); }
            if(b.X+b.Radius>aspect) { b.X=aspect-b.Radius; b.VX=-Math.Abs(b.VX); }
            if(b.Y-b.Radius< -1) { b.Y=-1+b.Radius; b.VY=Math.Abs(b.VY); }
            if(b.Y+b.Radius>1) { b.Y=1-b.Radius; b.VY=-Math.Abs(b.VY); }
        }
        for(int i=0;i<Bubbles.Count;i++) for(int j=i+1;j<Bubbles.Count;j++) {
            var a=Bubbles[i]; var b=Bubbles[j]; double dx=b.X-a.X,dy=b.Y-a.Y,d2=dx*dx+dy*dy,minimum=a.Radius+b.Radius;
            if(d2<minimum*minimum&&d2>0.000001) {
                double d=Math.Sqrt(d2),nx=dx/d,ny=dy/d,relative=(b.VX-a.VX)*nx+(b.VY-a.VY)*ny;
                if(relative<0) { a.VX+=relative*nx; a.VY+=relative*ny; b.VX-=relative*nx; b.VY-=relative*ny; }
                double overlap=(minimum-d)*0.22; a.X-=nx*overlap; a.Y-=ny*overlap; b.X+=nx*overlap; b.Y+=ny*overlap;
            }
        }
    }
    static void Colour(Color c,double alpha) { GL.glColor4f(c.R/255f,c.G/255f,c.B/255f,(float)alpha); }
    public void Draw(double ratio,double[] clip=null) {
        aspect=Math.Max(0.4,ratio);
        GL.glDisable(0x0B50); GL.glDisable(0x0B71); GL.glEnable(0x0BE2);
        GL.glMatrixMode(0x1701); GL.glPushMatrix(); GL.glLoadIdentity();
        if(clip==null) GL.glOrtho(-aspect,aspect,-1,1,-1,1); else GL.glOrtho(clip[0],clip[1],clip[2],clip[3],-1,1);
        GL.glMatrixMode(0x1700); GL.glPushMatrix(); GL.glLoadIdentity();
        if(settings.Fireworks) DrawFireworks();
        if(settings.Bubbles) DrawBubbles();
        if(Dvd!=null) Dvd.Draw();
        GL.glPopMatrix(); GL.glMatrixMode(0x1701); GL.glPopMatrix(); GL.glMatrixMode(0x1700);
        GL.glDisable(0x0BE2); GL.glEnable(0x0B71); GL.glEnable(0x0B50);
    }
    void DrawFireworks() {
        GL.glBlendFunc(0x0302,1); GL.glLineWidth(1.5f); GL.glBegin(1);
        foreach(var r in Rockets) {
            Colour(r.Colour,0); GL.glVertex3d(r.X-r.VX*0.22,r.Y-0.23,0);
            GL.glColor4f(1,0.95f,0.8f,0.95f); GL.glVertex3d(r.X,r.Y,0);
        }
        foreach(var p in Sparks) {
            double fade=Math.Pow(p.Life/p.MaxLife,0.65);
            Colour(p.Colour,0); GL.glVertex3d(p.X-p.VX*0.17,p.Y-p.VY*0.17,0);
            Colour(p.Colour,fade); GL.glVertex3d(p.X,p.Y,0);
        }
        GL.glEnd(); GL.glPointSize(2.5f); GL.glBegin(0);
        foreach(var p in Sparks) { Colour(p.Colour,Math.Pow(p.Life/p.MaxLife,0.8)); GL.glVertex3d(p.X,p.Y,0); }
        GL.glEnd(); GL.glLineWidth(1);
    }
    void DrawBubbles() {
        GL.glBlendFunc(0x0302,0x0303);
        foreach(var b in Bubbles) {
            // A transparent face, iridescent rim and two curved reflections.
            GL.glBegin(6); Colour(Hue(b.Hue),0.012); GL.glVertex3d(b.X,b.Y,0);
            for(int i=0;i<=64;i++) { double a=i*Math.PI/32; Colour(Hue(b.Hue+a/10+age*0.014),0.085); GL.glVertex3d(b.X+Math.Cos(a)*b.Radius,b.Y+Math.Sin(a)*b.Radius,0); }
            GL.glEnd();
            GL.glBegin(5);
            for(int i=0;i<=96;i++) {
                double a=i*Math.PI/48,highlight=Math.Pow(Math.Max(0,Math.Cos(a-2.2)),10),alpha=0.3+highlight*0.6;
                Color c=Hue(b.Hue+a/6.28+age*0.014);
                Colour(c,0.01); GL.glVertex3d(b.X+Math.Cos(a)*b.Radius*0.91,b.Y+Math.Sin(a)*b.Radius*0.91,0);
                Colour(c,alpha); GL.glVertex3d(b.X+Math.Cos(a)*b.Radius,b.Y+Math.Sin(a)*b.Radius,0);
            }
            GL.glEnd();
            GL.glLineWidth(2); GL.glBegin(3);
            for(int i=0;i<=28;i++) { double a=1.6+i*1.0/28; GL.glColor4f(0.9f,0.97f,1,(float)(Math.Sin(i*Math.PI/28)*0.82)); GL.glVertex3d(b.X+Math.Cos(a)*b.Radius*0.86,b.Y+Math.Sin(a)*b.Radius*0.86,0); }
            GL.glEnd(); GL.glLineWidth(1); GL.glBegin(3);
            for(int i=0;i<=20;i++) { double a=4.55+i*0.7/20; GL.glColor4f(0.75f,0.85f,1,(float)(Math.Sin(i*Math.PI/20)*0.5)); GL.glVertex3d(b.X+Math.Cos(a)*b.Radius*0.9,b.Y+Math.Sin(a)*b.Radius*0.9,0); }
            GL.glEnd();
        }
    }
}
}

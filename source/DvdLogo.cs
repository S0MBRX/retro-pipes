using System;
using System.Drawing;

namespace RetroPipes {
// Original vector recreation: slanted DVD lettering and the elliptical disc.
// Geometry stays inside the 220 x 100 drawing box used for edge collisions.
class DvdLogo {
    public double X,Y,VX,VY;
    public readonly double HalfWidth,HalfHeight;
    public Color Colour;
    public int Bounces;
    readonly double aspect; readonly Random random;
    public DvdLogo(Random rng,double ratio) {
        random=rng; aspect=ratio;
        HalfWidth=Math.Min(0.25,aspect*0.38); HalfHeight=HalfWidth*100/220;
        X=(rng.NextDouble()*2-1)*(aspect-HalfWidth); Y=(rng.NextDouble()*2-1)*(1-HalfHeight);
        VX=(rng.Next(2)==0?-1:1)*0.18; VY=(rng.Next(2)==0?-1:1)*0.13;
        Colour=Effects.Hue(rng.NextDouble());
    }
    static bool Reflect(ref double position,ref double velocity,double limit,double dt) {
        double next=position+velocity*dt;
        if(next==limit) { position=limit; velocity=-Math.Abs(velocity); return true; }
        if(next==-limit) { position=-limit; velocity=Math.Abs(velocity); return true; }
        if(next>=-limit&&next<=limit) { position=next; return false; }
        // Fold the travelled distance into the box, including long delayed frames.
        double phase=(next+limit)%(4*limit); if(phase<0) phase+=4*limit;
        if(phase<=2*limit) position=-limit+phase;
        else { position=3*limit-phase; velocity=-velocity; }
        return true;
    }
    public void Step(double dt) {
        bool hitX=Reflect(ref X,ref VX,aspect-HalfWidth,dt);
        bool hitY=Reflect(ref Y,ref VY,1-HalfHeight,dt);
        if(hitX||hitY) {
            Bounces++; Color next;
            do { next=Effects.Hue(random.NextDouble()); } while(Math.Abs(next.R-Colour.R)+Math.Abs(next.G-Colour.G)+Math.Abs(next.B-Colour.B)<120);
            Colour=next;
        }
    }
    static void Vertex(double x,double y) { GL.glVertex3d(x,y,0); }
    static void LetterVertex(double x,double y) { Vertex(x+(y-38)*0.18,y); }
    static void D(double x) {
        GL.glBegin(7); LetterVertex(x,38); LetterVertex(x+16,38); LetterVertex(x+16,98); LetterVertex(x,98); GL.glEnd();
        GL.glBegin(5);
        for(int i=0;i<=40;i++) {
            double a=Math.PI/2-i*Math.PI/40;
            LetterVertex(x+16+43*Math.Cos(a),68+30*Math.Sin(a));
            LetterVertex(x+16+25*Math.Cos(a),68+13*Math.Sin(a));
        }
        GL.glEnd();
    }
    public void Draw() {
        GL.glBlendFunc(0x0302,0x0303); GL.glColor4f(Colour.R/255f,Colour.G/255f,Colour.B/255f,1);
        GL.glPushMatrix(); GL.glTranslated(X-HalfWidth,Y-HalfHeight,0); GL.glScaled(HalfWidth*2/220,HalfHeight*2/100,1);
        D(2); D(146);
        GL.glBegin(7);
        LetterVertex(75,98); LetterVertex(94,98); LetterVertex(110,38); LetterVertex(91,38);
        LetterVertex(91,38); LetterVertex(110,38); LetterVertex(143,98); LetterVertex(123,98);
        GL.glEnd();
        GL.glBegin(5);
        for(int i=0;i<=96;i++) {
            double a=i*Math.PI/48;
            Vertex(110+106*Math.Cos(a),17+15*Math.Sin(a));
            Vertex(110+28*Math.Cos(a),17+4*Math.Sin(a));
        }
        GL.glEnd(); GL.glPopMatrix();
    }
}
struct CameraPose { public double Pitch,Yaw,Roll,AimX,AimY; }
}

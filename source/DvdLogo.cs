using System;
using System.Drawing;

namespace RetroPipes {
// Bouncing DVD-Video PNG; its original aspect ratio also defines the hit box.
class DvdLogo {
    public double X,Y,VX,VY;
    public readonly double HalfWidth,HalfHeight;
    public Color Colour;
    public int Bounces;
    readonly double aspect; readonly Random random;
    public DvdLogo(Random rng,double ratio) {
        random=rng; aspect=ratio;
        HalfWidth=Math.Min(0.25,aspect*0.38); HalfHeight=HalfWidth*DvdTexture.AspectRatio;
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
    public void Draw(uint texture) {
        GL.glBlendFunc(0x0302,0x0303); GL.glColor4f(Colour.R/255f,Colour.G/255f,Colour.B/255f,1);
        GL.glEnable(0x0DE1); GL.glBindTexture(0x0DE1,texture);
        double left=X-HalfWidth,right=X+HalfWidth,top=Y+HalfHeight,bottom=Y-HalfHeight;
        GL.glBegin(7);
        GL.glTexCoord2d(0,0); GL.glVertex3d(left,top,0);
        GL.glTexCoord2d(DvdTexture.U,0); GL.glVertex3d(right,top,0);
        GL.glTexCoord2d(DvdTexture.U,DvdTexture.V); GL.glVertex3d(right,bottom,0);
        GL.glTexCoord2d(0,DvdTexture.V); GL.glVertex3d(left,bottom,0);
        GL.glEnd(); GL.glBindTexture(0x0DE1,0); GL.glDisable(0x0DE1);
    }

}
struct CameraPose { public double Pitch,Yaw,Roll,AimX,AimY; }
}

using System;

namespace RetroPipes {
// The same camera transform as the renderer, used only as a soft preference.
// No coordinate or screen edge makes a growth direction illegal.
struct GrowthView {
    double a,b,c,d,e,f,g,h,i,aimX,aimY,aspect,halfHeight,distance,depthScale;
    bool panoramic;
    public GrowthView(CameraPose camera,double ratio,bool span,double heightScale,int width,int height) {
        double yaw=camera.Yaw*Math.PI/180,pitch=camera.Pitch*Math.PI/180,roll=camera.Roll*Math.PI/180;
        double sy=Math.Sin(yaw),cy=Math.Cos(yaw),sp=Math.Sin(pitch),cp=Math.Cos(pitch),sr=Math.Sin(roll),cr=Math.Cos(roll);
        a=cr*cy-sr*sp*sy; b=-sr*cp; c=cr*sy+sr*sp*cy;
        d=sr*cy+cr*sp*sy; e=cr*cp; f=sr*sy-cr*sp*cy;
        g=-cp*sy; h=sp; i=cp*cy;
        aimX=camera.AimX; aimY=camera.AimY; aspect=ratio; panoramic=span;
        halfHeight=27*0.41421356*heightScale; distance=span?30+width+height:27;
        depthScale=halfHeight*(span?Math.Sqrt(Math.Max(1,ratio)):1);
    }
    public double Cost(Cell point) {
        double x=a*point.X+b*point.Y+c*point.Z-aimX;
        double y=d*point.X+e*point.Y+f*point.Z-aimY;
        double z=g*point.X+h*point.Y+i*point.Z;
        double visibleHeight=panoramic?halfHeight:Math.Max(1,distance-z)*0.41421356;
        double nx=x/(visibleHeight*aspect),ny=y/visibleHeight;
        // A broad, nearly flat preference in the image, with a gradual pull back
        // from offscreen space and from extreme depth. Never a reflecting wall.
        double outsideX=Math.Max(0,Math.Abs(nx)-0.72),outsideY=Math.Max(0,Math.Abs(ny)-0.72);
        double depth=z/depthScale,nearCamera=Math.Max(0,z-distance+5)/halfHeight;
        return 3*(outsideX*outsideX+outsideY*outsideY+nearCamera*nearCamera)+0.16*depth*depth;
    }
    public Cell Spawn(Random random) {
        double angle=random.NextDouble()*Math.PI*2,radius=Math.Sqrt(random.NextDouble())*0.85;
        double z=(random.NextDouble()+random.NextDouble()-1)*depthScale*0.75;
        double visibleHeight=panoramic?halfHeight:(distance-z)*0.41421356;
        double x=Math.Cos(angle)*radius*visibleHeight*aspect+aimX,y=Math.Sin(angle)*radius*visibleHeight+aimY;
        // Inverse of an orthonormal camera rotation is its transpose.
        return new Cell((int)Math.Round(a*x+d*y+g*z),(int)Math.Round(b*x+e*y+h*z),(int)Math.Round(c*x+f*y+i*z));
    }
}
}

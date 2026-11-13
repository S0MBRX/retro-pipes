using System;
using System.Runtime.InteropServices;

namespace RetroPipes {
// Audio-reactive background in the spirit of early-2000s media player visualizations:
// feedback tunnels and kaleidoscopes, glowing ribbons, soft smoke, bars and a scope.
// One Visualizer is shared by every view of a scene; each view keeps its own
// low-resolution feedback texture (VisualizerSurface).
sealed class Visualizer : IDisposable {
    public static readonly string[] Names={"Tunnel","Kaleidoscope","Vortex","Ribbons","Smoke","Bars","Scope","Starburst"};
    static readonly double[] decay={0.955,0.94,0.94,0.955,0.975,0.80,0.88,0.93};
    internal sealed class Look { public int Preset,Fold,Sides; public double Hue,Seed; }
    readonly Random random; readonly Func<AudioFrame> source; readonly bool cycle; bool listening;
    internal Look Current,Previous; internal double Fade=1; double nextChange;
    public double Time,Motion,Turn; public int Frame,BeatCount;
    public double Level,Bass,Mid,Treble,Pulse,Energy,Hue,Spin=1; double spinTarget=1,ringClock; int seenBeats=-1;
    public bool Ring; public int Rings;
    public readonly float[] Spectrum=new float[AudioFrame.Bands],Peaks=new float[AudioFrame.Bands],Wave=new float[AudioFrame.Points];
    // preset < 0 picks random presets and cycles them; otherwise that preset stays and only its look varies.
    public Visualizer(int preset,int seed,Func<AudioFrame> audio=null) {
        random=new Random(seed); cycle=preset<0||preset>=Names.Length;
        if(audio==null) { AudioCapture.Acquire(); listening=true; source=AudioCapture.Latest; } else source=audio;
        Current=NewLook(cycle?random.Next(Names.Length):preset);
        Hue=random.NextDouble(); Spin=spinTarget=random.Next(2)==0?-1:1;
        nextChange=35+random.NextDouble()*25;
    }
    public string Name { get { return Names[Current.Preset]; } }
    public bool Cycling { get { return cycle; } }
    public void Dispose() { if(listening) { listening=false; AudioCapture.Release(); } }
    Look NewLook(int preset) {
        var look=new Look {Preset=preset,Hue=random.NextDouble(),Seed=random.NextDouble()*Math.PI*2,Sides=3+random.Next(6)};
        look.Fold=preset==1?new[]{6,8,10,12}[random.Next(4)]:preset==7?new[]{6,8}[random.Next(2)]:0;
        return look;
    }
    // A new scene (automatic cycle, R or the tray roll) brings a new preset, or a new look of a fixed one.
    public void NewScene() {
        int next=Current.Preset;
        if(cycle) { next=random.Next(Names.Length-1); if(next>=Current.Preset) next++; }
        Previous=Current; Current=NewLook(next); Fade=0; nextChange=Time+35+random.NextDouble()*25;
    }
    static double Mix(double idle,double heard,double energy) { return idle+(heard-idle)*energy; }
    static void Follow(ref double value,double target,double dt) { value+=(target-value)*(1-Math.Exp(-dt*(target>value?14:4))); }
    public void Step(double dt) {
        Time+=dt; Frame++;
        var heard=source()??AudioFrame.Silent;
        // Fade into a slow, calm idle animation when nothing is playing.
        Energy+=((heard.Live?1:0)-Energy)*(1-Math.Exp(-dt*(heard.Live?2.5:0.7)));
        double t=Time,e=Energy;
        Follow(ref Level,Mix(0.32+0.08*Math.Sin(t*0.31),heard.Level,e),dt);
        Follow(ref Bass,Mix(0.30+0.14*Math.Sin(t*0.83),heard.Bass,e),dt);
        Follow(ref Mid,Mix(0.28+0.10*Math.Sin(t*0.47+1.3),heard.Mid,e),dt);
        Follow(ref Treble,Mix(0.20+0.08*Math.Sin(t*1.13+2.1),heard.Treble,e),dt);
        for(int i=0;i<Spectrum.Length;i++) {
            double idle=0.24+0.16*Math.Sin(t*0.7+i*0.33)*(0.6+0.4*Math.Sin(t*0.23+i*0.11)),value=Spectrum[i];
            Follow(ref value,Mix(idle,heard.Spectrum[i],e),dt*1.6); Spectrum[i]=(float)value;
            Peaks[i]=(float)Math.Max(value,Peaks[i]-dt*0.45);
        }
        for(int i=0;i<Wave.Length;i++) {
            double x=i*Math.PI*2/Wave.Length,idle=0.45*Math.Sin(2*x+t*1.2)*Math.Cos(t*0.37)+0.12*Math.Sin(5*x-t*0.8);
            Wave[i]=(float)Mix(idle,heard.Wave[i],e);
        }
        if(seenBeats<0) seenBeats=heard.Beats;
        if(heard.Beats!=seenBeats) {
            seenBeats=heard.Beats; Pulse=1; BeatCount++;
            if(random.NextDouble()<0.08) spinTarget=-spinTarget;
            if(random.NextDouble()<0.2) Current.Sides=3+random.Next(6);
        }
        // Tunnel rings leave the centre at a bass-driven rate, and on every beat.
        ringClock+=dt*(2.2+2*Bass+5*Bass*e); Ring=Pulse==1||ringClock>=1; if(Ring) { ringClock-=Math.Floor(ringClock); Rings++; }
        Pulse*=Math.Exp(-dt*5);
        Spin+=(spinTarget-Spin)*(1-Math.Exp(-dt*0.8));
        Hue+=dt*(0.012+0.035*Level*e);
        double motion=dt*(0.45+0.55*e+0.5*e*Level); Motion+=motion; Turn+=motion*Spin;
        if(Previous!=null) { Fade=Math.Min(1,Fade+dt/2.5); if(Fade>=1) Previous=null; }
        if(Time>=nextChange) NewScene();
    }
    internal static double Smooth(double x) { return x*x*(3-2*x); }
    // Trails last longer in idle, where the zoom is slow, so the picture still fills the screen.
    double Persistence(Look look) { return decay[look.Preset]+(look.Preset==0?0.017:look.Preset==1?0.012:0)*(1-Energy); }
    internal double Decay(double dt) {
        double current=Math.Pow(Persistence(Current),dt*30);
        if(Previous==null) return current;
        double old=Math.Pow(Persistence(Previous),dt*30); return old+(current-old)*Smooth(Fade);
    }
    internal double FoldAngle(Look look) { return look.Seed+Turn*0.06; }
    // Where each pixel of the next frame is sampled from in the previous one.
    internal void Source(double x,double y,double dt,out double sx,out double sy) {
        Warp(Current,x,y,dt,out sx,out sy);
        if(Previous!=null) { double ox,oy,f=Smooth(Fade); Warp(Previous,x,y,dt,out ox,out oy); sx=ox+(sx-ox)*f; sy=oy+(sy-oy)*f; }
    }
    void Warp(Look look,double x,double y,double dt,out double sx,out double sy) {
        double r=Math.Sqrt(x*x+y*y),rotate=0,scale=1,ox=0,oy=0,m=Motion;
        switch(look.Preset) {
            case 0: scale=Math.Exp(-(0.9+0.6*Bass+1.2*Bass*Energy+1.0*Pulse)*dt); rotate=Spin*(0.18+0.6*Mid)*dt; break;
            case 1: scale=Math.Exp(-(0.45+0.5*Bass+0.6*Pulse)*dt); rotate=Spin*((0.3+0.6*Mid)+0.25/(0.4+r))*dt; break;
            case 2: scale=Math.Exp(-(0.3+0.6*Bass)*dt); rotate=Spin*(0.5+1.4*Mid+0.6*Pulse)*dt/(0.3+r); break;
            case 3: scale=Math.Exp(-0.06*dt); rotate=Spin*0.06*dt; ox=0.05*dt*Math.Sin(y*3.1+m*0.7); oy=0.05*dt*Math.Cos(x*2.7-m*0.6); break;
            case 4: rotate=Spin*0.03*dt; ox=dt*(0.13*Math.Sin(y*2.3+m*0.35)+0.06*Math.Sin(y*5.1-m*0.5+x*1.3)); oy=-dt*(0.06+0.10*Math.Cos(x*1.9-m*0.3)+0.05*Math.Sin(x*4.3+y*1.1+m*0.4)+0.05*Level); break;
            case 5: oy=-dt*(0.35+0.4*Level); break;
            case 6: scale=Math.Exp((0.3+0.4*Bass)*dt); rotate=Spin*0.08*dt; break;
            default: scale=Math.Exp(-(0.35+1.1*Bass+0.8*Pulse)*dt); rotate=Spin*(0.12+0.35*Treble)*dt; break;
        }
        double c=Math.Cos(rotate),s=Math.Sin(rotate);
        sx=(x*c+y*s)*scale+ox; sy=(-x*s+y*c)*scale+oy;
    }
    internal void DrawShapes(double a,double pixel) {
        if(Previous!=null) Shapes(Previous,1-Smooth(Fade),a,pixel);
        Shapes(Current,Previous!=null?Smooth(Fade):1,a,pixel);
    }
    static double Hash(int a,int b) { unchecked { uint h=(uint)(a*374761393+b*668265263); h=(h^(h>>13))*1274126177u; h^=h>>16; return (h&0xFFFFFF)/16777216.0; } }
    void Shapes(Look look,double weight,double a,double pixel) {
        if(weight<=0.001) return;
        double hue=Hue+look.Hue,m=Motion,turn=Turn+look.Seed,glow=0.55+0.45*Level;
        GL.glBlendFunc(0x0302,1);
        switch(look.Preset) {
            case 0: {
                // Tunnel: morphing polygon rings that the zoom carries outward.
                int sides=look.Sides,per=6,n=sides*per; double r0=0.11+0.16*Bass+0.08*Pulse,spin=turn*0.9;
                hue+=Rings*0.035;
                if(!Ring) { Paint.Disc(0,0,r0*0.7,hue+0.5,0.7,0.6,0.03*weight); break; }
                for(int i=0;i<n;i++) {
                    double c0=spin+(i/per)*Math.PI*2/sides,c1=c0+Math.PI*2/sides,f=(double)(i%per)/per;
                    double x=Math.Cos(c0)*(1-f)+Math.Cos(c1)*f,y=Math.Sin(c0)*(1-f)+Math.Sin(c1)*f;
                    Paint.Set(i,x*r0,y*r0,hue+0.08*Math.Sin(i*Math.PI*2/n),0.85,0.75+0.25*Level,1);
                }
                Paint.Line(n,true,pixel*7,0.35*weight); Paint.Line(n,true,pixel*(3+2.5*Level),(0.85+0.15*Pulse)*weight);
                double inner=r0*0.55;
                for(int i=0;i<n;i++) Paint.Set(i,Paint.X[i]*0.55,Paint.Y[i]*0.55,hue+0.5,0.8,glow,1);
                Paint.Line(n,true,pixel*2,(0.3+0.6*Treble)*weight);
                int sparks=2+(int)(Treble*10);
                for(int i=0;i<sparks;i++) {
                    double angle=Hash(Frame,i)*Math.PI*2,radius=r0*(1.1+0.7*Hash(Frame,i+50));
                    Paint.Disc(Math.Cos(angle)*radius,Math.Sin(angle)*radius,pixel*(2.5+3*Treble),hue+0.3,0.3,1,0.8*weight);
                }
                break;
            }
            case 1: {
                // Kaleidoscope: three comets and the threads between them, folded on display.
                double[] heard={Bass,Mid,Treble};
                for(int k=0;k<3;k++) {
                    double angle=turn*(0.5+0.17*k)+k*2.094,radius=(0.18+0.42*(0.5+0.5*Math.Sin(m*0.31+k*1.7)))*(0.65+0.35*Mid);
                    double x=Math.Cos(angle)*radius,y=Math.Sin(angle)*radius;
                    Paint.Set(k,x,y,hue+k*0.18,0.8,1,1);
                    Paint.Disc(x,y,0.03+0.07*heard[k]+0.03*Pulse,hue+k*0.18,0.8,1,0.2*weight);
                    Paint.Disc(x,y,pixel*3,hue+k*0.18,0.3,1,0.6*weight);
                }
                Paint.Line(3,true,pixel*5,0.12*weight*glow); Paint.Line(3,true,pixel*1.4,0.5*weight*glow);
                // A small rose curve that the outward spiral turns into a mandala.
                int petals=look.Sides,points=96; double rose=0.10+0.22*Mid+0.06*Pulse;
                for(int i=0;i<points;i++) {
                    double angle=i*Math.PI*2/points,radius=rose*(0.35+0.65*Math.Abs(Math.Cos(petals*angle*0.5+m*0.4)));
                    Paint.Set(i,Math.Cos(angle+turn*0.3)*radius,Math.Sin(angle+turn*0.3)*radius,hue+0.5+0.1*Math.Sin(angle*petals),0.7,1,1);
                }
                Paint.Line(points,true,pixel*4,0.08*weight*glow); Paint.Line(points,true,pixel*1.2,(0.25+0.3*Treble)*weight*glow);
                break;
            }
            case 2: {
                // Vortex: the waveform wrapped into a ring and swirled.
                int n=Wave.Length;
                for(int i=0;i<n;i++) {
                    double angle=i*Math.PI*2/n+turn*0.4,radius=0.34+0.10*Bass+0.08*Pulse+0.16*Wave[i]*(0.5+Level);
                    Paint.Set(i,Math.Cos(angle)*radius,Math.Sin(angle)*radius,hue+0.15*Math.Sin(i*Math.PI*2/n),0.8,glow,1);
                }
                Paint.Line(n,true,pixel*6,0.2*weight); Paint.Line(n,true,pixel*(1.5+1.5*Level),0.8*weight);
                break;
            }
            case 3: {
                // Ribbons: glowing Lissajous trails that bend and wobble with the music.
                const int n=90;
                for(int k=0;k<3;k++) {
                    double p1=2+k+0.5*Math.Sin(m*0.05+k),p2=3+0.7*Math.Sin(m*0.043+2*k);
                    for(int j=0;j<n;j++) {
                        double s=m*0.55+k*2.1-j*0.018,jitter=Wave[(j*3+k*17)%Wave.Length]*0.035*Treble;
                        Paint.Set(j,a*0.82*Math.Sin(p1*s+k),0.8*Math.Sin(p2*s+2*k)+jitter,hue+k*0.31+j*0.002,0.75,1,1-(double)j/n);
                    }
                    Paint.Line(n,false,pixel*(6+8*Level),0.12*weight); Paint.Line(n,false,pixel*(1.4+2*Mid),0.75*weight);
                }
                break;
            }
            case 4: {
                // Smoke: soft colour clouds that drift, rise and swell with the volume.
                double[] heard={Bass,Mid,Treble};
                for(int k=0;k<7;k++) {
                    double x=a*0.8*Math.Sin(m*0.11*(1+0.3*k)+k*1.7+look.Seed),y=0.7*Math.Sin(m*0.09*(1+0.25*k)+k*2.3)-0.15;
                    Paint.Disc(x,y,0.3+0.15*Level+0.10*heard[k%3],hue+k*0.08,0.6,1,(0.016+0.016*Level+0.01*Pulse)*weight);
                }
                break;
            }
            case 5: {
                // Bars: a spectrum analyzer whose ghosts rise and fade.
                GL.glBlendFunc(0x0302,0x0303);
                int n=Spectrum.Length; double width=2*a/n,gap=width*0.18,bottom=-0.98;
                GL.glBegin(7);
                for(int i=0;i<n;i++) {
                    double left=-a+i*width+gap/2,right=left+width-gap,top=bottom+0.04+1.75*Spectrum[i];
                    Paint.Colour(hue+i*0.4/n,0.9,0.55,0.95*weight); GL.glVertex2d(left,bottom); GL.glVertex2d(right,bottom);
                    Paint.Colour(hue+i*0.4/n+0.12,0.7,1,0.95*weight); GL.glVertex2d(right,top); GL.glVertex2d(left,top);
                    double cap=bottom+0.04+1.75*Peaks[i];
                    Paint.Colour(hue+0.5,0.25,1,weight); GL.glVertex2d(left,cap); GL.glVertex2d(right,cap); GL.glVertex2d(right,cap+pixel*3); GL.glVertex2d(left,cap+pixel*3);
                }
                GL.glEnd();
                break;
            }
            case 6: {
                // Scope: the waveform across the screen, with echoes shrinking inward.
                int n=Wave.Length; double height=0.35+0.45*Level;
                for(int pass=0;pass<2;pass++) {
                    for(int i=0;i<n;i++) Paint.Set(i,-a*0.96+i*1.92*a/(n-1),(pass==0?1:-1)*Wave[i]*height,hue+(pass==0?0:0.5)+0.3*i/(n-1),0.8,glow,1);
                    double strength=pass==0?1:0.35;
                    Paint.Line(n,false,pixel*6,0.2*strength*weight); Paint.Line(n,false,pixel*(1.4+1.5*Level),(0.7+0.3*Pulse)*strength*weight);
                }
                break;
            }
            default: {
                // Starburst: mirrored spectrum spokes that the zoom turns into a flower.
                for(int i=0;i<32;i++) {
                    int band=i<16?i:31-i; double angle=i*Math.PI*2/32+turn*0.25,level=Spectrum[band*2];
                    double outer=0.06+0.55*level*(0.6+0.4*Level)+0.1*Pulse,c=Math.Cos(angle),s=Math.Sin(angle);
                    Paint.Set(0,c*0.07,s*0.07,hue+band*0.025,0.9,1,0.15); Paint.Set(1,c*(outer+0.03),s*(outer+0.03),hue+band*0.025+0.1,0.85,1,1);
                    Paint.Line(2,false,pixel*5,0.1*weight); Paint.Line(2,false,pixel*1.6,0.55*weight);
                }
                Paint.Disc(0,0,0.05+0.08*Bass,hue+0.5,0.7,1,0.06*weight);
                break;
            }
        }
    }
}

// Small immediate-mode helpers for soft, glowing 2D shapes.
static class Paint {
    public static readonly float[] X=new float[256],Y=new float[256],R=new float[256],G=new float[256],B=new float[256],A=new float[256];
    public static void Rgb(double h,double s,double v,out double r,out double g,out double b) {
        h-=Math.Floor(h); double f=h*6; int i=(int)f; f-=i; double p=v*(1-s),q=v*(1-s*f),t=v*(1-s*(1-f));
        switch(i%6) { case 0:r=v;g=t;b=p;break; case 1:r=q;g=v;b=p;break; case 2:r=p;g=v;b=t;break; case 3:r=p;g=q;b=v;break; case 4:r=t;g=p;b=v;break; default:r=v;g=p;b=q;break; }
    }
    public static void Colour(double h,double s,double v,double alpha) { double r,g,b; Rgb(h,s,v,out r,out g,out b); GL.glColor4f((float)r,(float)g,(float)b,(float)Math.Max(0,Math.Min(1,alpha))); }
    public static void Set(int i,double x,double y,double h,double s,double v,double alpha) {
        double r,g,b; Rgb(h,s,v,out r,out g,out b); X[i]=(float)x; Y[i]=(float)y; R[i]=(float)r; G[i]=(float)g; B[i]=(float)b; A[i]=(float)alpha;
    }
    // A line with a bright centre fading to transparent edges, drawn as two strips.
    public static void Line(int n,bool closed,double half,double alpha) {
        if(n<2) return;
        for(int side=-1;side<=1;side+=2) {
            GL.glBegin(5);
            for(int k=0;k<=(closed?n:n-1);k++) {
                int i=k%n,previous=closed?(i+n-1)%n:Math.Max(0,i-1),next=closed?(i+1)%n:Math.Min(n-1,i+1);
                double tx=X[next]-X[previous],ty=Y[next]-Y[previous],length=Math.Sqrt(tx*tx+ty*ty);
                if(length<1e-9) { tx=1; ty=0; length=1; }
                double nx=-ty/length*half*side,ny=tx/length*half*side; float a=(float)Math.Max(0,Math.Min(1,A[i]*alpha));
                GL.glColor4f(R[i],G[i],B[i],a); GL.glVertex2d(X[i],Y[i]);
                GL.glColor4f(R[i],G[i],B[i],0); GL.glVertex2d(X[i]+nx,Y[i]+ny);
            }
            GL.glEnd();
        }
    }
    public static void Disc(double x,double y,double radius,double h,double s,double v,double alpha) {
        double r,g,b; Rgb(h,s,v,out r,out g,out b);
        GL.glBegin(6); GL.glColor4f((float)r,(float)g,(float)b,(float)Math.Max(0,Math.Min(1,alpha))); GL.glVertex2d(x,y);
        GL.glColor4f((float)r,(float)g,(float)b,0);
        for(int i=0;i<=24;i++) { double angle=i*Math.PI/12; GL.glVertex2d(x+Math.Cos(angle)*radius,y+Math.Sin(angle)*radius); }
        GL.glEnd();
    }
    public static void Rect(double left,double bottom,double right,double top) {
        GL.glBegin(7); GL.glVertex2d(left,bottom); GL.glVertex2d(right,bottom); GL.glVertex2d(right,top); GL.glVertex2d(left,top); GL.glEnd();
    }
}

// Per-window OpenGL feedback: last frame -> warp and fade -> new shapes -> copy back.
// Very wide spanning canvases repeat the picture as mirrored tiles, so it stays seamless.
sealed class VisualizerSurface {
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void BlendEquationProc(uint mode);
    uint texture; int textureWidth,textureHeight,width,height,renderedFrame=-1; double aspect=1,renderedTime;
    BlendEquationProc blendEquation; bool lookedUp;
    public static int Tiles(double canvasAspect) { return Math.Max(1,(int)Math.Round(canvasAspect/1.9)); }
    static int PowerOfTwo(int value) { int result=1; while(result<value) result*=2; return result; }
    public int FeedbackWidth { get { return width; } }
    public int FeedbackHeight { get { return height; } }
    void Allocate(int w,int h) {
        Delete(); width=w; height=h; textureWidth=PowerOfTwo(w); textureHeight=PowerOfTwo(h); renderedFrame=-1;
        GL.glGenTextures(1,out texture); if(texture==0) throw new Exception("Could not allocate the visualizer texture.");
        GL.glBindTexture(0x0DE1,texture);
        GL.glTexParameteri(0x0DE1,0x2801,0x2601); GL.glTexParameteri(0x0DE1,0x2800,0x2601);
        GL.glTexParameteri(0x0DE1,0x2802,0x812F); GL.glTexParameteri(0x0DE1,0x2803,0x812F);
        GL.glPixelStorei(0x0CF5,1); GL.glTexImage2D(0x0DE1,0,0x1907,textureWidth,textureHeight,0,0x1907,0x1401,new byte[textureWidth*textureHeight*3]); GL.glPixelStorei(0x0CF5,4);
        GL.glBindTexture(0x0DE1,0);
    }
    public void Delete() { if(texture!=0) { GL.glDeleteTextures(1,ref texture); texture=0; } }
    public void Render(Visualizer v,int w,int h,double canvasAspect) {
        double a=canvasAspect/Tiles(canvasAspect);
        int fh=(int)Math.Round(Math.Min(600.0,Math.Max(Math.Min(h,100),h*0.5))),fw=(int)Math.Round(fh*a);
        if(fw>Math.Min(w,1400)) { fw=Math.Min(w,1400); fh=(int)Math.Round(fw/a); }
        fw=Math.Max(8,Math.Min(fw,w)); fh=Math.Max(8,Math.Min(fh,h));
        if(texture==0||fw!=width||fh!=height||Math.Abs(a-aspect)>1e-9) { Allocate(fw,fh); aspect=a; }
        if(!lookedUp) {
            lookedUp=true; IntPtr proc=GL.wglGetProcAddress("glBlendEquation");
            if(Invalid(proc)) proc=GL.wglGetProcAddress("glBlendEquationEXT");
            if(!Invalid(proc)) blendEquation=(BlendEquationProc)Marshal.GetDelegateForFunctionPointer(proc,typeof(BlendEquationProc));
        }
        if(v.Frame==renderedFrame) return;
        double dt=renderedFrame<0?1/30.0:Math.Max(0.001,Math.Min(0.1,v.Time-renderedTime));
        renderedFrame=v.Frame; renderedTime=v.Time;
        GL.glViewport(0,0,width,height);
        GL.glMatrixMode(0x1701); GL.glLoadIdentity(); GL.glOrtho(-a,a,-1,1,-1,1);
        GL.glMatrixMode(0x1700); GL.glLoadIdentity();
        GL.glDisable(0x0B50); GL.glDisable(0x0B71); GL.glDisable(0x0BE2);
        // 1. Previous frame through the preset's zoom, turn or flow, slightly faded.
        GL.glEnable(0x0DE1); GL.glBindTexture(0x0DE1,texture);
        float fade=(float)v.Decay(dt); GL.glColor4f(fade,fade,fade,1);
        int columns=Math.Max(8,(int)Math.Round(14*a)),rows=14; double us=(double)width/textureWidth/(2*a),vs=(double)height/textureHeight/2;
        for(int j=0;j<rows;j++) {
            GL.glBegin(8);
            for(int i=0;i<=columns;i++) for(int k=0;k<2;k++) {
                double x=-a+2*a*i/columns,y=-1+2.0*(j+k)/rows,sx,sy; v.Source(x,y,dt,out sx,out sy);
                GL.glTexCoord2d((sx+a)*us,(sy+1)*vs); GL.glVertex2d(x,y);
            }
            GL.glEnd();
        }
        GL.glBindTexture(0x0DE1,0); GL.glDisable(0x0DE1);
        GL.glEnable(0x0BE2);
        // 2. Subtract a hair so 8-bit rounding cannot leave a dim residue forever.
        if(blendEquation!=null) { GL.glBlendFunc(1,1); blendEquation(0x800B); float c=1f/255; GL.glColor4f(c,c,c,1); Paint.Rect(-a,-1,a,1); blendEquation(0x8006); }
        // 3. This frame's shapes, with line widths measured against a 540-line screen.
        v.DrawShapes(a,Math.Max(2.0/540,1.0/height));
        // 4. A black frame keeps samples from beyond the edge dark.
        GL.glDisable(0x0BE2); GL.glColor4f(0,0,0,1); double px=2*a/width,py=2.0/height;
        Paint.Rect(-a,-1,-a+px,1); Paint.Rect(a-px,-1,a,1); Paint.Rect(-a,-1,a,-1+py); Paint.Rect(-a,1-py,a,1);
        // 5. Keep it for the next frame.
        GL.glBindTexture(0x0DE1,texture); GL.glCopyTexSubImage2D(0x0DE1,0,0,0,0,0,width,height); GL.glBindTexture(0x0DE1,0);
    }
    static bool Invalid(IntPtr proc) { long value=proc.ToInt64(); return value>=-1&&value<=3; }
    // Draw the feedback over the canvas (or this view's slice of a shared canvas).
    public void Display(Visualizer v,double canvasAspect,double[] clip) {
        if(texture==0) return;
        int tiles=Tiles(canvasAspect); double a=canvasAspect/tiles;
        GL.glMatrixMode(0x1701); GL.glLoadIdentity();
        if(clip==null) GL.glOrtho(-canvasAspect,canvasAspect,-1,1,-1,1); else GL.glOrtho(clip[0],clip[1],clip[2],clip[3],-1,1);
        GL.glMatrixMode(0x1700); GL.glLoadIdentity();
        GL.glDisable(0x0B50); GL.glDisable(0x0B71); GL.glDisable(0x0BE2);
        GL.glEnable(0x0DE1); GL.glBindTexture(0x0DE1,texture); GL.glColor4f(1,1,1,1);
        var previous=v.Previous; var current=v.Current;
        if(previous!=null&&(previous.Fold>0||current.Fold>0)) {
            DrawTiles(tiles,a,canvasAspect,previous.Fold,v.FoldAngle(previous));
            GL.glEnable(0x0BE2); GL.glBlendFunc(0x0302,0x0303); GL.glColor4f(1,1,1,(float)Visualizer.Smooth(v.Fade));
        }
        DrawTiles(tiles,a,canvasAspect,current.Fold,v.FoldAngle(current));
        GL.glBindTexture(0x0DE1,0); GL.glDisable(0x0DE1); GL.glDisable(0x0BE2);
        GL.glEnable(0x0B71); GL.glEnable(0x0B50);
    }
    void DrawTiles(int tiles,double a,double canvasAspect,int fold,double angle) {
        double us=(double)width/textureWidth/(2*a),vs=(double)height/textureHeight/2;
        for(int k=0;k<tiles;k++) {
            double centre=-canvasAspect+a*(2*k+1),mirror=k%2==0?1:-1;
            if(fold<2) {
                GL.glBegin(7);
                GL.glTexCoord2d(0,0); GL.glVertex2d(centre-a*mirror,-1);
                GL.glTexCoord2d(2*a*us,0); GL.glVertex2d(centre+a*mirror,-1);
                GL.glTexCoord2d(2*a*us,2*vs); GL.glVertex2d(centre+a*mirror,1);
                GL.glTexCoord2d(0,2*vs); GL.glVertex2d(centre-a*mirror,1);
                GL.glEnd();
                continue;
            }
            // Kaleidoscope: alternate wedges reflect one turning slice of the feedback.
            double wedge=Math.PI*2/fold,corner=Math.Atan2(1,a),scale=0.97*Math.Min(a,1)/Math.Sqrt(a*a+1);
            double[] corners={corner,Math.PI-corner,Math.PI+corner,Math.PI*2-corner};
            for(int j=0;j<fold;j++) {
                double from=j*wedge,to=from+wedge;
                GL.glBegin(6);
                WedgeVertex(0,from,from,to,j,angle,scale,a,us,vs,centre,mirror);
                WedgeVertex(Reach(from,a),from,from,to,j,angle,scale,a,us,vs,centre,mirror);
                foreach(double c in corners) if(c>from&&c<to) WedgeVertex(Reach(c,a),c,from,to,j,angle,scale,a,us,vs,centre,mirror);
                WedgeVertex(Reach(to,a),to,from,to,j,angle,scale,a,us,vs,centre,mirror);
                GL.glEnd();
            }
        }
    }
    static double Reach(double angle,double a) {
        double dx=Math.Abs(Math.Cos(angle)),dy=Math.Abs(Math.Sin(angle));
        return Math.Min(dx>1e-9?a/dx:double.MaxValue,dy>1e-9?1/dy:double.MaxValue);
    }
    static void WedgeVertex(double radius,double phi,double from,double to,int j,double angle,double scale,double a,double us,double vs,double centre,double mirror) {
        double source=j%2==0?angle+(phi-from):angle+(to-phi),sx=Math.Cos(source)*radius*scale,sy=Math.Sin(source)*radius*scale;
        GL.glTexCoord2d((sx+a)*us,(sy+1)*vs); GL.glVertex2d(centre+mirror*Math.Cos(phi)*radius,Math.Sin(phi)*radius);
    }
}
}

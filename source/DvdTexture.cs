using System;
using System.Drawing;
using System.Reflection;

namespace RetroPipes {
static class DvdTexture {
    static readonly byte[] alpha;
    static readonly int width,height,textureWidth,textureHeight;
    public static double AspectRatio { get { return (double)height/width; } }
    public static double U { get { return (double)width/textureWidth; } }
    public static double V { get { return (double)height/textureHeight; } }
    static DvdTexture() {
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("RetroPipes.DVDVideo"))
        using(var bitmap=new Bitmap(stream)) {
            width=bitmap.Width; height=bitmap.Height;
            textureWidth=PowerOfTwo(width); textureHeight=PowerOfTwo(height);
            alpha=new byte[textureWidth*textureHeight];
            // Use the PNG's coverage as a mask so each bounce can tint the logo.
            // Transparent holes and antialiased edges remain transparent.
            for(int y=0;y<height;y++) for(int x=0;x<width;x++) alpha[y*textureWidth+x]=bitmap.GetPixel(x,y).A;
        }
    }
    static int PowerOfTwo(int value) { int result=1; while(result<value) result*=2; return result; }
    public static uint Create() {
        uint texture; GL.glGenTextures(1,out texture);
        if(texture==0) throw new Exception("Could not allocate the DVD logo texture.");
        GL.glBindTexture(0x0DE1,texture);
        GL.glTexParameteri(0x0DE1,0x2801,0x2601); GL.glTexParameteri(0x0DE1,0x2800,0x2601);
        GL.glTexParameteri(0x0DE1,0x2802,0x812F); GL.glTexParameteri(0x0DE1,0x2803,0x812F);
        GL.glPixelStorei(0x0CF5,1);
        GL.glTexImage2D(0x0DE1,0,0x1906,textureWidth,textureHeight,0,0x1906,0x1401,alpha);
        GL.glBindTexture(0x0DE1,0); GL.glPixelStorei(0x0CF5,4);
        return texture;
    }
}
}

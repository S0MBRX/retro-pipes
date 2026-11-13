using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace RetroPipes {
// One snapshot of what the PC is playing. All values are 0..1 (Wave is -1..1).
sealed class AudioFrame {
    public const int Bands=32,Points=128;
    public float Level,Bass,Mid,Treble; public int Beats; public bool Live;
    public readonly float[] Spectrum=new float[Bands],Wave=new float[Points];
    public static readonly AudioFrame Silent=new AudioFrame();
}

// Hann-windowed 2048-point FFT every 512 samples: bass/mid/treble, overall level,
// 32 log-spaced bars, a triggered oscilloscope trace and bass-onset beats.
// Levels follow a slow automatic gain so quiet and loud tracks both move the picture.
sealed class AudioAnalyzer {
    const int Size=2048,Hop=512,History=90;
    readonly float[] ring=new float[Size]; int write,sinceHop;
    readonly double[] re=new double[Size],im=new double[Size],window=new double[Size],power=new double[Size/2];
    readonly double[] cosTable=new double[Size/2],sinTable=new double[Size/2]; readonly int[] reverse=new int[Size];
    readonly double[] bassHistory=new double[History]; int historyIndex,historyCount;
    readonly double[] bandRef=new double[3];
    int inputRate,decimate=1,decimateCount; double rate=48000,decimateSum;
    double levelRef,spectrumRef,waveRef,sinceBeat=10,silentFor=10; int beats;
    volatile AudioFrame latest=AudioFrame.Silent;
    public AudioFrame Latest { get { return latest; } }
    public double SampleRate { get { return rate; } }
    public AudioAnalyzer() {
        int bits=0; while((1<<bits)<Size) bits++;
        for(int i=0;i<Size;i++) {
            window[i]=0.5-0.5*Math.Cos(2*Math.PI*i/(Size-1));
            int r=0; for(int b=0;b<bits;b++) if((i&(1<<b))!=0) r|=1<<(bits-1-b);
            reverse[i]=r;
        }
        for(int k=0;k<Size/2;k++) { cosTable[k]=Math.Cos(2*Math.PI*k/Size); sinTable[k]=Math.Sin(2*Math.PI*k/Size); }
    }
    public void Push(float[] samples,int count,int sampleRate) {
        if(sampleRate!=inputRate) {
            inputRate=sampleRate; decimate=Math.Max(1,(int)Math.Round(sampleRate/48000.0)); rate=(double)sampleRate/decimate;
            decimateCount=0; decimateSum=0;
        }
        for(int i=0;i<count;i++) {
            float s=samples[i];
            if(decimate>1) { decimateSum+=s; if(++decimateCount<decimate) continue; s=(float)(decimateSum/decimate); decimateSum=0; decimateCount=0; }
            ring[write]=s; write=(write+1)&(Size-1);
            if(++sinceHop>=Hop) { sinceHop=0; Analyze(); }
        }
    }
    float Sample(int newestOffset) { return ring[(write-1-newestOffset)&(Size-1)]; }
    void Transform() {
        for(int i=0;i<Size;i++) { int j=reverse[i]; if(j>i) { double t=re[i]; re[i]=re[j]; re[j]=t; t=im[i]; im[i]=im[j]; im[j]=t; } }
        for(int length=2;length<=Size;length<<=1) {
            int half=length>>1,step=Size/length;
            for(int start=0;start<Size;start+=length) for(int k=0;k<half;k++) {
                double wr=cosTable[k*step],wi=-sinTable[k*step]; int a=start+k,b=a+half;
                double xr=re[b]*wr-im[b]*wi,xi=re[b]*wi+im[b]*wr;
                re[b]=re[a]-xr; im[b]=im[a]-xi; re[a]+=xr; im[a]+=xi;
            }
        }
    }
    double Band(double from,double to) {
        double binHz=rate/Size; int k0=Math.Max(1,(int)Math.Ceiling(from/binHz)),k1=Math.Min(Size/2-1,(int)Math.Floor(to/binHz));
        if(k1<k0) k1=k0; double sum=0; for(int k=k0;k<=k1;k++) sum+=power[k]; return sum;
    }
    double Amplitude(double hz) {
        double position=Math.Max(1,Math.Min(Size/2-2,hz/(rate/Size))); int k=(int)position; double f=position-k;
        return Math.Sqrt(power[k])*(1-f)+Math.Sqrt(power[k+1])*f;
    }
    static float Relative(double value,ref double reference,double floor,double decay,double range,bool square) {
        reference=Math.Max(Math.Max(value,floor),reference*decay);
        if(value<=1e-12) return 0;
        double n=1+20*Math.Log10(value/reference)/range; n=Math.Max(0,Math.Min(1,n));
        return (float)(square?n*n:n);
    }
    void Analyze() {
        int oldest=write; double squares=0;
        for(int i=0;i<Size;i++) { double s=ring[(oldest+i)&(Size-1)]; re[i]=s*window[i]; im[i]=0; if(i>=Size-1024) squares+=s*s; }
        Transform();
        double scale=4.0/Size;
        for(int k=1;k<Size/2;k++) power[k]=(re[k]*re[k]+im[k]*im[k])*scale*scale;
        double dt=Hop/rate,decay=Math.Exp(-dt/4),rms=Math.Sqrt(squares/1024);
        silentFor=rms<3e-5?silentFor+dt:0; bool live=silentFor<0.6;
        var frame=new AudioFrame {Live=live};
        frame.Level=Relative(rms,ref levelRef,0.0025,decay,30,true);
        frame.Bass=Relative(Math.Sqrt(Band(30,180)),ref bandRef[0],0.002,decay,24,true);
        frame.Mid=Relative(Math.Sqrt(Band(180,2500)),ref bandRef[1],0.002,decay,24,true);
        frame.Treble=Relative(Math.Sqrt(Band(2500,14000)),ref bandRef[2],0.001,decay,24,true);
        // 32 log-spaced bars from 50 Hz to 16 kHz with a gentle treble tilt.
        double binHz=rate/Size,loudest=0; var bars=new double[AudioFrame.Bands];
        for(int b=0;b<AudioFrame.Bands;b++) {
            double f0=50*Math.Pow(320,(double)b/AudioFrame.Bands),f1=50*Math.Pow(320,(b+1.0)/AudioFrame.Bands),centre=Math.Sqrt(f0*f1);
            double value=(f1-f0)/binHz<2?Amplitude(centre):Math.Sqrt(Band(f0,f1));
            bars[b]=value*Math.Pow(centre/1000,0.15); loudest=Math.Max(loudest,bars[b]);
        }
        spectrumRef=Math.Max(Math.Max(loudest,0.002),spectrumRef*Math.Exp(-dt/3));
        for(int b=0;b<AudioFrame.Bands;b++) frame.Spectrum[b]=bars[b]<=1e-12?0:(float)Math.Max(0,Math.Min(1,1+20*Math.Log10(bars[b]/spectrumRef)/40));
        // Oscilloscope: start at a rising zero crossing so the trace holds still.
        const int span=768; int trigger=0;
        for(int i=1;i<256;i++) if(Sample(span-i)<0&&Sample(span-1-i)>=0) { trigger=i; break; }
        double peak=0; var points=new double[AudioFrame.Points];
        for(int p=0;p<AudioFrame.Points;p++) {
            double sum=0; for(int j=0;j<4;j++) sum+=Sample(span-1-trigger-p*4-j);
            points[p]=sum/4; peak=Math.Max(peak,Math.Abs(points[p]));
        }
        waveRef=Math.Max(Math.Max(peak,0.01),waveRef*Math.Exp(-dt/2));
        for(int p=0;p<AudioFrame.Points;p++) frame.Wave[p]=(float)Math.Max(-1,Math.Min(1,points[p]/waveRef));
        // Beats: bass energy jumping well above its recent average.
        double bassPower=Band(35,160),average=0;
        for(int i=0;i<historyCount;i++) average+=bassHistory[i];
        average=historyCount>0?average/historyCount:0;
        bassHistory[historyIndex]=bassPower; historyIndex=(historyIndex+1)%History; if(historyCount<History) historyCount++;
        sinceBeat+=dt;
        if(live&&historyCount>20&&bassPower>average*1.5&&bassPower>2e-6&&sinceBeat>0.28) { beats++; sinceBeat=0; }
        frame.Beats=beats;
        latest=frame;
    }
}

// Shared WASAPI loopback capture of the default playback device ("what you hear").
// Nothing is recorded or stored; it only runs while a visualizer is on screen.
static class AudioCapture {
    sealed class Session { public volatile bool Stop; public readonly AudioAnalyzer Analyzer=new AudioAnalyzer(); }
    static readonly object gate=new object(); static int users; static volatile Session session; static volatile string status="Audio capture has not started.";
    public static string Status { get { return status; } }
    public static bool Running { get { return session!=null; } }
    public static AudioFrame Latest() { var s=session; return s==null?AudioFrame.Silent:s.Analyzer.Latest; }
    public static void Acquire() {
        lock(gate) {
            users++; if(session!=null) return;
            var s=new Session(); session=s;
            var thread=new Thread(delegate() { Run(s); }) {IsBackground=true,Name="Retro Pipes audio"};
            thread.SetApartmentState(ApartmentState.MTA); thread.Start();
        }
    }
    public static void Release() {
        lock(gate) { if(users>0) users--; if(users==0&&session!=null) { session.Stop=true; session=null; } }
    }
    static void Run(Session s) {
        while(!s.Stop) {
            try { Capture(s); }
            catch(Exception e) { status="Waiting for a playback device ("+e.Message.Trim()+")."; }
            var silence=new float[4800]; for(int i=0;i<10;i++) s.Analyzer.Push(silence,silence.Length,48000);
            for(int i=0;i<20&&!s.Stop;i++) Thread.Sleep(100);
        }
    }
    static void Check(int hr) { if(hr<0) Marshal.ThrowExceptionForHR(hr); }
    static void Free(object com) { if(com!=null) Marshal.ReleaseComObject(com); }
    static readonly Guid FloatFormat=new Guid("00000003-0000-0010-8000-00aa00389b71");
    static void Capture(Session s) {
        IMMDeviceEnumerator devices=null; IMMDevice device=null; IAudioClient client=null; IAudioCaptureClient capture=null; IntPtr format=IntPtr.Zero;
        try {
            devices=(IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            Check(devices.GetDefaultAudioEndpoint(0,0,out device));
            string id; Check(device.GetId(out id));
            object instance; var clientId=typeof(IAudioClient).GUID;
            Check(device.Activate(ref clientId,23,IntPtr.Zero,out instance)); client=(IAudioClient)instance;
            Check(client.GetMixFormat(out format));
            int tag=(ushort)Marshal.ReadInt16(format,0),channels=Math.Max(1,(int)(ushort)Marshal.ReadInt16(format,2)),rate=Marshal.ReadInt32(format,4);
            int block=(ushort)Marshal.ReadInt16(format,12),bits=(ushort)Marshal.ReadInt16(format,14),extra=(ushort)Marshal.ReadInt16(format,16);
            bool isFloat=tag==3;
            if(tag==0xFFFE&&extra>=22) { var guid=new byte[16]; Marshal.Copy(IntPtr.Add(format,24),guid,0,16); isFloat=new Guid(guid)==FloatFormat; }
            int width=block/channels;
            if(rate<8000||(isFloat&&width!=4)||(!isFloat&&width!=2&&width!=3&&width!=4)) throw new NotSupportedException("unsupported mix format");
            // Shared-mode loopback with a 200 ms buffer, polled every 10 ms.
            Check(client.Initialize(0,0x00020000,2000000,0,format,IntPtr.Zero));
            var captureId=typeof(IAudioCaptureClient).GUID; Check(client.GetService(ref captureId,out instance)); capture=(IAudioCaptureClient)instance;
            Check(client.Start());
            status="Listening to the default playback device: "+rate+" Hz, "+channels+" channel"+(channels==1?"":"s")+", "+(isFloat?"32-bit float":bits+"-bit PCM")+".";
            var clock=Stopwatch.StartNew(); double lastData=0,lastFill=0,lastCheck=0;
            var mono=new float[4096]; var bytes=new byte[0]; var zeros=new float[rate/5+1];
            while(!s.Stop) {
                Thread.Sleep(10);
                int packet; Check(capture.GetNextPacketSize(out packet)); bool got=false;
                while(packet>0&&!s.Stop) {
                    IntPtr data; int frames,flags; long devicePosition,counter;
                    Check(capture.GetBuffer(out data,out frames,out flags,out devicePosition,out counter));
                    if(frames>0) {
                        if(mono.Length<frames) mono=new float[frames];
                        if((flags&2)!=0||data==IntPtr.Zero) Array.Clear(mono,0,frames);
                        else {
                            int length=frames*block; if(bytes.Length<length) bytes=new byte[length];
                            Marshal.Copy(data,bytes,0,length);
                            for(int f=0;f<frames;f++) {
                                double sum=0; int offset=f*block;
                                for(int c=0;c<channels;c++,offset+=width) {
                                    if(isFloat) sum+=BitConverter.ToSingle(bytes,offset);
                                    else if(width==2) sum+=BitConverter.ToInt16(bytes,offset)/32768.0;
                                    else if(width==3) sum+=((bytes[offset]<<8|bytes[offset+1]<<16|bytes[offset+2]<<24)>>8)/8388608.0;
                                    else sum+=BitConverter.ToInt32(bytes,offset)/2147483648.0;
                                }
                                mono[f]=(float)(sum/channels);
                            }
                        }
                        s.Analyzer.Push(mono,frames,rate);
                    }
                    Check(capture.ReleaseBuffer(frames)); got=true;
                    Check(capture.GetNextPacketSize(out packet));
                }
                double now=clock.Elapsed.TotalSeconds;
                // Windows sends no packets while nothing plays; feed silence so levels settle.
                if(got) lastData=now;
                else if(now-lastData>0.1) s.Analyzer.Push(zeros,(int)Math.Min(zeros.Length,(now-lastFill)*rate),rate);
                lastFill=now;
                if(now-lastCheck>2) {
                    lastCheck=now; IMMDevice current; string currentId=null;
                    if(devices.GetDefaultAudioEndpoint(0,0,out current)>=0) { current.GetId(out currentId); Free(current); }
                    if(currentId!=id) break;
                }
            }
            client.Stop();
        } finally {
            if(format!=IntPtr.Zero) Marshal.FreeCoTaskMem(format);
            Free(capture); Free(client); Free(device); Free(devices);
        }
    }
    [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class MMDeviceEnumeratorComObject { }
    [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow,int stateMask,out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow,int role,out IMMDevice device);
    }
    [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice {
        [PreserveSig] int Activate(ref Guid iid,int context,IntPtr parameters,[MarshalAs(UnmanagedType.IUnknown)] out object instance);
        [PreserveSig] int OpenPropertyStore(int access,out IntPtr properties);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig] int GetState(out int state);
    }
    [ComImport,Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioClient {
        [PreserveSig] int Initialize(int shareMode,int streamFlags,long bufferDuration,long periodicity,IntPtr format,IntPtr sessionGuid);
        [PreserveSig] int GetBufferSize(out int frames);
        [PreserveSig] int GetStreamLatency(out long latency);
        [PreserveSig] int GetCurrentPadding(out int frames);
        [PreserveSig] int IsFormatSupported(int shareMode,IntPtr format,out IntPtr closest);
        [PreserveSig] int GetMixFormat(out IntPtr format);
        [PreserveSig] int GetDevicePeriod(out long defaultPeriod,out long minimumPeriod);
        [PreserveSig] int Start();
        [PreserveSig] int Stop();
        [PreserveSig] int Reset();
        [PreserveSig] int SetEventHandle(IntPtr handle);
        [PreserveSig] int GetService(ref Guid iid,[MarshalAs(UnmanagedType.IUnknown)] out object service);
    }
    [ComImport,Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioCaptureClient {
        [PreserveSig] int GetBuffer(out IntPtr data,out int frames,out int flags,out long devicePosition,out long counterPosition);
        [PreserveSig] int ReleaseBuffer(int frames);
        [PreserveSig] int GetNextPacketSize(out int frames);
    }
}
}

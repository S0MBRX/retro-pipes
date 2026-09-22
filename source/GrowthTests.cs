using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RetroPipes {
static class GrowthTests {
    public static void Run(string dir) {
        var directions=new[]{new Cell(1,0,0),new Cell(-1,0,0),new Cell(0,1,0),new Cell(0,-1,0),new Cell(0,0,1),new Cell(0,0,-1)};
        // Leave only an outward move free, well outside every old boundary.
        foreach(var direction in directions) {
            var scene=new Scene(new Settings {Count=1,Speed=1},19,1.6);
            var start=new Cell(direction.X*100,direction.Y*100,direction.Z*100);
            scene.Occupied.Clear(); scene.Occupied.Add(start); var pipe=scene.Pipes[0]; pipe.At=start;
            foreach(var block in directions) if(!block.Equals(direction)) scene.Occupied.Add(start+block);
            scene.Step(0.01);
            if(pipe.Dead||pipe.Growing==null||!pipe.Growing.B.Equals(start+direction)) throw new Exception("A former spatial boundary still prevents growth");
        }
        foreach(bool span in new[]{false,true}) foreach(bool orbit in new[]{false,true}) {
            double aspect=span?16.0/3:16.0/9;
            var scene=new Scene(new Settings {Count=1,SpanAllScreens=span,Rotate=orbit},15,aspect);
            var distant=new Cell(100,90,80); var view=scene.ViewAt(0); double lowest=double.MaxValue,highest=double.MinValue,bestWeight=0,worstWeight=0;
            foreach(var direction in directions) {
                double cost=view.Cost(distant+direction),weight=scene.DirectionWeight(distant,direction,new Cell());
                if(weight<=0||double.IsNaN(weight)||double.IsInfinity(weight)) throw new Exception("Soft bias forbids an otherwise valid direction");
                if(cost<lowest) { lowest=cost; bestWeight=weight; }
                if(cost>highest) { highest=cost; worstWeight=weight; }
            }
            if(bestWeight<=worstWeight) throw new Exception("Offscreen growth does not prefer returning towards view");
        }
        int escapedX=0,escapedY=0,escapedZ=0;
        for(int seed=0;seed<30;seed++) {
            var scene=new Scene(new Settings {Speed=5,Count=5},seed,1.6);
            for(int step=0;step<400;step++) scene.Step(0.03);
            foreach(var segment in scene.Segments) {
                if(Math.Abs(segment.B.X)>scene.HalfWidth) escapedX++;
                if(Math.Abs(segment.B.Y)>scene.HalfHeight) escapedY++;
                if(Math.Abs(segment.B.Z)>4) escapedZ++;
            }
        }
        if(escapedX<30||escapedY<30||escapedZ<30) throw new Exception("Random layouts still form the old bounded corridor");
        File.WriteAllText(Path.Combine(dir,"growth-results.txt"),"Endpoints beyond former X/Y/Z walls across 30 layouts: "+escapedX+" / "+escapedY+" / "+escapedZ);
        using(var window=new PipesWindow(new Settings {SpanAllScreens=true,Rotate=true,Speed=5,Count=10},"test",new Rectangle(0,0,1800,600),IntPtr.Zero,91)) {
            window.Show(); Application.DoEvents(); for(int frame=0;frame<1500;frame++) window.Advance(0.02);
            window.SaveFrame(Path.Combine(dir,"Free-growth-preview.png")); window.Close();
        }
    }
}
}

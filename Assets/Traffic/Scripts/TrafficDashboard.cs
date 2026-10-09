using UnityEngine;

namespace JunctionLab
{
    // Dependency-free dashboard; scales with the window and keeps the scene viewport unobstructed.
    public class TrafficDashboard : MonoBehaviour
    {
        public JunctionController junction;
        readonly string[] names={"SOUTH", "WEST", "NORTH", "EAST"};
        Texture2D pixel;
        GUIStyle text, small, heading, number, button;
        Color ink=new Color(.045f,.075f,.11f), muted=new Color(.56f,.65f,.69f), mint=new Color(.3f,.9f,.71f);
        void Init()
        {
            pixel=Texture2D.whiteTexture;
            text=new GUIStyle(GUI.skin.label){fontSize=14,normal={textColor=Color.white}};
            small=new GUIStyle(text){fontSize=11,normal={textColor=muted}};
            heading=new GUIStyle(text){fontSize=26,fontStyle=FontStyle.Bold};
            number=new GUIStyle(heading){fontSize=32,normal={textColor=mint}};
            button=new GUIStyle(GUI.skin.button){fontSize=13,fontStyle=FontStyle.Bold};
            button.normal.textColor=Color.white;
        }
        void Fill(Rect r,Color color) { var old=GUI.color; GUI.color=color; GUI.DrawTexture(r,pixel); GUI.color=old; }
        void Label(float x,float y,string value,GUIStyle style,float width=280) { GUI.Label(new Rect(x,y,width,40),value,style); }
        void OnGUI()
        {
            if(junction.Model==null) return;
            if(text==null) Init();
            float scale=Mathf.Min(Screen.height/900f,Screen.width/1200f);
            scale=Mathf.Max(.45f,scale);
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float w=Screen.width/scale,h=Screen.height/scale;
            junction.sceneCamera.rect=new Rect(320/w,0,1-320/w,1);
            var m=junction.Model;
            Fill(new Rect(0,0,320,h),ink);
            Fill(new Rect(22,28,28,4),mint);
            Label(22,44,"JUNCTION / LAB",heading);
            Label(22,81,"TRAFFIC SYSTEMS  /  01",small);
            Fill(new Rect(22,119,276,1),new Color(.2f,.27f,.3f));
            Label(22,137,"NETWORK OVERVIEW",small);
            Label(22,160,m.Completed.ToString("000"),number);
            Label(158,160,m.Cars.Count.ToString("00"),number);
            Label(22,202,"TRIPS COMPLETED",small);
            Label(158,202,"ACTIVE VEHICLES",small);
            Label(22,236,$"{m.MeanWait:0.0}s",heading);
            Label(158,236,$"{m.Elapsed/60:0.0} min",heading);
            Label(22,273,"MEAN STOPPED TIME",small);
            Label(158,273,"SIMULATED TIME",small);
            Label(22,315,"SIGNAL CONTROL",small);
            for(int a=0;a<4;a++)
            {
                float y=348+a*40;
                Fill(new Rect(22,y,276,33),new Color(.09f,.13f,.17f));
                bool green=m.IsGreen(a),amber=m.ActiveApproach==a&&m.Stage==SignalStage.Yellow;
                Fill(new Rect(33,y+11,10,10),green?mint:amber?new Color(1,.68f,.2f):new Color(.9f,.3f,.3f));
                Label(54,y+6,names[a],text,100);
                Label(184,y+7,$"{m.Queue[a]} queued",small,110);
            }
            Label(22,515,$"{m.Stage.ToString().ToUpper()}  /  {m.PhaseTime:0.0}s",small);
            if(GUI.Button(new Rect(22,550,276,34),m.Adaptive?"ADAPTIVE TIMING  ON":"FIXED CYCLE TIMING",button)) m.Adaptive=!m.Adaptive;
            Label(22,601,$"Arrival demand    {m.Demand*240:0} vehicles/min",text);
            m.Demand=GUI.HorizontalSlider(new Rect(22,630,276,18),m.Demand,.05f,.65f);
            Label(22,657,$"Green duration    {m.GreenDuration:0}s",text);
            m.GreenDuration=GUI.HorizontalSlider(new Rect(22,687,276,18),m.GreenDuration,4,20);
            if(GUI.Button(new Rect(22,725,132,34),junction.Paused?"RESUME":"PAUSE",button)) junction.TogglePause();
            if(GUI.Button(new Rect(166,725,132,34),$"SPEED  {junction.simulationSpeed:0}x",button)) junction.simulationSpeed=junction.simulationSpeed==1?2:junction.simulationSpeed==2?4:1;
            if(GUI.Button(new Rect(22,770,132,34),"NEXT PHASE",button)) m.RequestNextPhase();
            if(GUI.Button(new Rect(166,770,132,34),"RESET",button)) junction.ResetSimulation();
            Label(22,831,"Protected turns / right-hand traffic",small);
            Label(22,852,$"Pool: {junction.PoolCount}   |   Deferred arrivals: {m.DeferredArrivals}",small);
            Fill(new Rect(342,24,370,61),ink);
            Label(359,33,"FOUR-WAY / DOWNTOWN",text,340);
            Label(359,58,"A small, configurable traffic simulation",small,340);
            if(GUI.Button(new Rect(w-380,h-60,112,34),"ROUTES",button)) junction.ToggleRoutes();
            if(GUI.Button(new Rect(w-256,h-60,112,34),"ROTATE",button)) junction.RotateCamera();
            if(GUI.Button(new Rect(w-132,h-60,112,34),"TOP VIEW",button)) junction.ToggleCamera();
            GUI.matrix=Matrix4x4.identity;
        }
    }
}

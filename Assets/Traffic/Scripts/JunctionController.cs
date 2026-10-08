using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunctionLab
{
    public class JunctionController : MonoBehaviour
    {
        public TrafficSettings settings;
        public GameObject[] vehiclePrefabs;
        public Camera sceneCamera;
        public Renderer[] redLights, amberLights, greenLights;
        public TrafficManager Model { get; private set; }
        public bool Paused { get; private set; }
        public float simulationSpeed = 1;
        readonly Dictionary<int, GameObject> views = new Dictionary<int, GameObject>();
        readonly Stack<GameObject>[] pool = { new Stack<GameObject>(), new Stack<GameObject>(), new Stack<GameObject>() };
        readonly List<int> retired = new List<int>();
        readonly HashSet<int> live = new HashSet<int>();
        readonly List<LineRenderer> routes = new List<LineRenderer>();
        float accumulator;
        float cameraYaw=-35;
        bool topDown, showRoutes;
        MaterialPropertyBlock lightProperties;
        public int PoolCount { get { int count=0; foreach(var stack in pool) count+=stack.Count; return count; } }

        void Awake()
        {
            Application.targetFrameRate=60;
            Model=new TrafficManager(settings);
            lightProperties=new MaterialPropertyBlock();
            for(int a=0;a<4;a++) for(int t=0;t<3;t++)
            {
                var go=new GameObject("Route "+a+" "+(Turn)t); go.transform.SetParent(transform);
                var line=go.AddComponent<LineRenderer>();
                line.sharedMaterial=new Material(Shader.Find("Sprites/Default"));
                line.startColor=line.endColor=new Color(.15f,.95f,.85f,.7f);
                line.startWidth=line.endWidth=.12f; line.positionCount=Model.Routes[a,t].Points.Length;
                var points=(Vector3[])Model.Routes[a,t].Points.Clone();
                for(int i=0;i<points.Length;i++) points[i]+=Vector3.up*.16f;
                line.SetPositions(points); line.enabled=false; routes.Add(line);
            }
            UpdateCamera();
        }
        void Update()
        {
            if(!Paused)
            {
                accumulator+=Mathf.Min(Time.unscaledDeltaTime,.1f)*simulationSpeed;
                while(accumulator>=.02f) { Model.Step(.02f); accumulator-=.02f; }
            }
            RefreshViews();
            for(int a=0;a<4;a++)
            {
                SetLamp(redLights[a],!Model.IsGreen(a) && !(a==Model.ActiveApproach && Model.Stage==SignalStage.Yellow),new Color(1,.22f,.2f));
                SetLamp(amberLights[a],a==Model.ActiveApproach && Model.Stage==SignalStage.Yellow,new Color(1,.65f,.1f));
                SetLamp(greenLights[a],Model.IsGreen(a),new Color(.15f,1,.57f));
            }
        }
        void SetLamp(Renderer renderer,bool on,Color color)
        {
            lightProperties.SetColor("_Color",on?color:new Color(.09f,.12f,.15f));
            lightProperties.SetColor("_EmissionColor",on?color*.7f:Color.black);
            renderer.SetPropertyBlock(lightProperties);
        }
        public void RefreshViews()
        {
            live.Clear();
            foreach(var car in Model.Cars)
            {
                live.Add(car.Id);
                if(!views.TryGetValue(car.Id,out var view))
                {
                    int variant=car.Id%vehiclePrefabs.Length;
                    view=pool[variant].Count>0?pool[variant].Pop():Instantiate(vehiclePrefabs[variant],transform);
                    view.SetActive(true); views.Add(car.Id,view);
                }
                view.transform.SetPositionAndRotation(car.Position,Quaternion.LookRotation(car.Route.Forward(car.Distance)));
            }
            retired.Clear();
            foreach(var pair in views) if(!live.Contains(pair.Key)) retired.Add(pair.Key);
            foreach(int id in retired)
            { var view=views[id]; view.SetActive(false); pool[id%vehiclePrefabs.Length].Push(view); views.Remove(id); }
        }
        public void ResetSimulation()
        {
            foreach(var pair in views) { pair.Value.SetActive(false); pool[pair.Key%vehiclePrefabs.Length].Push(pair.Value); }
            views.Clear();
            float demand=Model.Demand, duration=Model.GreenDuration; bool adaptive=Model.Adaptive;
            Model=new TrafficManager(settings){Demand=demand,GreenDuration=duration,Adaptive=adaptive};
            accumulator=0; Paused=false;
        }
        public void TogglePause() { Paused=!Paused; }
        public void ToggleRoutes() { showRoutes=!showRoutes; foreach(var line in routes) line.enabled=showRoutes; }
        public void RotateCamera() { cameraYaw+=90; UpdateCamera(); }
        public void ToggleCamera() { topDown=!topDown; UpdateCamera(); }
        void UpdateCamera()
        {
            sceneCamera.orthographic=true; sceneCamera.orthographicSize=45;
            sceneCamera.transform.position=topDown?new Vector3(0,80,0):Quaternion.Euler(0,cameraYaw,0)*new Vector3(0,55,-55);
            sceneCamera.transform.rotation=topDown?Quaternion.Euler(90,0,0):Quaternion.LookRotation(-sceneCamera.transform.position);
        }
    }
}

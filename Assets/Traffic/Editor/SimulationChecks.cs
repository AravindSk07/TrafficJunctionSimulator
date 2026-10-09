using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JunctionLab.Editor
{
    public static class SimulationChecks
    {
        static void Require(bool value,string message) { if(!value) throw new Exception(message); }

        [MenuItem("Junction Lab/Run Simulation Checks")]
        public static void Run()
        {
            var report=new StringBuilder("Junction Lab deterministic simulation checks\n");
            foreach(bool adaptive in new[]{false,true}) foreach(float demand in new[]{.1f,.35f,.65f})
            {
                var settings=ScriptableObject.CreateInstance<TrafficSettings>();
                var sim=new TrafficManager(settings){Adaptive=adaptive,Demand=demand};
                var twin=new TrafficManager(settings){Adaptive=adaptive,Demand=demand};
                int[] service=new int[4]; int previous=-1;
                float minSeparation=float.MaxValue;
                for(int step=0;step<15000;step++)
                {
                    sim.Step(.02f); twin.Step(.02f);
                    Require(sim.Cars.Count<=settings.capacity,"Capacity exceeded");
                    Require(sim.Completed==twin.Completed && sim.Cars.Count==twin.Cars.Count,"Deterministic replay diverged");
                    if(sim.Stage==SignalStage.Green && previous!=sim.ActiveApproach) { service[sim.ActiveApproach]++; previous=sim.ActiveApproach; }
                    int inside=0;
                    foreach(var car in sim.Cars)
                    {
                        Require(!float.IsNaN(car.Distance),"Invalid vehicle distance");
                        Require(car.Admitted || car.Distance<=car.Route.StopDistance+.001f,"Unadmitted vehicle passed stop line");
                        if(car.Admitted && car.Distance<car.Route.ClearDistance) inside++;
                    }
                    Require(inside<=1,"Conflicting intersection reservations");
                    for(int i=0;i<sim.Cars.Count;i++) for(int j=i+1;j<sim.Cars.Count;j++)
                    {
                        float separation=Vector3.Distance(sim.Cars[i].Position,sim.Cars[j].Position);
                        minSeparation=Mathf.Min(minSeparation,separation);
                        Require(separation>3.1f,"Vehicle overlap at step "+step+" separation "+separation);
                    }
                    if(step%2500==1000) { sim.RequestNextPhase(); twin.RequestNextPhase(); }
                }
                Require(sim.Completed>15,"Traffic failed to make progress");
                for(int a=0;a<4;a++) Require(service[a]>1,"Starved approach "+a);
                report.AppendLine($"PASS adaptive={adaptive} demand={demand} duration=300s trips={sim.Completed} minSeparation={minSeparation:0.00}m meanWait={sim.MeanWait:0.0}s");
                UnityEngine.Object.DestroyImmediate(settings);
            }
            var setting=ScriptableObject.CreateInstance<TrafficSettings>();
            var direct=new TrafficManager(setting){Demand=0};
            Require(direct.TrySpawn(0,Turn.Left),"Initial spawn failed");
            Require(!direct.TrySpawn(0,Turn.Right),"Blocked spawn accepted");
            Require(direct.TrySpawn(2,Turn.Straight),"Opposing spawn failed");
            for(int i=0;i<6000;i++) direct.Step(.02f);
            Require(direct.Completed==2,"Vehicles failed to clear/drain");
            report.AppendLine("PASS blocked spawn, opposing red, turning, final drain");
            UnityEngine.Object.DestroyImmediate(setting);
            Directory.CreateDirectory("Evidence"); File.WriteAllText("Evidence/simulation-checks.txt",report.ToString());
            Debug.Log(report.ToString()+"TRAFFIC_TESTS_PASS");
        }
    }
}

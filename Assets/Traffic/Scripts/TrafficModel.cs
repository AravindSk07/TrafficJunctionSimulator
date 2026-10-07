using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunctionLab
{
    public sealed class CarInfo
    {
        public int Id;
        public TrafficRoute Route;
        public float Distance, Speed, Age, Wait;
        public bool Admitted;
        public Vector3 Position => Route.Position(Distance);
    }

    public sealed class TrafficManager
    {
        public readonly List<CarInfo> Cars = new List<CarInfo>();
        public readonly TrafficRoute[,] Routes = new TrafficRoute[4,3];
        public readonly int[] Queue = new int[4];
        public TrafficSettings Settings { get; }
        public SignalStage Stage { get; private set; }
        public int ActiveApproach { get; private set; }
        public float PhaseTime { get; private set; }
        public float Elapsed { get; private set; }
        public int Completed { get; private set; }
        public int DeferredArrivals { get; private set; }
        public float TotalWait { get; private set; }
        public int Reservation { get; private set; } = -1;
        public bool Adaptive;
        public float Demand, GreenDuration;
        public float MeanWait => Completed > 0 ? TotalWait / Completed : 0;
        readonly System.Random random;
        readonly float[] spawnIn = new float[4];
        readonly float[] lastServed = new float[4];
        int nextId;
        bool request;

        public TrafficManager(TrafficSettings settings)
        {
            Settings=settings; Adaptive=settings.adaptive; Demand=settings.arrivalsPerSecondPerApproach;
            GreenDuration=settings.greenSeconds; random=new System.Random(settings.seed);
            for (int a=0;a<4;a++)
            {
                spawnIn[a]=a*.7f;
                for (int t=0;t<3;t++) Routes[a,t]=new TrafficRoute(a,(Turn)t);
            }
        }
        public void RequestNextPhase() { request=true; }
        public bool IsGreen(int approach) => Stage==SignalStage.Green && ActiveApproach==approach;

        public bool TrySpawn(int approach, Turn turn)
        {
            var route=Routes[approach,(int)turn];
            if (Cars.Count>=Settings.capacity) return false;
            foreach(var car in Cars)
                if (Vector3.Distance(car.Position,route.Points[0])<6) return false;
            Cars.Add(new CarInfo{Id=nextId++,Route=route});
            return true;
        }

        public void Step(float dt)
        {
            if (dt<=0) return;
            Elapsed+=dt; PhaseTime+=dt;
            Array.Clear(Queue,0,4);
            foreach(var car in Cars)
                if (!car.Admitted) Queue[car.Route.Approach]++;
            UpdateSignal();
            for(int a=0;a<4;a++)
            {
                spawnIn[a]-=dt;
                if(spawnIn[a]<=0 && Demand>0)
                {
                    if(!TrySpawn(a,(Turn)random.Next(3))) DeferredArrivals++;
                    spawnIn[a]=(float)(.65+random.NextDouble()*.7)/Demand;
                }
            }
            // Oldest first provides FIFO service at the shared single-lane entry.
            for(int i=0;i<Cars.Count;i++)
            {
                var car=Cars[i]; var route=car.Route;
                car.Age+=dt;
                float available=float.PositiveInfinity;
                if(!car.Admitted)
                {
                    if(Reservation<0 && IsGreen(route.Approach) && car.Distance>=route.StopDistance-.3f)
                    { Reservation=car.Id; car.Admitted=true; }
                    else available=Mathf.Max(0,route.StopDistance-car.Distance);
                }
                // Shared incoming lane queue, including cars whose chosen turns differ.
                foreach(var other in Cars)
                {
                    if(other==car) continue;
                    if(route.Approach==other.Route.Approach && car.Distance<=route.StopDistance && other.Distance<=other.Route.StopDistance+1 && other.Distance>car.Distance)
                        available=Mathf.Min(available,Mathf.Max(0,other.Distance-car.Distance-5));
                    var delta=other.Position-car.Position;
                    var forward=route.Forward(car.Distance);
                    float ahead=Vector3.Dot(delta,forward);
                    if(ahead>0 && ahead<12 && Vector3.Cross(delta,forward).magnitude<1.6f)
                        available=Mathf.Min(available,Mathf.Max(0,ahead-4.7f));
                }
                float desired=Mathf.Min(Settings.speed,Mathf.Sqrt(2*4*available));
                car.Speed=Mathf.MoveTowards(car.Speed,desired,(desired<car.Speed?6:Settings.acceleration)*dt);
                float travel=Mathf.Min(car.Speed*dt,available);
                if(travel<car.Speed*dt) car.Speed=travel/dt;
                car.Distance+=travel;
                if(car.Speed<.5f) car.Wait+=dt;
                if(Reservation==car.Id && car.Distance>=route.ClearDistance) Reservation=-1;
                if(car.Distance>=route.Length)
                {
                    TotalWait+=car.Wait; Completed++; Cars.RemoveAt(i--);
                }
            }
        }

        void UpdateSignal()
        {
            if(Stage==SignalStage.Green)
            {
                bool empty=Queue[ActiveApproach]==0;
                if(PhaseTime>=GreenDuration || (PhaseTime>=4 && (request || (Adaptive && empty))))
                { Stage=SignalStage.Yellow; PhaseTime=0; request=false; }
            }
            else if(Stage==SignalStage.Yellow && PhaseTime>=Settings.yellowSeconds)
            { Stage=SignalStage.AllRed; PhaseTime=0; }
            else if(Stage==SignalStage.AllRed && PhaseTime>=Settings.clearanceSeconds && Reservation<0)
            {
                lastServed[ActiveApproach]=Elapsed;
                int next=(ActiveApproach+1)%4;
                if(Adaptive)
                {
                    float best=float.NegativeInfinity;
                    for(int offset=1;offset<=3;offset++)
                    {
                        int a=(ActiveApproach+offset)%4;
                        // Age prevents a small queue from being starved by a busy approach.
                        float score=Queue[a]*2+(Elapsed-lastServed[a])*.3f;
                        if(score>best) { best=score; next=a; }
                    }
                }
                ActiveApproach=next; Stage=SignalStage.Green; PhaseTime=0;
            }
        }
    }
}

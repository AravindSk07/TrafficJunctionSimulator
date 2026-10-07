using System;
using UnityEngine;

namespace JunctionLab
{
    public enum Turn { Straight, Left, Right }
    public enum SignalStage { Green, Yellow, AllRed }

    // Distance-parametrized polylines make following gaps independent of corner curvature.
    public sealed class TrafficRoute
    {
        public readonly int Approach;
        public readonly Turn Turn;
        public readonly Vector3[] Points;
        public readonly float[] Distances;
        public readonly float StopDistance;
        public readonly float ClearDistance;
        public float Length => Distances[Distances.Length - 1];

        public TrafficRoute(int approach, Turn turn)
        {
            Approach = approach; Turn = turn;
            Points = new Vector3[35];
            var start = new Vector3(2, 0, -38);
            var entry = new Vector3(2, 0, -9);
            Vector3 end, controlA, controlB, finish;
            if (turn == Turn.Left)
            {
                end = new Vector3(-9, 0, 2); finish = new Vector3(-38, 0, 2);
                controlA = new Vector3(2, 0, 0); controlB = new Vector3(0, 0, 2);
            }
            else if (turn == Turn.Right)
            {
                end = new Vector3(9, 0, -2); finish = new Vector3(38, 0, -2);
                controlA = new Vector3(2, 0, -4); controlB = new Vector3(4, 0, -2);
            }
            else
            {
                end = new Vector3(2, 0, 9); finish = new Vector3(2, 0, 38);
                controlA = new Vector3(2, 0, -3); controlB = new Vector3(2, 0, 3);
            }
            var rotation = Quaternion.Euler(0, approach * 90, 0);
            Points[0] = rotation * start;
            for (int i = 0; i <= 32; i++)
            {
                float t = i / 32f, s = 1-t;
                Points[i+1] = rotation * (s*s*s*entry + 3*s*s*t*controlA + 3*s*t*t*controlB + t*t*t*end);
            }
            Points[34] = rotation * finish;
            Distances = new float[Points.Length];
            for (int i=1; i<Points.Length; i++) Distances[i] = Distances[i-1] + Vector3.Distance(Points[i-1], Points[i]);
            // Stop before the crosswalk; the curve still begins nearer the junction.
            StopDistance = Distances[1] - 4;
            // Retain the reservation until the rear has cleared the conflict box.
            ClearDistance = Distances[33] + 4;
        }

        public Vector3 Position(float distance)
        {
            distance = Mathf.Clamp(distance, 0, Length);
            for (int i=1; i<Points.Length; i++)
                if (distance <= Distances[i])
                    return Vector3.Lerp(Points[i-1], Points[i], (distance-Distances[i-1])/(Distances[i]-Distances[i-1]));
            return Points[Points.Length-1];
        }
        public Vector3 Forward(float distance) => (Position(Mathf.Min(distance + .15f, Length)) - Position(Mathf.Max(0,distance-.15f))).normalized;
    }
}

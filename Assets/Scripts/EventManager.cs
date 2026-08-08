using UnityEngine;
using UnityEngine.Events;

public static class EventManager
{
    public static UnityAction<CarController2D> OnSegmentRestart; 
    public static UnityAction<Segment> OnSegmentCompleted;
}

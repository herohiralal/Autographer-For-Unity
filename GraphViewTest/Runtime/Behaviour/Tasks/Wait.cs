using UnityEngine;

namespace GraphViewTest.Runtime.Behaviour.Tasks
{
    public class Wait : Task
    {
        [SerializeField] private float duration;
        [SerializeField] private float randomDeviation;
    }
}
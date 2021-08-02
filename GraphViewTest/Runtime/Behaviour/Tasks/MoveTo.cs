using UnityEngine;

namespace GraphViewTest.Runtime.Behaviour.Tasks
{
    public class MoveTo : Task
    {
        [SerializeField] private string locationKeyName;
        [SerializeField] private float speed;
        [SerializeField] private float acceptanceRadius;
        [SerializeField] private bool trackTarget;
    }
}
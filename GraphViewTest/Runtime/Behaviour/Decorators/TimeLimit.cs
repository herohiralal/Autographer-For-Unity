using UnityEngine;

namespace GraphViewTest.Runtime.Behaviour.Decorators
{
    public class TimeLimit : ScriptableObject
    {
        [SerializeField] private float failAfter;
    }
}
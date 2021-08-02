using UnityEngine;

namespace GraphViewTest.Runtime.Behaviour.Decorators
{
    public class CheckBlackboardFlag : Decorator
    {
        [SerializeField] private string targetKeyName;
        [SerializeField] private bool value;
    }
}
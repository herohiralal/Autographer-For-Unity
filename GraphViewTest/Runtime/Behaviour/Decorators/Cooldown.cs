using UnityEngine;

namespace GraphViewTest.Runtime.Behaviour.Decorators
{
    public class Cooldown : Decorator
    {
        [SerializeField] private float duration;
    }
}
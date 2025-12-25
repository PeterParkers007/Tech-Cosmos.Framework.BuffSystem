using System;

namespace TechCosmos.GBF.Runtime
{
    public interface IBuff<T> where T : class
    {
        public T target { get; set; }
        public int priority {  get; set; }
        public bool isOver { get; set; }
        public string[] tags { get; set; }
        public void TriggerApplyEvent(T target);
        public void TriggerRemoveEvent(T target);
        public void Apply();
        public void Remove();
        public void Update(float deltaTime);

        public event Action<T> OnApply;
        public event Action<T> OnRemove;
    }
}


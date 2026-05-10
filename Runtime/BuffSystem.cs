// ============================================================
// ÎÄ¼þ£ºBuffSystem.cs
// Â·¾¶£ºTechCosmos.GBF.Runtime/BuffSystem.cs
// ============================================================
using System;
using System.Collections.Generic;

namespace TechCosmos.GBF.Runtime
{
    public class BuffSystem<T> where T : class
    {
        protected T _target;
        protected List<IBuff<T>> buffs = new();

        public event Action<IBuff<T>> OnBuffAdded;
        public event Action<IBuff<T>> OnBuffRemoved;
        public T Target => _target;
        public int BuffCount => buffs.Count;
        public BuffSystem(T target) => _target = target;

        public void BuffUpdate(float deltaTime)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var buff = buffs[i];
                buff.Update(deltaTime);
                if (buff.isOver) RemoveBuff(buff);
            }
        }

        public void SortBuffs() => buffs.Sort((a, b) => a.priority.CompareTo(b.priority));

        public void AddBuff(IBuff<T> buff)
        {
            if (buff == null) return;
            var existing = FindBuffByName(buff.BuffName);
            if (existing != null)
            {
                switch (existing.StackPolicy)
                {
                    case BuffStackPolicy.ExtendDuration:
                        existing.Refresh(); OnBuffAdded?.Invoke(existing); return;
                    case BuffStackPolicy.StackAndRefresh:
                        if (existing.CurrentStacks < existing.MaxStacks) existing.CurrentStacks++;
                        existing.Refresh(); OnBuffAdded?.Invoke(existing); return;
                    case BuffStackPolicy.Independent: break;
                    case BuffStackPolicy.Replace: RemoveBuff(existing); break;
                }
            }
            buff.target = _target;
            buff.TriggerApplyEvent(buff.target);
            buffs.Add(buff);
            SortBuffs();
            OnBuffAdded?.Invoke(buff);
        }

        public void AddBuff(params IBuff<T>[] buffs) { for (int i = 0; i < buffs.Length; i++) AddBuff(buffs[i]); }
        public void RemoveBuff(IBuff<T> buff)
        {
            if (buff == null) return;
            buff.isOver = true;
            buffs.Remove(buff);
            OnBuffRemoved?.Invoke(buff);
        }
        public void ManualRemoveBuff(IBuff<T> buff) { if (buff != null) buff.isOver = true; }
        public void ClearBuff() => buffs.Clear();

        public void DispelByTags(params string[] tags) { for (int i = buffs.Count - 1; i >= 0; i--) { if (CheckBuffHasAnyTag(buffs[i], tags)) ManualRemoveBuff(buffs[i]); } }
        public void RemoveBuffsByName(string buffName) { for (int i = buffs.Count - 1; i >= 0; i--) { if (buffs[i].BuffName == buffName) ManualRemoveBuff(buffs[i]); } }

        public float GetModifiedValue(string modifyType, float baseValue, BuffModifyContext<T> context = null)
        {
            float result = baseValue;
            var ctx = context ?? new BuffModifyContext<T> { target = _target };
            for (int i = 0; i < buffs.Count; i++)
                if (!buffs[i].isOver) result = buffs[i].ModifyValue(modifyType, result, ctx);
            return result;
        }

        public void DispatchAction(string actionName, T unit = null, float value = default, string damageType = default)
        {
            for (int i = 0; i < buffs.Count; i++)
                if (!buffs[i].isOver) buffs[i].OnAction(actionName, unit, value, damageType);
        }

        public void RemoveBuffsByAnyTag(params string[] tags) { for (int i = buffs.Count - 1; i >= 0; i--) { if (CheckBuffHasAnyTag(buffs[i], tags)) ManualRemoveBuff(buffs[i]); } }
        public void RemoveBuffsByAllTags(params string[] tags) { for (int i = buffs.Count - 1; i >= 0; i--) { if (CheckBuffHasAllTags(buffs[i], tags)) ManualRemoveBuff(buffs[i]); } }

        public bool HasAnyBuff(params string[] tags) { for (int i = 0; i < buffs.Count; i++) { if (CheckBuffHasAnyTag(buffs[i], tags)) return true; } return false; }
        public bool HasAllBuff(params string[] tags) { for (int i = 0; i < buffs.Count; i++) { if (CheckBuffHasAllTags(buffs[i], tags)) return true; } return false; }

        public IBuff<T> FindBuffByAnyTag(params string[] tags) { for (int i = 0; i < buffs.Count; i++) { if (CheckBuffHasAnyTag(buffs[i], tags)) return buffs[i]; } return null; }
        public IBuff<T> FindBuffByAllTags(params string[] tags) { for (int i = 0; i < buffs.Count; i++) { if (CheckBuffHasAllTags(buffs[i], tags)) return buffs[i]; } return null; }

        public List<IBuff<T>> FindAllBuffsByAnyTag(params string[] tags) { var r = new List<IBuff<T>>(); for (int i = 0; i < buffs.Count; i++) { if (CheckBuffHasAnyTag(buffs[i], tags)) r.Add(buffs[i]); } return r; }
        public List<IBuff<T>> FindAllBuffsByAllTags(params string[] tags) { var r = new List<IBuff<T>>(); for (int i = 0; i < buffs.Count; i++) { if (CheckBuffHasAllTags(buffs[i], tags)) r.Add(buffs[i]); } return r; }

        public IBuff<T> FindBuffByName(string buffName) { for (int i = 0; i < buffs.Count; i++) { if (buffs[i].BuffName == buffName) return buffs[i]; } return null; }

        private bool CheckBuffHasAnyTag(IBuff<T> buff, params string[] searchTags)
        {
            var bt = buff.tags; if (bt == null) return false;
            for (int i = 0; i < bt.Length; i++) for (int j = 0; j < searchTags.Length; j++) if (bt[i] == searchTags[j]) return true;
            return false;
        }

        private bool CheckBuffHasAllTags(IBuff<T> buff, params string[] searchTags)
        {
            var bt = buff.tags; if (bt == null) return false;
            for (int i = 0; i < searchTags.Length; i++) { bool f = false; for (int j = 0; j < bt.Length; j++) { if (bt[j] == searchTags[i]) { f = true; break; } } if (!f) return false; }
            return true;
        }
    }
}
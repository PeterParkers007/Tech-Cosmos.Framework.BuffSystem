// ============================================================
// 文件：BuffSystem.cs
// 路径：TechCosmos.GBF.Runtime/BuffSystem.cs
// ============================================================
using System;
using System.Collections.Generic;

namespace TechCosmos.GBF.Runtime
{
    public class BuffSystem<T> where T : class
    {
        protected T _target;
        protected List<IBuff<T>> buffs = new();

        // 新增：待添加 Buff 缓冲队列，避免遍历时修改集合
        private List<IBuff<T>> _pendingAddBuffs = new();
        // 新增：标记是否正在遍历中
        private bool _isUpdating = false;

        public event Action<IBuff<T>> OnBuffAdded;
        public event Action<IBuff<T>> OnBuffRemoved;
        public T Target => _target;
        public int BuffCount => buffs.Count + _pendingAddBuffs.Count;
        public BuffSystem(T target) => _target = target;

        public void BuffUpdate(float deltaTime)
        {
            _isUpdating = true;

            try
            {
                // 1. 先处理等待队列中的新增 Buff
                FlushPendingBuffs();

                // 2. 倒序遍历更新（删除安全）
                for (int i = buffs.Count - 1; i >= 0; i--)
                {
                    var buff = buffs[i];
                    buff.Update(deltaTime);
                    if (buff.isOver) RemoveBuff(buff);
                }
            }
            finally
            {
                _isUpdating = false;
                // 3. 遍历结束后，再次处理可能由 Buff 效果触发的新增
                FlushPendingBuffs();
            }
        }

        /// <summary>
        /// 将待添加队列中的 Buff 正式加入主列表
        /// </summary>
        private void FlushPendingBuffs()
        {
            if (_pendingAddBuffs.Count == 0) return;

            foreach (var buff in _pendingAddBuffs)
            {
                // 再次检查是否有同名 Buff（可能在等待期间已经被添加）
                var existing = FindBuffByName(buff.BuffName);
                if (existing != null)
                {
                    // 如果主列表中已存在，应用堆叠逻辑
                    ApplyStackPolicy(existing, buff);
                }
                else
                {
                    buffs.Add(buff);
                    OnBuffAdded?.Invoke(buff);
                }
            }

            _pendingAddBuffs.Clear();

            if (buffs.Count > 0)
                SortBuffs();
        }

        public void SortBuffs() => buffs.Sort((a, b) => a.priority.CompareTo(b.priority));

        public void AddBuff(IBuff<T> buff)
        {
            if (buff == null) return;

            // 先检查主列表中是否已存在同名 Buff
            var existing = FindBuffByName(buff.BuffName);
            if (existing != null)
            {
                switch (existing.StackPolicy)
                {
                    case BuffStackPolicy.ExtendDuration:
                        existing.Refresh();
                        OnBuffAdded?.Invoke(existing);
                        return;

                    case BuffStackPolicy.StackAndRefresh:
                        if (existing.CurrentStacks < existing.MaxStacks)
                            existing.CurrentStacks++;
                        existing.Refresh();
                        OnBuffAdded?.Invoke(existing);
                        return;

                    case BuffStackPolicy.Independent:
                        break;

                    case BuffStackPolicy.Replace:
                        RemoveBuff(existing);
                        break;
                }
            }

            // 如果正在遍历中，放入缓冲队列；否则直接添加
            if (_isUpdating)
            {
                _pendingAddBuffs.Add(buff);
            }
            else
            {
                // 也检查缓冲队列中是否有同名
                var pendingExisting = _pendingAddBuffs.Find(b => b.BuffName == buff.BuffName);
                if (pendingExisting != null)
                {
                    _pendingAddBuffs.Remove(pendingExisting);
                }

                buff.target = _target;
                buff.TriggerApplyEvent(buff.target);
                buffs.Add(buff);
                SortBuffs();
                OnBuffAdded?.Invoke(buff);
            }
        }

        public void AddBuff(params IBuff<T>[] buffs)
        {
            for (int i = 0; i < buffs.Length; i++)
                AddBuff(buffs[i]);
        }

        public void RemoveBuff(IBuff<T> buff)
        {
            if (buff == null) return;
            buff.isOver = true;
            buffs.Remove(buff);
            OnBuffRemoved?.Invoke(buff);
        }

        public void ManualRemoveBuff(IBuff<T> buff)
        {
            if (buff != null) buff.isOver = true;
        }

        public void ClearBuff() => buffs.Clear();

        public void DispelByTags(params string[] tags)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (CheckBuffHasAnyTag(buffs[i], tags))
                    ManualRemoveBuff(buffs[i]);
            }
        }

        public void RemoveBuffsByName(string buffName)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].BuffName == buffName)
                    ManualRemoveBuff(buffs[i]);
            }
        }

        public float GetModifiedValue(string modifyType, float baseValue, BuffModifyContext<T> context = null)
        {
            float result = baseValue;
            var ctx = context ?? new BuffModifyContext<T> { target = _target };
            for (int i = 0; i < buffs.Count; i++)
                if (!buffs[i].isOver)
                    result = buffs[i].ModifyValue(modifyType, result, ctx);
            return result;
        }

        public void DispatchAction(string actionName, T unit = null, float value = default, string damageType = default)
        {
            for (int i = 0; i < buffs.Count; i++)
                if (!buffs[i].isOver)
                    buffs[i].OnAction(actionName, unit, value, damageType);
        }

        public void RemoveBuffsByAnyTag(params string[] tags)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (CheckBuffHasAnyTag(buffs[i], tags))
                    ManualRemoveBuff(buffs[i]);
            }
        }

        public void RemoveBuffsByAllTags(params string[] tags)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (CheckBuffHasAllTags(buffs[i], tags))
                    ManualRemoveBuff(buffs[i]);
            }
        }

        public bool HasAnyBuff(params string[] tags)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                if (CheckBuffHasAnyTag(buffs[i], tags)) return true;
            }
            return false;
        }

        public bool HasAllBuff(params string[] tags)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                if (CheckBuffHasAllTags(buffs[i], tags)) return true;
            }
            return false;
        }

        public IBuff<T> FindBuffByAnyTag(params string[] tags)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                if (CheckBuffHasAnyTag(buffs[i], tags)) return buffs[i];
            }
            return null;
        }

        public IBuff<T> FindBuffByAllTags(params string[] tags)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                if (CheckBuffHasAllTags(buffs[i], tags)) return buffs[i];
            }
            return null;
        }

        public List<IBuff<T>> FindAllBuffsByAnyTag(params string[] tags)
        {
            var r = new List<IBuff<T>>();
            for (int i = 0; i < buffs.Count; i++)
            {
                if (CheckBuffHasAnyTag(buffs[i], tags)) r.Add(buffs[i]);
            }
            return r;
        }

        public List<IBuff<T>> FindAllBuffsByAllTags(params string[] tags)
        {
            var r = new List<IBuff<T>>();
            for (int i = 0; i < buffs.Count; i++)
            {
                if (CheckBuffHasAllTags(buffs[i], tags)) r.Add(buffs[i]);
            }
            return r;
        }

        public IBuff<T> FindBuffByName(string buffName)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                if (buffs[i].BuffName == buffName) return buffs[i];
            }
            return null;
        }

        /// <summary>
        /// 对已存在的 Buff 应用堆叠策略
        /// </summary>
        private void ApplyStackPolicy(IBuff<T> existing, IBuff<T> newBuff)
        {
            switch (existing.StackPolicy)
            {
                case BuffStackPolicy.ExtendDuration:
                    existing.Refresh();
                    break;

                case BuffStackPolicy.StackAndRefresh:
                    if (existing.CurrentStacks < existing.MaxStacks)
                        existing.CurrentStacks++;
                    existing.Refresh();
                    break;

                case BuffStackPolicy.Independent:
                    // 独立叠加，直接当成新 Buff 添加
                    buffs.Add(newBuff);
                    break;

                case BuffStackPolicy.Replace:
                    RemoveBuff(existing);
                    buffs.Add(newBuff);
                    break;
            }
        }

        private bool CheckBuffHasAnyTag(IBuff<T> buff, params string[] searchTags)
        {
            var bt = buff.tags;
            if (bt == null) return false;
            for (int i = 0; i < bt.Length; i++)
                for (int j = 0; j < searchTags.Length; j++)
                    if (bt[i] == searchTags[j]) return true;
            return false;
        }

        private bool CheckBuffHasAllTags(IBuff<T> buff, params string[] searchTags)
        {
            var bt = buff.tags;
            if (bt == null) return false;
            for (int i = 0; i < searchTags.Length; i++)
            {
                bool f = false;
                for (int j = 0; j < bt.Length; j++)
                {
                    if (bt[j] == searchTags[i]) { f = true; break; }
                }
                if (!f) return false;
            }
            return true;
        }
    }
}
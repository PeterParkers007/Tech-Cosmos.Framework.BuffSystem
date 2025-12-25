using System;
using System.Collections.Generic;
using System.Linq;
namespace TechCosmos.GBF.Runtime
{
    public class BuffSystem<T> where T : class
    {
        protected T _target;
        protected List<IBuff<T>> buffs = new();

        public event Action<IBuff<T>> OnBuffAdded;
        public event Action<IBuff<T>> OnBuffRemoved;
        public event Action<T> OnBuffsCleared;

        public BuffSystem(T target) => _target = target;
        public void BuffUpdate(float deltaTime)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var buff = buffs[i];
                buff.Update(deltaTime);
                if (buff.isOver)
                {
                    RemoveBuff(buff);
                }
            }
        }
        public void SortBuffs() => buffs.Sort((a, b) => a.priority.CompareTo(b.priority));
        public void AddBuff(IBuff<T> buff)
        {
            buff.target = _target;
            buff.TriggerApplyEvent(buff.target);
            buffs.Add(buff);
            SortBuffs();
            OnBuffAdded?.Invoke(buff);
        }
        public void AddBuff(params IBuff<T>[] buffs)
        {
            foreach (var buff in buffs)
            {
                buff.target = _target;
                AddBuff(buff);
            }
        }
        public void RemoveBuff(IBuff<T> buff)
        {
            buffs.Remove(buff);
            OnBuffRemoved?.Invoke(buff);
        }
        public void ManualRemoveBuff(IBuff<T> buff) => buff.isOver = true;
        public void ClearBuff()
        {
            buffs.Clear();
            OnBuffsCleared?.Invoke(_target);
        }
        /// <summary>
        /// 移除包含任意一个指定标签的所有buff
        /// </summary>
        public void RemoveBuffsByAnyTag(params string[] tags)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var buff = buffs[i];
                if (CheckBuffHasAnyTag(buff, tags))
                {
                    ManualRemoveBuff(buff);
                }
            }
        }

        /// <summary>
        /// 移除包含所有指定标签的所有buff
        /// </summary>
        public void RemoveBuffsByAllTags(params string[] tags)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var buff = buffs[i];
                if (CheckBuffHasAllTags(buff, tags))
                {
                    ManualRemoveBuff(buff);
                }
            }
        }

        // 辅助方法
        private bool CheckBuffHasAnyTag(IBuff<T> buff, params string[] searchTags)
        {
            foreach (var buffTag in buff.tags)
            {
                foreach (var searchTag in searchTags)
                {
                    if (buffTag == searchTag)
                        return true;
                }
            }
            return false;
        }

        private bool CheckBuffHasAllTags(IBuff<T> buff, params string[] searchTags)
        {
            foreach (var searchTag in searchTags)
            {
                bool tagFound = false;
                foreach (var buffTag in buff.tags)
                {
                    if (buffTag == searchTag)
                    {
                        tagFound = true;
                        break;
                    }
                }
                if (!tagFound) return false;
            }
            return true;
        }

        // 查询方法也应该有两个版本
        public IBuff<T> FindBuffByAnyTag(params string[] tags)
        {
            foreach (var buff in buffs)
            {
                if (CheckBuffHasAnyTag(buff, tags))
                    return buff;
            }
            return null;
        }

        public IBuff<T> FindBuffByAllTags(params string[] tags)
        {
            foreach (var buff in buffs)
            {
                if (CheckBuffHasAllTags(buff, tags))
                    return buff;
            }
            return null;
        }

        // 批量查询版本
        public List<IBuff<T>> FindAllBuffsByAnyTag(params string[] tags)
        {
            List<IBuff<T>> result = new List<IBuff<T>>();
            foreach (var buff in buffs)
            {
                if (CheckBuffHasAnyTag(buff, tags))
                    result.Add(buff);
            }
            return result;
        }

        public List<IBuff<T>> FindAllBuffsByAllTags(params string[] tags)
        {
            List<IBuff<T>> result = new List<IBuff<T>>();
            foreach (var buff in buffs)
            {
                if (CheckBuffHasAllTags(buff, tags))
                    result.Add(buff);
            }
            return result;
        }
        /// <summary>
        /// 检查是否存在任何一个buff包含任意一个指定的标签
        /// </summary>
        public bool HasAnyBuff(params string[] buffTags)
        {
            foreach (var buff in buffs)
            {
                foreach (var buffTag in buff.tags)
                {
                    foreach (var searchTag in buffTags)
                    {
                        if (buffTag == searchTag)
                            return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 检查是否存在任何一个buff包含所有指定的标签
        /// </summary>
        public bool HasAllBuff(params string[] buffTags)
        {
            foreach (var buff in buffs)
            {
                bool hasAllTags = true;

                foreach (var searchTag in buffTags)
                {
                    bool tagFound = false;

                    foreach (var buffTag in buff.tags)
                    {
                        if (buffTag == searchTag)
                        {
                            tagFound = true;
                            break;
                        }
                    }

                    if (!tagFound)
                    {
                        hasAllTags = false;
                        break;
                    }
                }

                if (hasAllTags) return true;
            }

            return false;
        }
    }
}


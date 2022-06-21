using System.Collections;
using System.Collections.Generic;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Maintain occurances of individually added ReferenceInfos
    /// </summary>
    public class ReferenceCollection : IList<ReferenceInfo>
    {
        public int Count => list.Count;
        public bool IsReadOnly => false;

        public ReferenceInfo this[int index] { get => list[index]; set => list[index] = value; }

        private List<ReferenceInfo> list = new();


        /// <summary>
        /// Stack occurances of the added item
        /// </summary>
        /// <param name="item"></param>
        public void Add(ReferenceInfo item)
        {
            if (item.Occurances == 0) item.Occurances++;
            ReferenceInfo? matchedItem = list.Find(i =>
                i.ReferencedMember.ToString().Equals(item.ReferencedMember.ToString()) &&
                i.ReferencingMember.ToString().Equals(item.ReferencingMember.ToString()));

            if (matchedItem is null)
                list.Add(item);
            else
                matchedItem.Occurances += item.Occurances;
        }
        public void Clear() => list.Clear();
        public bool Contains(ReferenceInfo item)
        {
            return list.Exists(i =>
                i.ReferencedMember.ToString().Equals(item.ReferencedMember.ToString()) &&
                i.ReferencingMember.ToString().Equals(item.ReferencingMember.ToString()));
        }
        public void CopyTo(ReferenceInfo[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
        /// <summary>
        /// Remove item regardless of occurances
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if the item was successfully deleted</returns>
        public bool Delete(ReferenceInfo item)
        {
            ReferenceInfo? matchedItem = list.Find(i =>
                i.ReferencedMember.ToString().Equals(item.ReferencedMember.ToString()) &&
                i.ReferencingMember.ToString().Equals(item.ReferencingMember.ToString()));
            return matchedItem is not null && list.Remove(matchedItem);
        }
        public IEnumerator<ReferenceInfo> GetEnumerator() => list.GetEnumerator();
        public int IndexOf(ReferenceInfo item)
        {
            ReferenceInfo matchedItem = list.Find(i =>
                i.ReferencedMember.ToString().Equals(item.ReferencedMember.ToString()) &&
                i.ReferencingMember.ToString().Equals(item.ReferencingMember.ToString()));

            return matchedItem is null ? -1 : list.IndexOf(matchedItem);
        }
        public void Insert(int index, ReferenceInfo item) => throw new System.NotImplementedException();
        /// <summary>
        /// Unstack occurances of the removed item
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Return true if the item still maintains occurances</returns>
        public bool Remove(ReferenceInfo item)
        {
            if (item.Occurances == 0) item.Occurances++;
            ReferenceInfo? matchedItem = list.Find(i =>
                i.ReferencedMember.ToString().Equals(item.ReferencedMember.ToString()) &&
                i.ReferencingMember.ToString().Equals(item.ReferencingMember.ToString()));

            if (matchedItem is not null)
            {
                matchedItem.Occurances -= item.Occurances;
                if (matchedItem.Occurances == 0) list.Remove(matchedItem);
            }

            return matchedItem is not null && list.Contains(matchedItem);
        }
        public void RemoveAt(int index) => throw new System.NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
    }
}

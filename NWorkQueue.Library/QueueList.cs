using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NWorkQueue.Library
{
    internal class QueueList
    {
        private SortedList<string, int> _sortedList = new SortedList<string, int>();

        private Object SortLock => (_sortedList as ICollection).SyncRoot;


        public void Reload(SortedList<string, int> loadQueueList)
        {
            lock (SortLock)
            {
                _sortedList.Clear();
                _sortedList = new SortedList<string, int>(loadQueueList);
            }
        }

        internal bool ContainsKey(string fixedName)
        {
            lock (SortLock)
            {
                return _sortedList.ContainsKey(fixedName);
            }
        }

        public void Add(string fixedName, int queueId)
        {
            lock (SortLock)
            {
                _sortedList.Add(fixedName, queueId);
            }
        }

        public bool TryGetValue(string fixedName, out int id)
        {
            lock (SortLock)
            {
                return _sortedList.TryGetValue(fixedName, out id);
            }
        }

        public void Delete(string fixedName)
        {
            lock (SortLock)
            {
                _sortedList.Remove(fixedName);
            }
        }
    }
}

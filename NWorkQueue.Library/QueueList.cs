using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NWorkQueue.Library
{
    internal class QueueList
    {
        private SortedList<string, WorkQueueModel> _sortedList;
        
        private Object SortLock => (_sortedList as ICollection).SyncRoot;

        public QueueList()
        {
            _sortedList = SortedListFactory();
        }

        public void Reload(SortedList<string, WorkQueueModel> loadQueueList)
        {
            lock (SortLock)
            {
                _sortedList.Clear();
                _sortedList = SortedListFactory(loadQueueList);
            }
        }

        internal SortedList<string, WorkQueueModel> SortedListFactory(SortedList<string, WorkQueueModel> list = null)
        {
            if (list == null)
                return new SortedList<string, WorkQueueModel>(StringComparer.CurrentCultureIgnoreCase);
            else
                return new SortedList<string, WorkQueueModel>(list, StringComparer.CurrentCultureIgnoreCase);
        }

        internal bool ContainsKey(string fixedName)
        {
            lock (SortLock)
            {
                return _sortedList.ContainsKey(fixedName);
            }
        }

        public void Add(WorkQueueModel workQueueModel)
        {
            lock (SortLock)
            {
                _sortedList.Add(workQueueModel.Name, workQueueModel);
            }
        }

        public bool TryGetQueueId(string fixedName, out Int64 id)
        {
            if (TryGetQueue(fixedName, out WorkQueueModel workQueueModel))
            {
                id = workQueueModel.Id;
                return true;
            }
            id = 0;
            return false;
        }

        public bool TryGetQueue(string fixedName, out WorkQueueModel workQueueModel)
        {
            lock (SortLock)
            {
                return _sortedList.TryGetValue(fixedName, out workQueueModel);
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

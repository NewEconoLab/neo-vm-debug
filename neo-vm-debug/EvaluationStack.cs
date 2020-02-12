using Neo.VM.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public sealed class EvaluationStack : IReadOnlyCollection<StackItem>
    {
        public enum OpType
        {
            Non,
            Clear,
            Insert,
            Peek,
            Pop,
            Push,
            Remove,
            Reserve,
            Set,
        }

        public struct Op
        {
            public Op(OpType type, int ind = -1)
            {
                this.type = type;
                this.ind = ind;
            }
            public OpType type;
            public int ind;
        }

        public List<Op> record = new List<Op>();

        public void ClearRecord()
        {
            record.Clear();
        }

        public OpType GetLastRecordType()
        {
            if (record.Count == 0)
                return OpType.Non;
            else
                return record.Last().type;
        }

        private readonly List<StackItem> innerList = new List<StackItem>();
        private readonly ReferenceCounter referenceCounter;

        internal EvaluationStack(ReferenceCounter referenceCounter)
        {
            this.referenceCounter = referenceCounter;
        }

        public int Count => innerList.Count;

        internal void Clear()
        {
            record.Add(new Op(OpType.Clear));
            foreach (StackItem item in innerList)
                referenceCounter.RemoveStackReference(item);
            innerList.Clear();
        }

        internal void CopyTo(EvaluationStack stack, int count = -1)
        {
            if (count == 0) return;
            if (count == -1)
                stack.innerList.AddRange(innerList);
            else
                stack.innerList.AddRange(innerList.Skip(innerList.Count - count));
        }

        public IEnumerator<StackItem> GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Insert(int index, StackItem item)
        {
            record.Add(new Op(OpType.Insert, index));
            if (index > innerList.Count) throw new InvalidOperationException();
            innerList.Insert(innerList.Count - index, item);
            referenceCounter.AddStackReference(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Peek(int index = 0)
        {
            record.Add(new Op(OpType.Peek, index));
            if (index >= innerList.Count) throw new InvalidOperationException();
            if (index < 0)
            {
                index += innerList.Count;
                if (index < 0) throw new InvalidOperationException();
            }
            return innerList[innerList.Count - index - 1];
        }

        public StackItem PeekWithoutLog(int index = 0)
        {
            return this.Peek(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Pop()
        {
            record.Add(new Op(OpType.Pop));
            if (!TryPop(out StackItem item))
                throw new InvalidOperationException();
            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(StackItem item)
        {
            record.Add(new Op(OpType.Push));
            innerList.Add(item);
            referenceCounter.AddStackReference(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Reverse(int n)
        {
            record.Add(new Op(OpType.Reserve, n));
            if (n < 0 || n > innerList.Count) return false;
            if (n <= 1) return true;
            innerList.Reverse(innerList.Count - n, n);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop<T>(out T item) where T : StackItem
        {
            return TryRemove(0, out item);
        }

        internal bool TryRemove<T>(int index, out T item) where T : StackItem
        {
            record.Add(new Op(OpType.Remove, index));
            if (index >= innerList.Count)
            {
                item = default;
                return false;
            }
            if (index < 0)
            {
                index += innerList.Count;
                if (index < 0)
                {
                    item = default;
                    return false;
                }
            }
            index = innerList.Count - index - 1;
            item = innerList[index] as T;
            if (item is null) return false;
            innerList.RemoveAt(index);
            referenceCounter.RemoveStackReference(item);
            return true;
        }
    }
}

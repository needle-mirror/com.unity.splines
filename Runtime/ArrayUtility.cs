using System;
using System.Collections.Generic;

namespace UnityEngine.Splines
{
    static class ArrayUtility
    {
        public static void RemoveAt<T>(ref T[] array, int index)
        {
            if (index < 0 || index >= array.Length)
                throw new IndexOutOfRangeException();

            Array.Copy(array, index + 1, array, index, array.Length - index - 1);
            Array.Resize(ref array, array.Length - 1);
        }

        public static void RemoveAt<T>(ref T[] array, IEnumerable<int> indices)
        {
            List<int> sorted = new List<int>(indices);
            sorted.Sort();
            SortedRemoveAt(ref array, sorted);
        }

        public static void SortedRemoveAt<T>(ref T[] array, IList<int> sorted)
        {
            int indexeSortedCount = sorted.Count;
            int len = array.Length;

            T[] newArray = new T[len - indexeSortedCount];
            int n = 0;

            for (int i = 0; i < len; i++)
            {
                if (n < indexeSortedCount && sorted[n] == i)
                {
                    // handle duplicate indexes
                    while (n < indexeSortedCount && sorted[n] == i)
                        n++;

                    continue;
                }

                newArray[i - n] = array[i];
            }

            array = newArray;
        }

        public static void Remove<T>(ref T[] array, T element)
        {
            var index = Array.IndexOf(array, element);
            if (index >= 0)
                RemoveAt(ref array, index);
        }

        public static void Add<T>(ref T[] array, T element)
        {
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = element;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace LoginServer
{
    /// <summary>
    /// Multi-Key Dictionary Class
    /// </summary>	
    /// <typeparam name="K">Primary Key Type</typeparam>
    /// <typeparam name="L">Sub Key Type</typeparam>
    /// <typeparam name="V">Value Type</typeparam>
    class MultiKeyDictionary <V>
    {
        internal readonly Dictionary<int, V> baseDictionary = new Dictionary<int, V>();
        internal readonly Dictionary<int, int> subDictionary = new Dictionary<int, int>();
        internal readonly Dictionary<int, int> primaryToSubkeyMapping = new Dictionary<int, int>();

        ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();

        public bool Associate(int subKey, int primaryKey)
        {
            bool fState = false;
            readerWriterLock.EnterUpgradeableReadLock();
            try
            {
                if (!baseDictionary.ContainsKey(primaryKey))
                {
                    throw new KeyNotFoundException(string.Format("MultiKeyDictionary::Associate The base dictionary does not contain the key '{0}'", primaryKey));
                }
                if (primaryToSubkeyMapping.ContainsKey(primaryKey)) // Remove the old mapping first
                {
                    throw new ArgumentException(string.Format("MultiKeyDictionary::Associate The primaryToSubKeyMapping dictionary already contain the key '{0}'", primaryKey));
                }
                if (subDictionary.ContainsKey(subKey))
                {
                    throw new ArgumentException(string.Format("MultiKeyDictionary::Associate The subDictionary dictionary already contain the key '{0}'", subKey));
                }
                subDictionary[subKey] = primaryKey;
                primaryToSubkeyMapping[primaryKey] = subKey;
                fState = true;
            }
            catch (ArgumentException e)
            {
                Output.WriteLine("MultiKeyDictionary::Associate Exception catched [ArgumentException]");
                fState = false;
            }
            catch (KeyNotFoundException e)
            {
                Output.WriteLine("MultiKeyDictionary::Associate Exception catched [KeyNotFoundException]");
                fState = false;
            }
            finally
            {
                readerWriterLock.ExitUpgradeableReadLock();
            }
            return fState;
        }

        public bool TryGetValue(int key, out V val)
        {
            val = default(V);
            readerWriterLock.EnterReadLock();
            try
            {
                return baseDictionary.TryGetValue(key, out val);
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
            //return false;
        }

        public bool TryGetValue(int primaryKey, int subKey, out V val)
        {
            val = default(V);
            readerWriterLock.EnterReadLock();
            try
            {
                if (primaryToSubkeyMapping.ContainsKey(primaryKey))
                {
                    if (primaryToSubkeyMapping[primaryKey] == subKey)
                    {
                        if (subDictionary.ContainsKey(primaryToSubkeyMapping[primaryKey]))
                        {
                            return baseDictionary.TryGetValue(primaryKey, out val);
                        }
                    }
                }
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
            return false;
        }

        public bool ContainsKey(int primaryKey)
        {
            V val;
            return TryGetValue(primaryKey, out val);
        }

        public void Remove(int primaryKey)
        {
            readerWriterLock.EnterWriteLock();
            try
            {
                if (primaryToSubkeyMapping.ContainsKey(primaryKey))
                {
                    if (subDictionary.ContainsKey(primaryToSubkeyMapping[primaryKey]))
                    {
                        subDictionary.Remove(primaryToSubkeyMapping[primaryKey]);
                    }

                    primaryToSubkeyMapping.Remove(primaryKey);
                }

                baseDictionary.Remove(primaryKey);
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public void Remove(int primaryKey, int subKey)
        {
            readerWriterLock.EnterWriteLock();
            try
            {
                if (primaryToSubkeyMapping.ContainsKey(primaryKey))
                {
                    if (primaryToSubkeyMapping[primaryKey] == subKey)
                    {
                        if (subDictionary.ContainsKey(primaryToSubkeyMapping[primaryKey]))
                        {
                            subDictionary.Remove(primaryToSubkeyMapping[primaryKey]);
                            primaryToSubkeyMapping.Remove(primaryKey);
                            baseDictionary.Remove(primaryKey);
                        }
                    }
                }
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public void Remove(int primaryKey, out V val)
        {
            val = default(V);
            TryGetValue(primaryKey, out val);
            readerWriterLock.EnterWriteLock();
            try
            {
                if (primaryToSubkeyMapping.ContainsKey(primaryKey))
                {
                    if (subDictionary.ContainsKey(primaryToSubkeyMapping[primaryKey]))
                    {
                        subDictionary.Remove(primaryToSubkeyMapping[primaryKey]);
                    }
                    primaryToSubkeyMapping.Remove(primaryKey);
                }
                baseDictionary.Remove(primaryKey);
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public bool Add(int primaryKey, V val)
        {
            bool finalState = false;
            readerWriterLock.EnterWriteLock();
            try
            {
                baseDictionary.Add(primaryKey, val);
                finalState = true;
            }
            catch(ArgumentException e)
            {
                Output.WriteLine("MultiKeyDictionary::Add - exception catched [ArgumentException]");
            }
            catch(KeyNotFoundException e)
            {
                Output.WriteLine("MultiKeyDictionary::Add - exception catched [KeyNotFoundException]");
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
            return finalState;
        }

        public bool Add(int primaryKey, int subKey, V val)
        {
            bool fState;
            fState = Add(primaryKey, val);
            if (fState)
            {
                if (!Associate(subKey, primaryKey)) fState = false;
            }
            return fState;
        }


        public List<V> Values
        {
            get
            {
                readerWriterLock.EnterReadLock();
                try
                {
                    return baseDictionary.Values.ToList();
                }
                finally
                {
                    readerWriterLock.ExitReadLock();
                }
            }
        }

        public void Clear()
        {
            readerWriterLock.EnterWriteLock();
            try
            {
                baseDictionary.Clear();
                subDictionary.Clear();
                primaryToSubkeyMapping.Clear();
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public int Count
        {
            get
            {
                readerWriterLock.EnterReadLock();
                try
                {
                    return baseDictionary.Count;
                }
                finally
                {
                    readerWriterLock.ExitReadLock();
                }
            }
        }

        public IEnumerator<KeyValuePair<int, V>> GetEnumerator()
        {
            readerWriterLock.EnterReadLock();
            try
            {
                return baseDictionary.GetEnumerator();
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
        }
    }

}

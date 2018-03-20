using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace LoginServer
{
    class ConnectionsPool
    {
        Object locker;
        // Pool of reusable SocketAsyncEventArgs objects.        
        Queue<Connection> pool;

        // initializes the object pool to the specified size.
        // "capacity" = Maximum number of SocketAsyncEventArgs objects
        internal ConnectionsPool(int capacity)
        {
            this.pool = new Queue<Connection>(capacity);
            this.locker = new object();
        }

        // The number of SocketAsyncEventArgs instances in the pool.         
        internal int Count
        {
            get { return this.pool.Count; }
        }

        // Removes a SocketAsyncEventArgs instance from the pool.
        // returns SocketAsyncEventArgs removed from the pool.
        internal Connection Pop()
        {
            lock (this.pool)
            {
                return this.pool.Dequeue();
            }
        }

        // Add a SocketAsyncEventArg instance to the pool. 
        // "item" = SocketAsyncEventArgs instance to add to the pool.
        internal void Push(Connection item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("ConnectionsPool:: Push\n" + "Items added to a ConnectionsPool cannot be null");
            }
            lock (this.pool)
            {
                this.pool.Enqueue(item);
            }
        }
        //iterate through pool and call toDo() at every object
        internal void Iterate(Func<Connection, bool> toDo)
        {
            lock (this.pool)
            {
                foreach(Connection con in pool)
                {
                    if (!toDo(con)) return; ;
                }
            }
        }

    }
}

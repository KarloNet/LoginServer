using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace LoginServer
{
    class BufferMenager
    {
        byte[] buffer;
        Int32 bufferSize;
        Stack<int> freeIndexPool;
        Int32 currentIndex;
        Int32 bufferBlockSize;

        public BufferMenager(Int32 maxNumberOfConnections, Int32 bufferBlockSize)
        {
            try
            {
                buffer = new byte[maxNumberOfConnections * bufferBlockSize];
                this.currentIndex = 0;
                this.bufferBlockSize = bufferBlockSize;
                this.freeIndexPool = new Stack<int>();
                this.bufferSize = maxNumberOfConnections * bufferBlockSize;
                Output.WriteLine("BufferMenager:: Allocated " + bufferSize.ToString() + " bytes");
            }
            catch (OutOfMemoryException e)
            {
                Output.WriteLine("BufferMenager::Init Memory allocation failed (bytes: " + (maxNumberOfConnections * bufferBlockSize).ToString() +")");
                this.buffer = null;
                this.currentIndex = 0;
                this.bufferBlockSize = bufferBlockSize;
                this.freeIndexPool = new Stack<int>();
                this.bufferSize = 0;
            }
        }

        internal bool SetBuffer(SocketAsyncEventArgs args)
        {

            if (freeIndexPool.Count > 0)
            {
                //This if-statement is only true if you have called the FreeBuffer
                //method previously, which would put an offset for a buffer space 
                //back into this stack.
                args.SetBuffer(buffer, freeIndexPool.Pop(), bufferBlockSize);
            }
            else
            {
                //Inside this else-statement is the code that is used to set the 
                //buffer for each SAEA object when the pool of SAEA objects is built
                //in the Init method.
                if ((bufferSize - bufferBlockSize) < currentIndex)
                {
                    return false;
                }
                args.SetBuffer(buffer, currentIndex, bufferBlockSize);
                currentIndex += bufferBlockSize;
            }
            return true;
        }

        // Removes the buffer from a SocketAsyncEventArg object.   This frees the
        // buffer back to the buffer pool. Try NOT to use the FreeBuffer method,
        // unless you need to destroy the SAEA object, or maybe in the case
        // of some exception handling. Instead, on the server
        // keep the same buffer space assigned to one SAEA object for the duration of
        // this app's running.
        internal void FreeBuffer(SocketAsyncEventArgs args)
        {
            freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }

    }
}

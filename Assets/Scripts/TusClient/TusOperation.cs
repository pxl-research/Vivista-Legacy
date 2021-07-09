using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TusDotNetClient
{
    /// <summary>
    /// A delegate used for reporting progress of a transfer of bytes.
    /// </summary>
    /// <param name="bytesTransferred">The number of bytes transferred so far.</param>
    /// <param name="bytesTotal">The total number of bytes to transfer.</param>
    public delegate void ProgressDelegate(long bytesTransferred, long bytesTotal);

    /// <summary>
    /// Represents an operation against a Tus enabled server. <see cref="TusOperation{T}"/> supports progress reports.
    /// </summary>
    /// <typeparam name="T">The type of the operation result.</typeparam>
    public class TusOperation<T>
    {
        private readonly OperationDelegate _operation;
        private Task<T> _operationTask;

        /// <summary>
        /// Represents an operation which receives a delegate to report transfer progress to.
        /// </summary>
        /// <param name="reportProgress">A delegate which transfer progress can be reported to.</param>
        public delegate Task<T> OperationDelegate(ProgressDelegate reportProgress);

        /// <summary>
        /// Occurs when progress sending the request is made.
        /// </summary>
        public event ProgressDelegate Progressed;

        /// <summary>
        /// Get the asynchronous operation to be performed. This will initiate the operation.
        /// </summary>
        public Task<T> Operation =>
            _operationTask ??
            (_operationTask = _operation((transferred, total) =>
                Progressed?.Invoke(transferred, total)));

        /// <summary>
        /// Create an instance of a <see cref="TusOperation{T}"/>
        /// </summary>
        /// <param name="operation">The operation to perform.</param>
        internal TusOperation(OperationDelegate operation)
        {
            _operation = operation;
        }

        /// <summary>
        /// Gets an awaiter used to initiate and await the operation.
        /// </summary>
        /// <returns>The <see cref="TaskAwaiter{TResult}"/> of the underlying <see cref="Task{TResult}"/>.</returns>
        public TaskAwaiter<T> GetAwaiter() => Operation.GetAwaiter();
    }
}
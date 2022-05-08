using System;

namespace Echo.Core.Compute;

/// <summary>
/// Exception thrown by <see cref="IScheduler"/> during an <see cref="Operation"/> abortion.
/// </summary>
sealed class OperationAbortedException : Exception { }
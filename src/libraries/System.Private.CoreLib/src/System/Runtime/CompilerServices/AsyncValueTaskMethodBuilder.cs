// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Internal.Runtime.CompilerServices;

#if FEATURE_POOLASYNCVALUETASKS
using StateMachineBox = System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.StateMachineBox;
#endif

namespace System.Runtime.CompilerServices
{
    /// <summary>Represents a builder for asynchronous methods that return a <see cref="ValueTask"/>.</summary>
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncValueTaskMethodBuilder
    {
#if FEATURE_POOLASYNCVALUETASKS
        /// <summary>true if we should use reusable boxes for async completions of ValueTask methods; false if we should use tasks.</summary>
        /// <remarks>
        /// We rely on tiered compilation turning this into a const and doing dead code elimination to make checks on this efficient.
        /// It's also required for safety that this value never changes once observed, as Unsafe.As casts are employed based on its value.
        /// </remarks>
        internal static readonly bool s_valueTaskPoolingEnabled = GetPoolAsyncValueTasksSwitch();
        /// <summary>Maximum number of boxes that are allowed to be cached per state machine type.</summary>
        internal static readonly int s_valueTaskPoolingCacheSize = GetPoolAsyncValueTasksLimitValue();
#endif

        /// <summary>Sentinel object used to indicate that the builder completed synchronously and successfully.</summary>
        private static readonly object s_syncSuccessSentinel = AsyncValueTaskMethodBuilder<VoidTaskResult>.s_syncSuccessSentinel;

        /// <summary>The wrapped state machine box or task, based on the value of s_valueTaskPoolingEnabled.</summary>
        /// <remarks>
        /// If the operation completed synchronously and successfully, this will be <see cref="s_syncSuccessSentinel"/>.
        /// </remarks>
        private object? m_task; // Debugger depends on the exact name of this field.

        /// <summary>Creates an instance of the <see cref="AsyncValueTaskMethodBuilder"/> struct.</summary>
        /// <returns>The initialized instance.</returns>
        public static AsyncValueTaskMethodBuilder Create() => default;

        /// <summary>Begins running the builder with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine =>
            AsyncMethodBuilderCore.Start(ref stateMachine);

        /// <summary>Associates the builder with the specified state machine.</summary>
        /// <param name="stateMachine">The state machine instance to associate with the builder.</param>
        public void SetStateMachine(IAsyncStateMachine stateMachine) =>
            AsyncMethodBuilderCore.SetStateMachine(stateMachine, task: null);

        /// <summary>Marks the task as successfully completed.</summary>
        public void SetResult()
        {
            if (m_task is null)
            {
                m_task = s_syncSuccessSentinel;
            }
#if FEATURE_POOLASYNCVALUETASKS
            else if (s_valueTaskPoolingEnabled)
            {
                Unsafe.As<StateMachineBox>(m_task).SetResult(default);
            }
#endif
            else
            {
                AsyncTaskMethodBuilder<VoidTaskResult>.SetExistingTaskResult(Unsafe.As<Task<VoidTaskResult>>(m_task), default);
            }
        }

        /// <summary>Marks the task as failed and binds the specified exception to the task.</summary>
        /// <param name="exception">The exception to bind to the task.</param>
        public void SetException(Exception exception)
        {
#if FEATURE_POOLASYNCVALUETASKS
            if (s_valueTaskPoolingEnabled)
            {
                AsyncValueTaskMethodBuilder<VoidTaskResult>.SetException(exception, ref Unsafe.As<object?, StateMachineBox?>(ref m_task));
            }
            else
#endif
            {
                AsyncTaskMethodBuilder<VoidTaskResult>.SetException(exception, ref Unsafe.As<object?, Task<VoidTaskResult>?>(ref m_task));
            }
        }

        /// <summary>Gets the task for this builder.</summary>
        public ValueTask Task
        {
            get
            {
                if (m_task == s_syncSuccessSentinel)
                {
                    return default;
                }

                // With normal access paterns, m_task should always be non-null here: the async method should have
                // either completed synchronously, in which case SetResult would have set m_task to a non-null object,
                // or it should be completing asynchronously, in which case AwaitUnsafeOnCompleted would have similarly
                // initialized m_task to a state machine object.  However, if the type is used manually (not via
                // compiler-generated code) and accesses Task directly, we force it to be initialized.  Things will then
                // "work" but in a degraded mode, as we don't know the TStateMachine type here, and thus we use a box around
                // the interface instead.

#if FEATURE_POOLASYNCVALUETASKS
                if (s_valueTaskPoolingEnabled)
                {
                    var box = Unsafe.As<StateMachineBox?>(m_task);
                    if (box is null)
                    {
                        m_task = box = AsyncValueTaskMethodBuilder<VoidTaskResult>.CreateWeaklyTypedStateMachineBox();
                    }
                    return new ValueTask(box, box.Version);
                }
                else
#endif
                {
                    var task = Unsafe.As<Task<VoidTaskResult>?>(m_task);
                    if (task is null)
                    {
                        m_task = task = new Task<VoidTaskResult>(); // base task used rather than box to minimize size when used as manual promise
                    }
                    return new ValueTask(task);
                }
            }
        }

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
#if FEATURE_POOLASYNCVALUETASKS
            if (s_valueTaskPoolingEnabled)
            {
                AsyncValueTaskMethodBuilder<VoidTaskResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref Unsafe.As<object?, StateMachineBox?>(ref m_task));
            }
            else
#endif
            {
                AsyncTaskMethodBuilder<VoidTaskResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref Unsafe.As<object?, Task<VoidTaskResult>?>(ref m_task));
            }
        }

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
#if FEATURE_POOLASYNCVALUETASKS
            if (s_valueTaskPoolingEnabled)
            {
                AsyncValueTaskMethodBuilder<VoidTaskResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref Unsafe.As<object?, StateMachineBox?>(ref m_task));
            }
            else
#endif
            {
                AsyncTaskMethodBuilder<VoidTaskResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref Unsafe.As<object?, Task<VoidTaskResult>?>(ref m_task));
            }
        }

        /// <summary>
        /// Gets an object that may be used to uniquely identify this builder to the debugger.
        /// </summary>
        /// <remarks>
        /// This property lazily instantiates the ID in a non-thread-safe manner.
        /// It must only be used by the debugger and tracing purposes, and only in a single-threaded manner
        /// when no other threads are in the middle of accessing this or other members that lazily initialize the box.
        /// </remarks>
        internal object ObjectIdForDebugger
        {
            get
            {
                if (m_task is null)
                {
                    m_task =
#if FEATURE_POOLASYNCVALUETASKS
                        s_valueTaskPoolingEnabled ? (object)
                        AsyncValueTaskMethodBuilder<VoidTaskResult>.CreateWeaklyTypedStateMachineBox() :
#endif
                        AsyncTaskMethodBuilder<VoidTaskResult>.CreateWeaklyTypedStateMachineBox();
                }

                return m_task;
            }
        }

#if FEATURE_POOLASYNCVALUETASKS
        private static bool GetPoolAsyncValueTasksSwitch() =>
            Environment.GetEnvironmentVariable("DOTNET_SYSTEM_THREADING_POOLASYNCVALUETASKS") is string value &&
            (bool.IsTrueStringIgnoreCase(value) || value == "1");

        private static int GetPoolAsyncValueTasksLimitValue() =>
            int.TryParse(Environment.GetEnvironmentVariable("DOTNET_SYSTEM_THREADING_POOLASYNCVALUETASKSLIMIT"), out int result) && result > 0 ?
                result :
                Environment.ProcessorCount * 4; // arbitrary default value
#endif
    }
}

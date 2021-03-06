﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Example.Native
{
    /// <summary>
    /// Allows you to marshal a <see cref="SafeHandle"/> array even though the marshaler is not equipped to marshal it directly.
    /// </summary>
    public sealed class SafeHandleArray : SafeHandle
    {
        private readonly SafeHandle[] handlesPrivate;
        private GCHandle dangerousHandleArrayHandle;
        private readonly bool[] mustReleaseArray;

        /// <summary>
        /// The number of handles in the array.
        /// </summary>
        public int Length => handlesPrivate.Length;

        /// <summary>
        /// Initializes a <see cref="SafeHandleArray"/> and copies the given <paramref name="handles"/> into it.
        /// </summary>
        public SafeHandleArray(params SafeHandle[] handles) : this(Initialize(handles))
        {
        }
        /// <summary>
        /// Initializes a <see cref="SafeHandleArray"/> and copies the given <paramref name="handles"/> into it.
        /// </summary>
        public SafeHandleArray(IEnumerable<SafeHandle> handles) : this(Initialize(handles))
        {
        }

        private struct InitializationParamaters
        {
            public readonly SafeHandle[] HandlesPrivate;
            public readonly GCHandle DangerousHandleArrayHandle;
            public readonly bool[] MustReleaseArray;

            public InitializationParamaters(SafeHandle[] handlesPrivate, GCHandle dangerousHandleArrayHandle, bool[] mustReleaseArray)
            {
                HandlesPrivate = handlesPrivate;
                DangerousHandleArrayHandle = dangerousHandleArrayHandle;
                MustReleaseArray = mustReleaseArray;
            }
        }

        private static InitializationParamaters Initialize(IEnumerable<SafeHandle> handles)
        {
            var privateHandles = handles.ToArray(); // Create a private copy so that it cannot be mutated

            var dangerousHandleArray = new IntPtr[privateHandles.Length];
            var mustReleaseArray = new bool[privateHandles.Length];

            for (var i = 0; i < privateHandles.Length; i++)
            {
                privateHandles[i].DangerousAddRef(ref mustReleaseArray[i]);
                dangerousHandleArray[i] = privateHandles[i].DangerousGetHandle();
            }

            return new InitializationParamaters(privateHandles, GCHandle.Alloc(dangerousHandleArray, GCHandleType.Pinned), mustReleaseArray);
        }

        private SafeHandleArray(InitializationParamaters parameters)
            : base(parameters.DangerousHandleArrayHandle.AddrOfPinnedObject(), true)
        {
            handlesPrivate = parameters.HandlesPrivate;
            dangerousHandleArrayHandle = parameters.DangerousHandleArrayHandle;
            mustReleaseArray = parameters.MustReleaseArray;
        }

        protected override bool ReleaseHandle()
        {
            for (var i = 0; i < handlesPrivate.Length; i++)
                if (mustReleaseArray[i])
                    handlesPrivate[i].DangerousRelease();

            dangerousHandleArrayHandle.Free();

            return true;
        }

        /// <summary>When overridden in a derived class, gets a value indicating whether the handle value is invalid.</summary>
        /// <returns>true if the handle value is invalid; otherwise, false.</returns>
        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}

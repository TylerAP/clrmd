// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.CorDebug;
using Microsoft.Diagnostics.Runtime.DacInterface;
using static Microsoft.Diagnostics.Runtime.Utilities.DebugShimNativeMethods;

namespace Microsoft.Diagnostics.Runtime
{
    public sealed class DacLibrary : IDisposable
    {
        private bool _disposed;
        private SOSDac _sos;

        internal DacDataTargetWrapper DacDataTarget { get; }

        internal RefCountedFreeLibrary OwningLibrary { get; }

        internal ClrDataProcess InternalDacPrivateInterface { get; }

        public ClrDataProcess DacPrivateInterface => new ClrDataProcess(InternalDacPrivateInterface);

        internal SOSDac GetSOSInterfaceNoAddRef()
        {
            if (_sos == null)
                _sos = InternalDacPrivateInterface.GetSOSDacInterface();

            return _sos;
        }

        public SOSDac SOSDacInterface
        {
            get
            {
                SOSDac sos = GetSOSInterfaceNoAddRef();
                return sos != null ? new SOSDac(sos) : null;
            }
        }

        public T GetInterface<T>(ref Guid riid)
            where T : CallableCOMWrapper
        {
            IntPtr pUnknown = InternalDacPrivateInterface.QueryInterface(ref riid);
            if (pUnknown == IntPtr.Zero)
                return null;

            T t = (T)Activator.CreateInstance(typeof(T), this, pUnknown);
            return t;
        }

        internal static IntPtr TryGetDacPtr(object ix)
        {
            if (!(ix is IntPtr pUnk))
            {
                if (Marshal.IsComObject(ix))
                    pUnk = Marshal.GetIUnknownForObject(ix);
                else
                    pUnk = IntPtr.Zero;
            }

            if (pUnk == IntPtr.Zero)
                throw new ArgumentException("clrDataProcess not an instance of IXCLRDataProcess");

            return pUnk;
        }

        internal DacLibrary(DataTarget dataTarget, IntPtr pUnk)
        {
            InternalDacPrivateInterface = new ClrDataProcess(this, pUnk);
        }

        public DacLibrary(DataTarget dataTarget, string dacDll)
        {
            if (dataTarget.ClrVersions.Count == 0)
                throw new ClrDiagnosticsException("Process is not a CLR process!");

            var n = DataTarget.PlatformFunctions;

            IntPtr addrDacLibrary = n.LoadLibrary(dacDll);
            if (addrDacLibrary == IntPtr.Zero)
                throw new ClrDiagnosticsException($"Failed to load dac {dacDll}");

            OwningLibrary = new RefCountedFreeLibrary(addrDacLibrary);
            dataTarget.AddDacLibrary(this);

            IntPtr addrInitializeDll = n.GetProcAddress(addrDacLibrary, "DAC_PAL_InitializeDLL");
            if (addrInitializeDll == IntPtr.Zero)
                addrInitializeDll = n.GetProcAddress(addrDacLibrary, "PAL_InitializeDLL");

            if (addrInitializeDll != IntPtr.Zero)
            {
                IntPtr dllMain = n.GetProcAddress(addrDacLibrary, "DllMain");
                DllMain main = (DllMain)Marshal.GetDelegateForFunctionPointer(dllMain, typeof(DllMain));
                bool dllMainResult = main(addrDacLibrary, 1, IntPtr.Zero);
                if (!dllMainResult)
                    Console.Error.WriteLine("Warning: DAC DllMain returned false");
            }

            IntPtr iUnk;

            IntPtr addrClrDataCreateInstance = n.GetProcAddress(addrDacLibrary, "CLRDataCreateInstance");
            DacDataTarget = new DacDataTargetWrapper(dataTarget);

            CreateDacInstance funcClrDataCreateInstance = (CreateDacInstance)
                Marshal.GetDelegateForFunctionPointer(addrClrDataCreateInstance, typeof(CreateDacInstance));

            Guid guid = new Guid("5c552ab6-fc09-4cb3-8e36-22fa03c798b7");
            int hr = funcClrDataCreateInstance(ref guid, DacDataTarget.IDacDataTarget, out iUnk);

            if (hr != 0)
                throw new ClrDiagnosticsException("Failure loading DAC: CreateDacInstance failed 0x" + hr.ToString("x"), ClrDiagnosticsException.HR.DacError);

            InternalDacPrivateInterface = new ClrDataProcess(this, iUnk);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DacLibrary()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                InternalDacPrivateInterface?.Dispose();
                _sos?.Dispose();
                OwningLibrary?.Release();

                _disposed = true;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool DllMain(IntPtr instance, int reason, IntPtr reserved);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PAL_Initialize();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateDacInstance(
            ref Guid riid,
            IntPtr dacDataInterface,
            out IntPtr ppObj);
    }
}
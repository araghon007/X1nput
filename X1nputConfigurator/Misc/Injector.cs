// Thanks to Dan Sporici https://codingvision.net/c-inject-a-dll-into-a-process-w-createremotethread

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace X1nputConfigurator.Misc
{
    public static class Injector
    {

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);
        
        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern int EnumProcessModulesEx(IntPtr hProcess, [Out] IntPtr lphModule, uint cb, out uint lpcbNeeded, uint dwFilterFlag);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern int GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpFilename, uint nSize);

        // privileges
        const int PROCESS_CREATE_THREAD = 0x0002;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_READ = 0x0010;

        // used for memory allocation
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 4;

        /// <summary>
        /// Unloads the X1nput DLL from specified process
        /// </summary>
        /// <param name="process">Process to unload X1nput from</param>
        public static bool Unload(ProcessInfo process)
        {
            // Refresh the process to get new modules
            var newProcess = Process.GetProcessById(process.Process.Id);

            // geting the handle of the process - with required privileges
            IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, newProcess.Id);

            // searching for the address of FreeLibraryAndExitThread and storing it in a pointer
            IntPtr freeLibraryAddr = IntPtr.Add(process.Kernel32, process.IsWOW64 ? Constants.FreeLibrary32 : Constants.FreeLibrary);

            // Setting up the variable for the second argument for EnumProcessModules
            IntPtr[] hMods = new IntPtr[1024];

            GCHandle gch = GCHandle.Alloc(hMods, GCHandleType.Pinned); // Don't forget to free this later
            IntPtr pModules = gch.AddrOfPinnedObject();

            // Setting up the rest of the parameters for EnumProcessModules
            uint uiSize = (uint)(Marshal.SizeOf(typeof(IntPtr)) * hMods.Length);
            
            if (EnumProcessModulesEx(procHandle, pModules, uiSize, out var cbNeeded, 0x03) == 1)
            {
                int uiTotalNumberofModules = (int)(cbNeeded / Marshal.SizeOf(typeof(IntPtr)));

                for (int i = 0; i < uiTotalNumberofModules; i++)
                {
                    StringBuilder strbld = new StringBuilder(1024);

                    GetModuleFileNameEx(procHandle, hMods[i], strbld, (uint)strbld.Capacity);

                    if (strbld.ToString().ToLower().Contains("x1nput"))
                    {
                        return CreateRemoteThread(procHandle, IntPtr.Zero, 0, freeLibraryAddr, hMods[i], 0, IntPtr.Zero) != IntPtr.Zero;
                    }
                }
            }

            // Must free the GCHandle object
            gch.Free();

            return false;
        }

        /// <summary>
        /// Inject the correct X1nput DLL into specified process 
        /// </summary>
        /// <param name="process">Process to inject X1nput to</param>
        public static bool Inject(ProcessInfo process)
        {
            // geting the handle of the process - with required privileges
            IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, process.Process.Id);

            // searching for the address of LoadLibraryA and storing it in a pointer
            //IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            // Q: "Did you really go this far just to make 1 executable for both 64 and 32-bit processes?" A: no u
            IntPtr loadLibraryAddr = IntPtr.Add(process.Kernel32, process.IsWOW64 ? Constants.LoadLibrary32 : Constants.LoadLibrary);


            // name of the dll we want to inject
            string dllName = $@"{Environment.CurrentDirectory}\X1nput.dll";

            if (IntPtr.Size == 8 && !process.IsWOW64)
            {
                dllName = $@"{Environment.CurrentDirectory}\X1nput64.dll";
            }
            // alocating some memory on the target process - enough to store the name of the dll
            // and storing its address in a pointer
            IntPtr allocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

            // writing the name of the dll there
            UIntPtr bytesWritten;
            WriteProcessMemory(procHandle, allocMemAddress, Encoding.Default.GetBytes(dllName), (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), out bytesWritten);

            // creating a thread that will call LoadLibraryA with allocMemAddress as argument
            // All that's needed for 32 bit injection is the right library address... How hard can it be?
            return CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero) != IntPtr.Zero;
        }
    }
}

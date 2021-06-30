// Thanks to Dan Sporici https://codingvision.net/c-inject-a-dll-into-a-process-w-createremotethread

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace X1nputConfigurator.Misc
{
    public static class Injector
    {

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);

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

        public static void UnDeInject(Process process)
        {
            // Refresh the process to get new modules
            process = Process.GetProcessById(process.Id);

            // geting the handle of the process - with required privileges
            IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, process.Id);

            // searching for the address of FreeLibraryAndExitThread and storing it in a pointer
            IntPtr freeLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "FreeLibraryAndExitThread");

            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.ToLower().Contains("x1nput"))
                {
                    // creating a thread that will call FreeLibraryAndExitThread with the module's base address as argument
                    // You have no idea how long this took to figure out
                    CreateRemoteThread(procHandle, IntPtr.Zero, 0, freeLibraryAddr, module.BaseAddress, 0, IntPtr.Zero);
                }
            }
        }

        /// <summary>
        /// Inject the correct X1nput DLL into specified process 
        /// </summary>
        /// <param name="process">Process to inject X1nput to</param>
        public static void Inject(Process process)
        {
            // geting the handle of the process - with required privileges
            IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, process.Id);

            // searching for the address of LoadLibraryA and storing it in a pointer
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            
            // name of the dll we want to inject
            string dllName = $@"{Environment.CurrentDirectory}\X1nput.dll";

            bool is64 = false;

            if (Environment.Is64BitOperatingSystem)
            {
                bool is32;
                if (IsWow64Process(process.Handle, out is32))
                {
                    if (!is32)
                    {
                        dllName = $@"{Environment.CurrentDirectory}\X1nput64.dll";
                        is64 = true;
                    }
                }
            }
            /*
            if (!is64 && IntPtr.Size == 8)
            {
                Inject32(process, dllName);
            }
            else
            {*/
                // alocating some memory on the target process - enough to store the name of the dll
                // and storing its address in a pointer
                IntPtr allocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

                // writing the name of the dll there
                UIntPtr bytesWritten;
                WriteProcessMemory(procHandle, allocMemAddress, Encoding.Default.GetBytes(dllName), (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), out bytesWritten);

                // creating a thread that will call LoadLibraryA with allocMemAddress as argument
                // All that's needed for 32 bit injection is the right library address... How hard can it be?
                CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
            //}
        }
    }
}

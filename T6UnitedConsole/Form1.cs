using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;

namespace T6UnitedConsole
{
    public partial class Form1 : Form
    {
        #region Mem Functions & Defines
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [Flags]
        public enum FreeType
        {
            Decommit = 0x4000,
            Release = 0x8000,
        }

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory( IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, FreeType dwFreeType);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        public byte[] cbuf_addtext_wrapper = 
        {
        0x55,
        0x8B, 0xEC,
        0x83, 0xEC, 0x8,
        0xC7, 0x45, 0xF8, 0x0, 0x0, 0x0, 0x0,
        0xC7, 0x45, 0xFC, 0x0, 0x0, 0x0, 0x0,
        0xFF, 0x75, 0xF8,
        0x6A, 0x0,
        0xFF, 0x55, 0xFC,
        0x83, 0xC4, 0x8,
        0x8B, 0xE5,
        0x5D,
        0xC3
        };

        #endregion

        IntPtr hProcess = IntPtr.Zero;
        int dwPID = -1;
        uint cbuf_address;
        uint nop_address;
        byte[] callbytes;
        IntPtr cbuf_addtext_alloc = IntPtr.Zero;
        byte[] commandbytes;
        IntPtr commandaddress;
        byte[] nopBytes = { 0x90, 0x90 };

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Send(textBox1.Text);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            FindGame();
            CheckMOTD();
        }

        void Send(string command)
        {
            try
            {
                callbytes = BitConverter.GetBytes(cbuf_address);
                if(command == "")
                {
                    MessageBox.Show("You must enter a command before pressing Send!", "Error", MessageBoxButtons.OK);
                }
                else
                {
                    if(cbuf_addtext_alloc == IntPtr.Zero)
                    {
                        cbuf_addtext_alloc = VirtualAllocEx(hProcess, IntPtr.Zero, (IntPtr)cbuf_addtext_wrapper.Length, AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ExecuteReadWrite);
                        commandbytes = System.Text.Encoding.ASCII.GetBytes(command);
                        commandaddress = VirtualAllocEx(hProcess, IntPtr.Zero, (IntPtr)(commandbytes.Length), AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ExecuteReadWrite);
                        int bytesWritten = 0;
                        int bytesWritten2 = commandbytes.Length;
                        WriteProcessMemory(hProcess, commandaddress, commandbytes, commandbytes.Length, out bytesWritten2);

                        Array.Copy(BitConverter.GetBytes(commandaddress.ToInt64()), 0, cbuf_addtext_wrapper, 9, 4);
                        Array.Copy(callbytes, 0, cbuf_addtext_wrapper, 16, 4);

                        WriteProcessMemory(hProcess, cbuf_addtext_alloc, cbuf_addtext_wrapper, cbuf_addtext_wrapper.Length, out bytesWritten);

                        IntPtr bytesOut;
                        CreateRemoteThread(hProcess, IntPtr.Zero, 0, cbuf_addtext_alloc, IntPtr.Zero, 0, out bytesOut);

                        if(cbuf_addtext_alloc != IntPtr.Zero && commandaddress != IntPtr.Zero)
                        {
                            VirtualFreeEx(hProcess, cbuf_addtext_alloc, cbuf_addtext_wrapper.Length, FreeType.Release);
                            VirtualFreeEx(hProcess, commandaddress, cbuf_addtext_wrapper.Length, FreeType.Release);
                        }
                    }
                    cbuf_addtext_alloc = IntPtr.Zero;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Send(richTextBox1.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog loadCFG = new OpenFileDialog();
            loadCFG.Filter = "T6 Movie Config(*.cfg)|*.cfg|All files(*.*)|*.*";
            DialogResult result = loadCFG.ShowDialog();
            if(result == DialogResult.OK)
            {
                StreamReader fileReader = new StreamReader(loadCFG.OpenFile());
                richTextBox1.Text = fileReader.ReadToEnd();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveCFG = new SaveFileDialog();
            saveCFG.Filter = "T6 Movie Config(*.cfg)|*.cfg|All files(*.*)|*.*";
            saveCFG.RestoreDirectory = true;
            DialogResult result = saveCFG.ShowDialog();
            if (result == DialogResult.OK)
            {
                StreamWriter fileWriter = new StreamWriter(saveCFG.OpenFile());
                fileWriter.WriteLine(richTextBox1.Text);
                fileWriter.Close();
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AboutForm about = new AboutForm();
            about.Show();
        }

        void CheckMOTD()
        {
            WebRequest request = WebRequest.Create("http://pastebin.com/raw/vC2ZV16r");
            WebResponse response = request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader MOTDreader = new StreamReader(responseStream);
            string currentMOTD = MOTDreader.ReadToEnd();
            if (Properties.Settings.Default.lastMOTD != currentMOTD)
            {
                MessageBox.Show(currentMOTD, "Message of the Day", MessageBoxButtons.OK);
                Properties.Settings.Default.lastMOTD = currentMOTD;
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Upgrade();
            }
            Debug.WriteLine(currentMOTD);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FindGame();
        }
        
        void FindGame()
        {
            if (Process.GetProcessesByName("t6mp").Length != 0)
            {
                cbuf_address = 0x5BDF70;
                nop_address = 0x8C90DA;
                hProcess = Process.GetProcessesByName("t6mp")[0].Handle;
                dwPID = Process.GetProcessesByName("t6mp")[0].Id;
                label3.Text = "Steam MP found.";
            }
            else if (Process.GetProcessesByName("t6zm").Length != 0)
            {
                cbuf_address = 0x4C7120;
                nop_address = 0x8C768A;
                hProcess = Process.GetProcessesByName("t6zm")[0].Handle;
                dwPID = Process.GetProcessesByName("t6zm")[0].Id;
                label3.Text = "Steam ZM found.";
            }
            else if (Process.GetProcessesByName("t6mpv43").Length != 0)
            {
                cbuf_address = 0x5C6F10;
                nop_address = 0x8C923A;
                hProcess = Process.GetProcessesByName("t6mpv43")[0].Handle;
                dwPID = Process.GetProcessesByName("t6mpv43")[0].Id;
                label3.Text = "Redacted MP found.";
            }
            else if (Process.GetProcessesByName("t6zmv41").Length != 0)
            {
                cbuf_address = 0x6B9D20;
                nop_address = 0x8C7E7A;
                hProcess = Process.GetProcessesByName("t6zmv41")[0].Handle;
                dwPID = Process.GetProcessesByName("t6zmv41")[0].Id;
                label3.Text = "Redacted ZM found.";
            }
            else
            {
                cbuf_address = 0x0;
                nop_address = 0x0;
                label3.Text = "No game found.";
            }
            hProcess = OpenProcess(ProcessAccessFlags.All, false, dwPID);
            int nopBytesLength = nopBytes.Length;
            WriteProcessMemory(hProcess, (IntPtr)nop_address, nopBytes, nopBytes.Length, out nopBytesLength);
        }
    }
}

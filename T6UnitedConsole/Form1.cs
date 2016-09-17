using System;
using System.Drawing;
using System.Threading;
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

        float sunLight,
            farBlur,
            farEnd,
            farStart,
            nearBlur,
            nearStart,
            nearEnd,
            intensity0,
            intensity1,
            skytemp,
            skytrans,
            skyrotation;

        float oldsunlight,
            oldfarblur,
            oldfarend,
            oldfarstart,
            oldnearblur,
            oldnearstart,
            oldnearend,
            oldintensity0,
            oldskytemp,
            oldskytrans,
            oldskyrotation;

        float[] sundir = new float[3],
            suncolor = new float[3],
            oldsundir = new float[3],
            oldsuncolor = new float[3];

        Thread monitorSun,
            monitorDOF,
            monitorSky;

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        private const int WM_DWMCOMPOSITIONCHANGED = 0x031A;
        private const int WM_THEMECHANGED = 0x031E;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

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


            this.BackColor = Color.LimeGreen;
            this.TransparencyKey = Color.LimeGreen;

            Random r = new Random();
            int rInt = r.Next(0, 5); //for ints

            switch(rInt)
            {
                case 0:
                    this.splitContainer1.BackgroundImage = T6UnitedConsole.Properties.Resources._1;
                    break;
                case 1:
                    this.splitContainer1.BackgroundImage = T6UnitedConsole.Properties.Resources._2;
                    break;
                case 2:
                    this.splitContainer1.BackgroundImage = T6UnitedConsole.Properties.Resources._3;
                    break;
                case 3:
                    this.splitContainer1.BackgroundImage = T6UnitedConsole.Properties.Resources._4;
                    break;
                case 4:
                    this.splitContainer1.BackgroundImage = T6UnitedConsole.Properties.Resources._5;
                    break;
                case 5:
                    this.splitContainer1.BackgroundImage = T6UnitedConsole.Properties.Resources._6;
                    break;
                default:
                    this.splitContainer1.BackgroundImage = T6UnitedConsole.Properties.Resources.Untitled_1;
                    break;
            }

            sunlight_Slider.BackColor = sunDirX_Slider.BackColor = sunDirY_Slider.BackColor = sunDirZ_Slider.BackColor = 
                sunColorR_Slider.BackColor = sunColorG_Slider.BackColor = sunColorB_Slider.BackColor =  
                dofFarBlur_Slider.BackColor = dofFarStart_Slider.BackColor = dofFarEnd_Slider.BackColor =
                dofNearBlur_Slider.BackColor = dofNearStart_Slider.BackColor = dofNearEnd_Slider.BackColor =
                skyIntensity0_Slider.BackColor = skyTemp_Slider.BackColor = skyRotation_Slider.BackColor = skyTransition_Slider.BackColor = Color.FromArgb(51, 51, 51);

            FindGame();
            CheckMOTD();
            monitorSun = new Thread(new ThreadStart(UpdateSun));
            monitorDOF = new Thread(new ThreadStart(UpdateDOF));
            monitorSky = new Thread(new ThreadStart(UpdateSky));

            monitorSun.Start();
            monitorDOF.Start();
            monitorSky.Start();

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
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

        private void button6_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
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
                DialogResult result = MessageBox.Show(currentMOTD + "\n\nWould you like to download the latest version now?", "Message of the Day", MessageBoxButtons.YesNo);
                if(result == DialogResult.Yes)
                {
                    Process.Start("http://azsry.com/data/t6uc.zip");
                    Environment.Exit(0);
                }
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
                dwPID = Process.GetProcessesByName("t6mp")[0].Id;
                label3.Text = "Steam MP found.";
            }
            else if (Process.GetProcessesByName("t6zm").Length != 0)
            {
                cbuf_address = 0x4C7120;
                nop_address = 0x8C768A;
                dwPID = Process.GetProcessesByName("t6zm")[0].Id;
                label3.Text = "Steam ZM found.";
            }
            else if (Process.GetProcessesByName("t6mpv43").Length != 0)
            {
                cbuf_address = 0x5C6F10;
                nop_address = 0x8C923A;
                dwPID = Process.GetProcessesByName("t6mpv43")[0].Id;
                label3.Text = "Redacted MP found.";
            }
            else if (Process.GetProcessesByName("t6zmv41").Length != 0)
            {
                cbuf_address = 0x6B9D20;
                nop_address = 0x8C7E7A;
                dwPID = Process.GetProcessesByName("t6zmv41")[0].Id;
                label3.Text = "Redacted ZM found.";
            }
            else if(Process.GetProcessesByName("BlackOpsMP").Length !=0)
            {
                cbuf_address = 0x56EF70;
                nop_address = 0x8B5A37;
                dwPID = Process.GetProcessesByName("BlackOpsMP")[0].Id;
                label3.Text = "Steam T5MP found.";
            }
            else if (Process.GetProcessesByName("BlackOps").Length != 0)
            {
                cbuf_address = 0x49B930;
                nop_address = 0x861D77;
                dwPID = Process.GetProcessesByName("BlackOps")[0].Id;
                label3.Text = "Steam T5SP/ZM found.";
            }
            else if (Process.GetProcessesByName("iw5mp").Length != 0)
            {
                cbuf_address = 0x545590;
                nop_address = 0x0;
                dwPID = Process.GetProcessesByName("iw5mp")[0].Id;
                label3.Text = "Steam T5MP found.";
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

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            sunLight = sunlight_Slider.Value;
            label5.Text = "r_lightTweakSunlight: " + sunlight_Slider.Value;
            
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            sundir[0] = sunDirX_Slider.Value;
            label6.Text = string.Format("r_lightTweakSunDirection: {0} {1} {2}", sundir[0], sundir[1], sundir[2]);
            
        }

        private void sunDirY_Slider_Scroll(object sender, EventArgs e)
        {
            sundir[1] = sunDirY_Slider.Value;
            label6.Text = string.Format("r_lightTweakSunDirection: {0} {1} {2}", sundir[0], sundir[1], sundir[2]);
            
        }

        private void sunDirZ_Slider_Scroll(object sender, EventArgs e)
        {
            sundir[2] = sunDirZ_Slider.Value;
            label6.Text = string.Format("r_lightTweakSunDirection: {0} {1} {2}", sundir[0], sundir[1], sundir[2]);
            
        }

        private void sunColorR_Slider_Scroll(object sender, EventArgs e)
        {
            suncolor[0] = (float)sunColorR_Slider.Value / 100;

            label7.Text = string.Format("r_lightTweakSunColor: {0} {1} {2}", suncolor[0], suncolor[1], suncolor[2]);
            
        }

        private void sunColorG_Slider_Scroll(object sender, EventArgs e)
        {
            suncolor[1] = (float)sunColorG_Slider.Value / 100;

            label7.Text = string.Format("r_lightTweakSunColor: {0} {1} {2}", suncolor[0], suncolor[1], suncolor[2]);
            
        }

        private void sunColorB_Slider_Scroll(object sender, EventArgs e)
        {
            suncolor[2] = (float)sunColorB_Slider.Value / 100;
            label7.Text = string.Format("r_lightTweakSunColor: {0} {1} {2}", suncolor[0], suncolor[1], suncolor[2]);
            
        }

        private void dofFarBlur_Slider_Scroll(object sender, EventArgs e)
        {
            farBlur = dofFarBlur_Slider.Value;
            label8.Text = "r_Dof_FarBlur: " + farBlur;
            
        }

        private void dofFarStart_Slider_Scroll(object sender, EventArgs e)
        {
            farStart = dofFarStart_Slider.Value;
            label9.Text = "r_Dof_FarStart: " + farStart;
            
        }

        private void dofFarEnd_Slider_Scroll(object sender, EventArgs e)
        {
            farEnd = dofFarEnd_Slider.Value;
            label10.Text = "r_Dof_FarEnd: " + farEnd;
            
        }

        private void dofNearBlur_Slider_Scroll(object sender, EventArgs e)
        {
            nearBlur = dofNearBlur_Slider.Value;
            label11.Text = "r_Dof_NearBlur: " + nearBlur;
            
        }

        private void dofNearStart_Slider_Scroll(object sender, EventArgs e)
        {
            nearStart = dofNearStart_Slider.Value;
            label4.Text = "r_Dof_NearStart: " + nearStart;
            
        }

        private void dofNearEnd_Slider_Scroll(object sender, EventArgs e)
        {
            nearEnd = dofNearEnd_Slider.Value;
            label12.Text = "r_Dof_NearEnd: " + nearEnd;
            
        }

        private void skyIntensity0_Slider_Scroll(object sender, EventArgs e)
        {
            intensity0 = (float)skyIntensity0_Slider.Value / 100;
            label14.Text = "r_sky_intensity_factor0: " + intensity0;
            
        }

        private void skyIntensity1_Slider_Scroll(object sender, EventArgs e)
        {
            intensity1 = (float)skyIntensity0_Slider.Value / 100;
            
        }

        private void skyTemp_Slider_Scroll(object sender, EventArgs e)
        {
            skytemp = skyTemp_Slider.Value;
            label15.Text = "r_skycolortemp: " + skytemp;
            
        }

        private void skyTransition_Slider_Scroll(object sender, EventArgs e)
        {
            skytrans = (float)skyTransition_Slider.Value / 100;
            label16.Text = "r_skyTransition: " + skytrans;
            
        }

        private void trackBar1_Scroll_1(object sender, EventArgs e)
        {
            skyrotation = skyRotation_Slider.Value;
            label17.Text = "r_skyRotation: " + skyrotation;
        }

        void UpdateSun()
        {
            for (;;)
            {
                if(sunLight != oldsunlight)
                {
                    Send(string.Format("r_lighttweaksunlight {0}", sunLight));
                    oldsunlight = sunLight;
                }

                if(sundir[0] != oldsundir[0] || sundir[1] != oldsundir[1] || sundir[2] != oldsundir[2])
                {
                    Send(string.Format("r_lighttweaksundirection {0} {1} {2}", sundir[0], sundir[1], sundir[2]));
                    oldsundir[0] = sundir[0];
                    oldsundir[1] = sundir[1];
                    oldsundir[2] = sundir[2];
                }

                if(suncolor[0] != oldsuncolor[0] || suncolor[1] != oldsuncolor[1] || suncolor[2] != oldsuncolor[2])
                {
                    Send(string.Format("r_lighttweaksuncolor {0} {1} {2}", suncolor[0], suncolor[1], suncolor[2]));
                    oldsuncolor[0] = suncolor[0];
                    oldsuncolor[1] = suncolor[1];
                    oldsuncolor[2] = suncolor[2];
                }
                Thread.Sleep(1);
            }
        }

        void UpdateDOF()
        {
            Send("r_Dof_Tweak 1;r_Dof_Bias 1;r_Dof_Enable 1");
            for(;;)
            {
                if(farBlur != oldfarblur)
                {
                    Send(string.Format("r_Dof_FarBlur {0}", farBlur));
                    oldfarblur = farBlur;
                }

                if(farStart != oldfarstart)
                {
                    Send(string.Format("r_Dof_FarStart {0}", farStart));
                    oldfarstart = farStart;
                }
                
                if(farEnd != oldfarend)
                {
                    Send(string.Format("r_Dof_FarEnd {0}", farEnd));
                    oldfarend = farEnd;
                }
               
                if(nearBlur != oldnearblur)
                {
                    Send(string.Format("r_Dof_NearBlur {0}", nearBlur));
                    oldnearblur = nearBlur;
                }
                
                if(nearStart != oldnearstart)
                {
                    Send(string.Format("r_Dof_NearStart {0}", nearStart));
                    oldnearstart = nearStart;
                }
                
                if(nearEnd != oldnearend)
                {
                    Send(string.Format("r_Dof_NearEnd {0}", nearEnd));
                    oldnearend = nearEnd;
                }
                
                Thread.Sleep(1);
            }

        }

        void UpdateSky()
        {
            for(;;)
            {
                if(intensity0 != oldintensity0)
                {
                    Send(string.Format("r_sky_intensity_factor0 {0}", intensity0));
                    oldintensity0 = intensity0;
                }
                
                if(skytemp != oldskytemp)
                {
                    Send(string.Format("r_skycolortemp {0}", skytemp));
                    oldskytemp = skytemp;
                }
                
                if(skytrans != oldskytrans)
                {
                    Send(string.Format("r_skyTransition {0}", skytrans));
                    oldskytrans = skytrans;
                }

                if(skyrotation != oldskyrotation)
                {
                    Send(string.Format("r_skyRotation {0}", skyrotation));
                    oldskyrotation = skyrotation;
                }
                
                Thread.Sleep(1);
            }
        }
    }
}

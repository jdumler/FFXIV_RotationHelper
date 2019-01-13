﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FFXIV_RotationHelper
{
    public partial class RotationWindow : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private RotationData loadedData;
        private List<SkillData> skillList;
        private List<PictureBox> pictureList;
        private int currentIdx = 0;

        public bool IsLoaded { get { return loadedData != null; } }
        public string IsLoadedURL { get { if (!IsLoaded) return string.Empty; return loadedData.URL; } }
        public bool IsPlaying { get; private set; }

        private const int interval = 20;

        public RotationWindow()
        {
            skillList = new List<SkillData>();
            pictureList = new List<PictureBox>();

            InitializeComponent();

            MouseDown += RotationWindow_MouseDown;
            VisibleChanged += RotationWindow_VisibleChanged;
            LocationChanged += RotationWindow_LocationChanged;
        }

        private void RotationWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void RotationWindow_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                Location = Properties.Settings.Default.Location;
                currentIdx = 0;
                MakePictureBox();
            }
        }

        private void RotationWindow_LocationChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                Properties.Settings.Default.Location = Location;
                Properties.Settings.Default.Save();
            }
        }

        public void LoadData(RotationData data)
        {
            loadedData = data;
            skillList = DB.Get(data.Sequence);
        }

        private void MakePictureBox()
        {
            if (pictureList.Count < skillList.Count)
            {
                int count = skillList.Count - pictureList.Count;
                for (int i = 0; i < count; ++i)
                {
                    PictureBox picture = new PictureBox
                    {
                        Size = new Size(Height, Height),
                        TabStop = false,
                        BackColor = Color.Black,
                        SizeMode = PictureBoxSizeMode.StretchImage,
                        WaitOnLoad = false
                    };
                    picture.MouseDown += RotationWindow_MouseDown;
                    Controls.Add(picture);
                    pictureList.Add(picture);
                }
            }

            for (int i = 0; i < skillList.Count; ++i)
            {
                pictureList[i].LoadAsync(skillList[i].IconURL);
            }

            Reposition();
            SetClickthrough(Properties.Settings.Default.Clickthrough);
        }

        private void Reposition()
        {
            for (int i = 0, idx = 0; i < skillList.Count; ++i)
            {
                if (i < currentIdx)
                {
                    pictureList[i].Visible = false;
                }
                else if (i < pictureList.Count)
                {
                    pictureList[i].Visible = true;
                    pictureList[i].Location = new Point((Height + interval) * idx++, 0);
                }
                else
                {
                    pictureList[i].Visible = false;
                }
            }

            for (int i = skillList.Count; i < pictureList.Count; ++i)
            {
                pictureList[i].Visible = false;
            }
        }

        public void OnActionCasted(LogData logData)
        {
            if (currentIdx >= skillList.Count)
                return;

            SkillData skillData = skillList[currentIdx];
            if (skillData.Idx == logData.DBCode)
            {
                ++currentIdx;
                if ((currentIdx >= skillList.Count) && Properties.Settings.Default.RestartOnEnd)
                {
                    currentIdx = 0;
                    MakePictureBox();
                }
                else
                {
                    Reposition();
                }
            }
        }

        public void SetClickthrough(bool clickthrough)
        {
            IntPtr hwnd = Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            int newStyle = clickthrough ? extendedStyle | WS_EX_TRANSPARENT : extendedStyle & ~WS_EX_TRANSPARENT;

            SetWindowLong(hwnd, GWL_EXSTYLE, newStyle);
        }
    }
}

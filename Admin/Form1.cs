﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

using tobid.rest;
using tobid.rest.position;
using tobid.util;
using tobid.util.orc;
using tobid.scheduler;
using tobid.scheduler.jobs;

namespace Admin {

    enum CaptchaInput {
        LEFT, MIDDLE, RIGHT
    }

    public partial class Form1 : Form {

        public Form1(String endPoint) {
            InitializeComponent();
            this.pictureSubs = new PictureBox[]{
                this.pictureSub1,
                this.pictureSub2,
                this.pictureSub3,
                this.pictureSub4,
                this.pictureSub5,
                this.pictureSub6
            };

            this.m_endPoint = endPoint;
        }

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Form1));
        private PictureBox[] pictureSubs;

        private String m_endPoint;

        private IOrc m_orcLogin;
        private IOrc m_orcCaptcha;
        private IOrc m_orcCaptchaLoading;
        private IOrc[] m_orcCaptchaTip;
        private IOrc m_orcPrice;
        private CaptchaUtil m_orcCaptchaTipsUtil;
        private BidForm m_bidForm;

        private System.Threading.Thread keepAliveThread;
        private System.Threading.Thread submitPriceThread;
        private System.Timers.Timer timer = new System.Timers.Timer();

        private Scheduler m_schedulerKeepAlive;
        private Scheduler m_schedulerSubmit;

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {

            Hotkey.UnregisterHotKey(this.Handle, 103);
            Hotkey.UnregisterHotKey(this.Handle, 104);
            Hotkey.UnregisterHotKey(this.Handle, 105);
            Hotkey.UnregisterHotKey(this.Handle, 106);
            Hotkey.UnregisterHotKey(this.Handle, 107);
            Hotkey.UnregisterHotKey(this.Handle, 108);
            Hotkey.UnregisterHotKey(this.Handle, 109);

            Hotkey.UnregisterHotKey(this.Handle, 121);
            Hotkey.UnregisterHotKey(this.Handle, 120);
            Hotkey.UnregisterHotKey(this.Handle, 122);
            Hotkey.UnregisterHotKey(this.Handle, 155);

            if (null != this.keepAliveThread)
                this.keepAliveThread.Abort();
            if (null != this.submitPriceThread)
                this.submitPriceThread.Abort();
        }

        private void Form1_Load(object sender, EventArgs e) {

            System.Console.WriteLine(logger.IsDebugEnabled);
            Form.CheckForIllegalCrossThreadCalls = false;
            this.textURL.Text = this.m_endPoint;
            this.m_bidForm = new BidForm();

            IGlobalConfig configResource = Resource.getInstance(this.m_endPoint);//加载配置

            this.Text = configResource.tag;
            this.m_orcLogin = configResource.Login;
            this.m_orcCaptcha = configResource.Captcha;//验证码
            this.m_orcPrice = configResource.Price;//价格识别
            this.m_orcCaptchaLoading = configResource.Loading;//LOADING识别
            this.m_orcCaptchaTip = configResource.Tips;//验证码提示（文字）
            this.m_orcCaptchaTipsUtil = new CaptchaUtil(m_orcCaptchaTip);

            //加载配置项2
            KeepAliveJob keepAliveJob = new KeepAliveJob(this.m_endPoint, new ReceiveOperation(this.receiveOperation));
            keepAliveJob.Execute();

            //keepAlive任务配置
            SchedulerConfiguration config5M = new SchedulerConfiguration(1000 * 60 * 1);
            config5M.Job = new KeepAliveJob(this.m_endPoint, new ReceiveOperation(this.receiveOperation));
            m_schedulerKeepAlive = new Scheduler(config5M);

            //Action任务配置
            SchedulerConfiguration config1S = new SchedulerConfiguration(1000);
            config1S.Job = new SubmitPriceJob(this.m_endPoint, this.m_orcPrice, this.m_orcCaptchaLoading, this.m_orcCaptchaTipsUtil, m_orcCaptcha);
            m_schedulerSubmit = new Scheduler(config1S);

            Hotkey.RegisterHotKey(this.Handle, 103, Hotkey.KeyModifiers.Ctrl, Keys.D3);
            Hotkey.RegisterHotKey(this.Handle, 104, Hotkey.KeyModifiers.Ctrl, Keys.D4);
            Hotkey.RegisterHotKey(this.Handle, 105, Hotkey.KeyModifiers.Ctrl, Keys.D5);
            Hotkey.RegisterHotKey(this.Handle, 106, Hotkey.KeyModifiers.Ctrl, Keys.D6);
            Hotkey.RegisterHotKey(this.Handle, 107, Hotkey.KeyModifiers.Ctrl, Keys.D7);
            Hotkey.RegisterHotKey(this.Handle, 108, Hotkey.KeyModifiers.Ctrl, Keys.D8);
            Hotkey.RegisterHotKey(this.Handle, 109, Hotkey.KeyModifiers.Ctrl, Keys.D9);

            Hotkey.RegisterHotKey(this.Handle, 121, Hotkey.KeyModifiers.Ctrl, Keys.Left);
            Hotkey.RegisterHotKey(this.Handle, 120, Hotkey.KeyModifiers.Ctrl, Keys.Up);
            Hotkey.RegisterHotKey(this.Handle, 122, Hotkey.KeyModifiers.Ctrl, Keys.Right);
            Hotkey.RegisterHotKey(this.Handle, 155, Hotkey.KeyModifiers.Ctrl, Keys.Enter);

        }

        protected override void WndProc(ref Message m) {
            const int WM_HOTKEY = 0x0312;
            switch (m.Msg) {
                case WM_HOTKEY:
                    switch (m.WParam.ToInt32()) {
                        case 103://CTRL+3
                            logger.Info("HOT KEY [CTRL+3]");
                            this.givePrice(this.m_endPoint, this.m_bidForm.bid.give, 300);
                            break;
                        case 104://CTRL+4
                            logger.Info("HOT KEY [CTRL+4]");
                            this.givePrice(this.m_endPoint, this.m_bidForm.bid.give, 400);
                            break;
                        case 105://CTRL+5
                            logger.Info("HOT KEY [CTRL+6]");
                            this.givePrice(this.m_endPoint, this.m_bidForm.bid.give, 500);
                            break;
                        case 106://CTRL+6
                            logger.Info("HOT KEY [CTRL+6]");
                            this.givePrice(this.m_endPoint, this.m_bidForm.bid.give, 600);
                            break;
                        case 107://CTRL+7
                            logger.Info("HOT KEY [CTRL+7]");
                            this.givePrice(this.m_endPoint, this.m_bidForm.bid.give, 700);
                            break;
                        case 108://CTRL+8
                            logger.Info("HOT KEY [CTRL+8]");
                            this.givePrice(this.m_endPoint, this.m_bidForm.bid.give, 800);
                            break;
                        case 109://CTRL+9
                            logger.Info("HOT KEY [CTRL+9]");
                            this.givePrice(this.m_endPoint, this.m_bidForm.bid.give, 900);
                            break;
                        case 120://CTRL+UP
                            logger.Info("HOT KEY [CTRL+UP]");
                            this.subimt(this.m_endPoint, this.m_bidForm.bid.submit, CaptchaInput.MIDDLE);
                            break;
                        case 121://CTRL+LEFT
                            logger.Info("HOT KEY [CTRL+LEFT]");
                            this.subimt(this.m_endPoint, this.m_bidForm.bid.submit, CaptchaInput.LEFT);
                            break;
                        case 122://CTRL+RIGHT
                            logger.Info("HOT KEY [CTRL+RIGHT]");
                            this.subimt(this.m_endPoint, this.m_bidForm.bid.submit, CaptchaInput.RIGHT);
                            break;
                        case 155://ENTER
                            logger.Info("HOT KEY [CTRL+ENTER]");
                            this.textPosition.Text = this.textMousePos.Text;
                            break;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        private void timer1_Tick(object sender, EventArgs e) {

            Point screenPoint = Control.MousePosition;
            this.textMousePos.Text = screenPoint.X + "," + screenPoint.Y;
        }

        private void button_checkPrice_Click(object sender, EventArgs e) {

            foreach (PictureBox picBox in this.pictureSubs)
                picBox.Image = null;

            String[] pos = this.textPosition.Text.Split(new char[] { ',' });
            byte[] content = new ScreenUtil().screenCaptureAsByte(Int32.Parse(pos[0]), Int32.Parse(pos[1]), 100, 24);
            this.pictureBox3.Image = Bitmap.FromStream(new System.IO.MemoryStream(content));
            String txtPrice = this.m_orcPrice.IdentifyStringFromPic(new Bitmap(this.pictureBox3.Image));
            for (int i = 0; i < this.m_orcPrice.SubImgs.Count; i++)
                this.pictureSubs[i].Image = this.m_orcPrice.SubImgs[i];
            this.label1.Text = txtPrice;
        }

        private void button_checkCaptcha_Click(object sender, EventArgs e) {

            foreach (PictureBox picBox in this.pictureSubs)
                picBox.Image = null;

            String[] pos = this.textPosition.Text.Split(new char[] { ',' });
            byte[] content = new ScreenUtil().screenCaptureAsByte(Int32.Parse(pos[0]), Int32.Parse(pos[1]), 120, 38);
            this.pictureBox3.Image = Bitmap.FromStream(new System.IO.MemoryStream(content));

            if (this.checkBoxCaptcha.Checked) {//如果选中“校验码”
            
                //String strCaptcha = new HttpUtil().postByteAsFile(this.textURL.Text + "/receive/captcha/detail.do", content);
                String strCaptcha = this.m_orcCaptcha.IdentifyStringFromPic(new Bitmap(new MemoryStream(content)));
                //String[] array = Newtonsoft.Json.JsonConvert.DeserializeObject<String[]>(strCaptcha);

                for (int i = 0; i < 6; i++)
                    this.pictureSubs[i].Image = this.m_orcCaptcha.SubImgs[i];
                this.label1.Text = strCaptcha;

            } else {//测试“正在加载校验码”

                String strLoading = this.m_orcCaptchaLoading.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));

                for (int i = 0; i < 6; i++)
                    this.pictureSubs[i].Image = this.m_orcCaptchaLoading.SubImgs[i];
                this.label2.Text = strLoading;
            }
        }

        private void button_checkTips_Click(object sender, EventArgs e) {

            foreach (PictureBox picBox in this.pictureSubs)
                picBox.Image = null;

            String[] pos = this.textPosition.Text.Split(new char[] { ',' });
            byte[] content = new ScreenUtil().screenCaptureAsByte(Int32.Parse(pos[0]), Int32.Parse(pos[1]), 140, 24);
            this.pictureBox3.Image = Bitmap.FromStream(new System.IO.MemoryStream(content));

            this.label2.Text = this.m_orcCaptchaTipsUtil.getActive("一二三四五六", new Bitmap(new MemoryStream(content)));
            for (int i = 0; i < this.m_orcCaptchaTipsUtil.SubImgs.Count; i++)
                this.pictureSubs[i].Image = this.m_orcCaptchaTipsUtil.SubImgs[i];
            
        }

        private void button_ConfigBid_Click(object sender, EventArgs e) {

            this.m_bidForm.endPoint = this.textURL.Text;
            this.m_bidForm.ShowDialog(this);
            this.m_bidForm.BringToFront();
        }

        private void receiveOperation(Operation operation) {
            try {
                //ShowInfoJob showInfo = new ShowInfoJob("MESSAGE!");
                //System.Threading.ThreadStart myThreadDelegate = new System.Threading.ThreadStart(showInfo.Execute);
                //System.Threading.Thread myThread = new System.Threading.Thread(myThreadDelegate);
                //myThread.Start();
            } catch {
            }

            if (null != operation) {
                BidOperation bidOps = (BidOperation)operation;
                Bid bid = Newtonsoft.Json.JsonConvert.DeserializeObject<Bid>(operation.content);
                this.m_bidForm.bid = bid;
                this.toolStripStatusLabel1.Text = String.Format("配置：+{5} @[{4}], 价格[{0},{1}], 校验码[{2},{3}]", bid.give.price.x, bid.give.price.y, bid.submit.captcha[0].x, bid.submit.captcha[0].y, operation.startTime, bidOps.price);
            }
        }

        private void radioButton_Manual_CheckedChanged(object sender, EventArgs e) {
            
            if (this.radioButton1.Checked) {
                if (null != this.keepAliveThread)
                    this.keepAliveThread.Abort();
                if (null != this.submitPriceThread)
                    this.submitPriceThread.Abort();
            }
        }

        private void radioButton_Auto_CheckedChanged(object sender, EventArgs e) {

            if (this.radioButton2.Checked) {
                System.Threading.ThreadStart keepAliveThread = new System.Threading.ThreadStart(this.m_schedulerKeepAlive.Start);
                this.keepAliveThread = new System.Threading.Thread(keepAliveThread);
                this.keepAliveThread.Name = "keepAliveThread";
                this.keepAliveThread.Start();

                System.Threading.ThreadStart submitPriceThreadStart = new System.Threading.ThreadStart(this.m_schedulerSubmit.Start);
                this.submitPriceThread = new System.Threading.Thread(submitPriceThreadStart);
                this.submitPriceThread.Name = "submitPriceThread";
                this.submitPriceThread.Start();
            }
        }


        #region 拍ACTION
        private void givePrice(String URL, GivePrice points, int deltaPrice) {

            logger.WarnFormat("BEGIN 出价格(delta : {0})", deltaPrice);
            byte[] content = new ScreenUtil().screenCaptureAsByte(points.price.x, points.price.y, 52, 18);
            this.pictureBox2.Image = Bitmap.FromStream(new System.IO.MemoryStream(content));
            logger.Info("\tBEGIN identify Price");
            String txtPrice = this.m_orcPrice.IdentifyStringFromPic(new Bitmap(this.pictureBox2.Image));
            logger.InfoFormat("\tEND  identify Price : {0}", txtPrice);
            int price = Int32.Parse(txtPrice);
            price += deltaPrice;
            txtPrice = String.Format("{0:D5}", price);

            logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
            ScreenUtil.SetCursorPos(points.inputBox.x, points.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);

            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);

            for (int i = 0; i < txtPrice.Length; i++) {
                System.Threading.Thread.Sleep(50);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0, 0);
            }
            logger.Info("\tEND   input PRICE");

            //点击出价
            System.Threading.Thread.Sleep(50);
            logger.Info("\tBEGIN click button[出价]");
            ScreenUtil.SetCursorPos(points.button.x, points.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click button[出价]");
            logger.Info("END   出价格");
        }

        private void subimt(String URL, SubmitPrice points, CaptchaInput inputType) {

            logger.WarnFormat("BEGIN 验证码({0})", inputType);

            ScreenUtil.SetCursorPos(points.inputBox.x, points.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            logger.Info("\tBEGIN make INPUTBOX blank");
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);

            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            logger.Info("\tEND   make INPUTBOX blank");

            byte[] content = new ScreenUtil().screenCaptureAsByte(points.captcha[0].x, points.captcha[0].y, 128, 28);
            Bitmap bitmap = new Bitmap(new MemoryStream(content));
            this.pictureBox1.Image = bitmap;
            String strLoading = this.m_orcCaptchaLoading.IdentifyStringFromPic(bitmap);
            logger.InfoFormat("LOADING : {0}", strLoading);
            //if ("正在获取校验码".Equals(strLoading)) {
            //    logger.InfoFormat("正在获取校验码，关闭&打开窗口重新获取");
            //    ScreenUtil.SetCursorPos(points.buttons[0].x + 188, points.buttons[0].y);//取消按钮
            //    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            //    return;
            //}
            
            logger.Info("\tBEGIN identify Captcha");
            String txtCaptcha = this.m_orcCaptcha.IdentifyStringFromPic(bitmap);
            logger.InfoFormat("\tEND   identify Captcha : [{0}]", txtCaptcha);

            logger.InfoFormat("\tBEGIN input ACTIVE CAPTCHA [{0}]", inputType);
            String strActive = "";
            if (CaptchaInput.LEFT == inputType)
                strActive = txtCaptcha.Substring(0, 4);
            else if (CaptchaInput.MIDDLE == inputType)
                strActive = txtCaptcha.Substring(1, 4);
            else if (CaptchaInput.RIGHT == inputType)
                strActive = txtCaptcha.Substring(2, 4);

            for (int i = 0; i < strActive.Length; i++) {
                ScreenUtil.keybd_event(ScreenUtil.keycode[strActive[i].ToString()], 0, 0, 0);
                System.Threading.Thread.Sleep(50);
            }
            logger.InfoFormat("\tEND   input ACTIVE CAPTCHA [{0}]", strActive);

            {
                System.Threading.Thread.Sleep(50);
                MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
                DialogResult dr = MessageBox.Show("确定要提交出价吗?", "提交出价", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                if (dr == DialogResult.OK) {
                    logger.Info("用户选择确定出价");
                    ScreenUtil.SetCursorPos(points.buttons[0].x, points.buttons[0].y);//确定按钮
                    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

                    System.Threading.Thread.Sleep(1000);
                    ScreenUtil.SetCursorPos(points.buttons[0].x + 188 / 2, points.buttons[0].y - 10);//确定按钮
                    //ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                } else {
                    logger.Warn("用户选择取消出价");
                    ScreenUtil.SetCursorPos(points.buttons[0].x + 188, points.buttons[0].y);//取消按钮
                    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                }
            }
            logger.Info("END   验证码");
        }
        #endregion

        static void ie_DocumentComplete(object pDisp, ref object URL) {

            DocComplete.Set();
            //"testBtnConfirm";
            //"protocolBtnConfirm";
            //mshtml.IHTMLDocument2 doc2 = (mshtml.IHTMLDocument2)Browser.Document;
            //mshtml.IHTMLElement confirm1 = doc2.all.item("testBtnConfirm") as mshtml.IHTMLElement;
            //confirm1.click();
            //System.Threading.Thread.Sleep(1000);

            //mshtml.IHTMLElement confirm2 = doc2.all.item("protocolBtnConfirm") as mshtml.IHTMLElement;
            //confirm2.click();
            //System.Threading.Thread.Sleep(1000);
        }

        private static System.Threading.AutoResetEvent DocComplete = new System.Threading.AutoResetEvent(false);
        private void button7_Click(object sender, EventArgs e) {

            System.Diagnostics.Process[] myProcesses;
            myProcesses = System.Diagnostics.Process.GetProcessesByName("IEXPLORE");
            foreach (System.Diagnostics.Process instance in myProcesses) {
                instance.Kill();
            }

            System.Diagnostics.Process.Start("iexplore.exe", "about:blank");
            System.Threading.Thread.Sleep(1000);

            SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindowsClass();
            foreach (SHDocVw.InternetExplorer Browser in shellWindows) {
                if (Browser.LocationURL.Contains("about:blank")) {

                    Browser.DocumentComplete += new SHDocVw.DWebBrowserEvents2_DocumentCompleteEventHandler(ie_DocumentComplete);
                    //Browser.Navigate("https://paimai.alltobid.com/bid/2015081501/login.htm");
                    Browser.Navigate("http://192.168.1.18/chapta.ws/command/login.do");
                    DocComplete.WaitOne();
                    //"testBtnConfirm";
                    //"protocolBtnConfirm";
                    mshtml.IHTMLDocument2 doc2 = (mshtml.IHTMLDocument2)Browser.Document;
                    mshtml.IHTMLElement confirm1 = doc2.all.item("testBtnConfirm") as mshtml.IHTMLElement;
                    if(null != confirm1)
                        confirm1.click();
                    System.Threading.Thread.Sleep(1000);

                    mshtml.IHTMLElement confirm2 = doc2.all.item("protocolBtnConfirm") as mshtml.IHTMLElement;
                    if(null != confirm2)
                        confirm2.click();
                    System.Threading.Thread.Sleep(1000);

                    //"bidnumber";
                    //"bidpassword";
                    //"idcard";
                    //"imagenumber";
                    //"imgcode";
                    //"btnlogin";
                    mshtml.IHTMLElement imgCode = doc2.images.item("imgcode") as mshtml.IHTMLElement;
                    mshtml.HTMLBody body = doc2.body as mshtml.HTMLBody;
                    mshtml.IHTMLControlRange rang = body.createControlRange() as mshtml.IHTMLControlRange;
                    mshtml.IHTMLControlElement img = imgCode as mshtml.IHTMLControlElement;
                    rang.add(img);
                    rang.execCommand("Copy", false, null);  //拷贝到内存
                    Image numImage = Clipboard.GetImage();
                    System.IO.MemoryStream ms = new System.IO.MemoryStream();
                    numImage.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    String strCaptcha = this.m_orcLogin.IdentifyStringFromPic(new Bitmap(ms), 5);

                    mshtml.IHTMLElementCollection inputs = (mshtml.IHTMLElementCollection)doc2.all.tags("INPUT");
                    mshtml.HTMLInputElement input1 = (mshtml.HTMLInputElement)inputs.item("bidnumber");
                    input1.value = "52869259";
                    mshtml.HTMLInputElement input2 = (mshtml.HTMLInputElement)inputs.item("bidpassword");
                    input2.value = "3621";
                    mshtml.HTMLInputElement input3 = (mshtml.HTMLInputElement)inputs.item("idcard");
                    input3.value = "ryo_hune";
                    mshtml.HTMLInputElement input4 = (mshtml.HTMLInputElement)inputs.item("imagenumber");
                    input4.value = strCaptcha;

                    mshtml.IHTMLElement loginBtn = doc2.all.item("btnlogin") as mshtml.IHTMLElement;
                    //loginBtn.click();
                }
            }
        }
    }
}

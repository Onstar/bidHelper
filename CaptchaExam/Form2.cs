﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using tobid.util.http;
using System.Configuration;

namespace CaptchaExam
{
    public partial class Form2 : Form
    {
        public Form2(int ms, Proxy proxy)
        {
            InitializeComponent();
            this.max = ms;
            this.proxySetting = proxy;
        }

        private Captcha[] repository;
        private String endPoint = "http://10.228.89.127/captcha.simulate";

        Random rd = new Random();
        private string captcha = "captcha";
        private DateTime start = DateTime.Now;
        private DateTime end = DateTime.Now;
        private int max;
        private Proxy proxySetting;

        public Boolean Result { get; set; }
        public TimeSpan Cost { get { return end - start; } }
        public Boolean isManual { get; set; }

        private Object lockObj = new Object();

        private void Form2_Load(object sender, EventArgs e)
        {
            this.endPoint = ConfigurationManager.AppSettings["ENDPOINT"];
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel, ((byte)(134)));

            Control.CheckForIllegalCrossThreadCalls = false;

            System.Threading.Monitor.Enter(this.lockObj);

            this.isManual = false;
            //RestClient rest = new RestClient(this.endPoint + "/repository.json", HttpVerb.GET);
            //String repository = rest.MakeRequest(false);
            //this.repository = Newtonsoft.Json.JsonConvert.DeserializeObject<Captcha[]>(repository);

            System.Threading.Thread startCaptcha = new System.Threading.Thread(delegate()
            {
                //int index = rd.Next(this.repository.Length);
                //String url = String.Format("{0}/{1}", this.endPoint, this.repository[index].url);

                this.labelTips.Text = "                    ";
                this.pictureBoxCaptcha.Image = Properties.Resources.Loading;

                Random random = new Random();
                int refresh = random.Next(100);
                System.Console.WriteLine(refresh);
                if (refresh > 89) {//刷新按钮

                    System.Threading.Thread.Sleep(1500);
                    this.pictureBoxCaptcha.Image = Properties.Resources.refreshBTN;
                    for (int left = 1000; left >= 0; left -= 250) {

                        this.label1.Text = String.Format("{0}ms", left);
                        System.Threading.Thread.Sleep(250);
                        if (System.Threading.Monitor.TryEnter(this.lockObj)) {
                            goto ShowCpatcha;
                        }
                    }
                    this.Result = false;
                    this.Close();
                    return;
                }

            ShowCpatcha:

                RestClient rest = new RestClient(this.endPoint + "/rest/service/simulate/captcha", HttpVerb.GET);
                String repository = rest.MakeRequest(false, this.proxySetting);
                Captcha captcha = Newtonsoft.Json.JsonConvert.DeserializeObject<Captcha>(repository);

                String url = String.Format("{0}/{1}", this.endPoint, captcha.url);
                HttpUtil httpReq = new HttpUtil();
                System.IO.Stream ms = httpReq.getAsBinary(url, this.proxySetting);
                
                this.pictureBoxCaptcha.Image = new Bitmap(ms);
                this.captcha = captcha.value;
                this.labelTips.Text = captcha.tip;

                this.textBox1.Text = "";
                this.textBox1.Focus();

                this.start = DateTime.Now;
                for (int left = max; left>=0; left -= 250)
                {
                    this.label1.Text = String.Format("{0}ms", left);
                    System.Threading.Thread.Sleep(250);
                }

                this.label1.ForeColor = Color.Red;
                this.label1.Text = "TIMEOUT!";

                this.end = DateTime.Now;
                this.Result = this.textBox1.Text.Equals(this.captcha);
                this.Close();
            });
            startCaptcha.Start();
        }


        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.end = DateTime.Now;
            this.Result = false;
            this.Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.end = DateTime.Now;
            this.Result = this.textBox1.Text.Equals(this.captcha);
            this.Close();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ')
            {
                this.isManual = true;
                this.end = DateTime.Now;
                this.Result = this.textBox1.Text.Equals(this.captcha);
                this.Close();
            }
        }

        private void pictureBoxCaptcha_Click(object sender, EventArgs e) {
            
            System.Threading.Monitor.Exit(this.lockObj);
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e) {
            if(System.Threading.Monitor.TryEnter(this.lockObj))
                System.Threading.Monitor.Exit(this.lockObj);
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e) {

            System.Console.WriteLine(e.KeyData);
            if (e.KeyData == (Keys.R | Keys.Control)) {
                System.Threading.Monitor.Exit(this.lockObj);
                System.Console.WriteLine("Release lock");
            }
        }
    }
}

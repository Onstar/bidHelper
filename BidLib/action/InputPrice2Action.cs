﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tobid.scheduler.jobs.action;
using tobid.util;
using tobid.rest.position;
using System.Drawing;

namespace tobid.scheduler.jobs.action {

    /// <summary>
    /// 判断是否需要2次重新出价
    /// 如果上次价格(basePrice<当前价+500 || basePrice>当前价+700)，需要取消并重新出价
    /// </summary>
    public class InputPrice2Action : IBidAction{

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(InputPrice2Action));

        private int delta;
        private IRepository repository;
        private InputPriceAction inputPrice;
        private TaskSwitchable switcher;
        public InputPrice2Action(int delta, IRepository repo, InputPriceAction inputPrice, TaskSwitchable switchTask) {
            this.delta = delta;
            this.repository = repo;
            this.inputPrice = inputPrice;
            this.switcher = switchTask;
        }

        public int BasePrice { get; set; }

        public void notify(string message) {
            logger.Debug(message);
        }

        public bool execute() {

            System.Drawing.Point origin = IEUtil.findOrigin();
            int x = origin.X;
            int y = origin.Y;
            BidStep2 bidStep2 = this.repository.bidStep2;

            logger.DebugFormat("CAPTURE PRICE-sm({0}, {1})", x + bidStep2.price.x, y + bidStep2.price.y);
            byte[] content = new ScreenUtil().screenCaptureAsByte(x + bidStep2.price.x, y + bidStep2.price.y, 52, 18);
            String txtPrice = this.repository.orcPriceSM.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
            int price = Int32.Parse(txtPrice) + this.delta;

            int min = Int32.Parse(txtPrice) + 500;
            int max = Int32.Parse(txtPrice) + 500;
            logger.DebugFormat("Last Price : {0}, RANGE({1} - {2})", this.inputPrice.BasePrice, min, max);
            if (this.inputPrice.BasePrice < min || this.inputPrice.BasePrice > max) {
                //需要取消重新出价
                logger.Info("Cancel and Re-Input price!");
                
                logger.Info("Update new submit trigger");
                this.switcher.toggle();
                
                //点取消按钮
                logger.Info("\tBEGIN click CANCEL button");
                ScreenUtil.SetCursorPos(x + bidStep2.submit.buttons[1].x, y + bidStep2.submit.buttons[1].y);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                logger.Info("\tEND   click CANCEL button");

                System.Threading.Thread.Sleep(250);

                logger.InfoFormat("\tBEGIN invoke InputPriceAction(+{0})", this.delta);
                InputPriceAction inputPrice = new InputPriceAction(this.delta, this.repository);
                inputPrice.execute();
                logger.Info("\tEND   invoke InputPriceAction");
            }
            return true;
        }
    }
}

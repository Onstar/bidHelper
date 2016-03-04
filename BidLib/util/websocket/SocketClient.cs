﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocketSharp;
using Newtonsoft.Json.Converters;
using System.Threading;

namespace tobid.util.http.ws {
    
    public class KeepAliveHandler {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(KeepAliveHandler));

        public String user { get; set; }
        public int interval { get; set; }
        public SocketClient session { get; set; }
        public Thread thread { get; set; }

        public void abort() { this.interval = 0; }

        #region KeepAlive Thread
        public static void keepAliveThread(object obj) {

            KeepAliveHandler handler = (KeepAliveHandler)obj;
            logger.InfoFormat("KeepAlive initialize.", handler.interval);
            while (handler.interval > 0) {

                logger.DebugFormat("SLEEP {0}s, push Heartbeat", handler.interval);
                Thread.Sleep(handler.interval * 1000);
                handler.session.send(new HeartBeat());
            }
            logger.InfoFormat("KeepAlive terminated!", handler.interval);
        }
        #endregion
    }

    public class SocketClient : IDisposable {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SocketClient));

        public void Dispose() { this.stop(); }

        public delegate void ProcessMessage(Command command);
        public delegate void ProcessClose();

        public static String USER = "USER";
        public static int MAX_RECONNECT = 5;

        private String url;
        private String user;
        private WebSocket webSocket;
        private ProcessMessage processMessage;

        private int interval;
        private KeepAliveHandler keepAlive;

        public SocketClient(String url, String user, ProcessMessage processor) {

            this.url = url;
            this.user = user;
            this.processMessage = processor;
        }

        public void start(int interval = 30) {

            logger.DebugFormat("connecting to {0}", this.url);
            logger.InfoFormat("START(keepAlive : {0})", interval);
            this.interval = interval;
            this.connect();
        }

        public void stop() {

            logger.Info("STOP!");
            if (null != this.keepAlive)
                this.keepAlive.abort();

            this.webSocket.Close(CloseStatusCode.Normal, "USER CLOSED");
            ((IDisposable)this.webSocket).Dispose();
        }

        public void send(Command command) {

            String value = Newtonsoft.Json.JsonConvert.SerializeObject(command, new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" });
            this.webSocket.Send(value);
        }

        private void OnError(Object sender, ErrorEventArgs msg) {

            logger.ErrorFormat("ERROR : {0}", msg.Message);
        }

        private void OnMessage(Object sender, MessageEventArgs msg) {

            logger.Info("ON MESSAGE");
            if (msg.IsPing) {

                logger.Debug("PING");
            } else {

                logger.DebugFormat("ON MESSAGE : {1}", this.user, msg.Data);
                Command command = Newtonsoft.Json.JsonConvert.DeserializeObject<Command>(msg.Data, new CommandConvert());
                this.processMessage(command);
            }
        }

        private void OnClose(Object sender, CloseEventArgs msg) {

            logger.InfoFormat("ON CLOSE code:{0} - {1}", msg.Code, msg.Reason);
            this.stop();
        }

        private void OnOpen(Object sender, EventArgs e) {

            logger.InfoFormat("ON CONNECT : {0}", this.user);
            if (null != this.keepAlive)
                this.keepAlive.abort();

            this.keepAlive = new KeepAliveHandler();
            this.keepAlive.session = this;
            this.keepAlive.interval = interval;
            this.keepAlive.user = this.user;
            this.keepAlive.thread = new System.Threading.Thread(KeepAliveHandler.keepAliveThread);
            this.keepAlive.thread.Start(this.keepAlive);
        }

        private void connect() {

            this.webSocket = new WebSocket(this.url);
            this.webSocket.OnOpen += new EventHandler(OnOpen);
            this.webSocket.OnClose += new EventHandler<CloseEventArgs>(OnClose);
            this.webSocket.OnError += new EventHandler<ErrorEventArgs>(OnError);
            this.webSocket.OnMessage += new EventHandler<MessageEventArgs>(OnMessage);

            this.webSocket.SetCookie(new WebSocketSharp.Net.Cookie(SocketClient.USER, String.IsNullOrEmpty(this.user)?"DEFAULT WSocket":this.user));
            this.webSocket.Log.Level = LogLevel.Debug;
            this.webSocket.Connect();
        }
    }
}

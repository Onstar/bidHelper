﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace tobid.util.hook {

    public class KeyboardHook {
        int hHook;
        Win32Api.HookProc KeyboardHookDelegate;
        public event KeyEventHandler OnKeyDownEvent;
        public event KeyEventHandler OnKeyUpEvent;
        public event KeyPressEventHandler OnKeyPressEvent;
        public KeyboardHook() { }
        public void SetHook() {
            KeyboardHookDelegate = new Win32Api.HookProc(KeyboardHookProc);
            Process cProcess = Process.GetCurrentProcess();
            ProcessModule cModule = cProcess.MainModule;
            var mh = Win32Api.GetModuleHandle(cModule.ModuleName);
            hHook = Win32Api.SetWindowsHookEx(Win32Api.WH_KEYBOARD_LL, KeyboardHookDelegate, mh, 0);
        }
        public void UnHook() {
            Win32Api.UnhookWindowsHookEx(hHook);
        }
        private List<Keys> preKeysList = new List<Keys>();//存放被按下的控制键，用来生成具体的键

        private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam) {

            Win32Api.KeyboardHookStruct KeyDataFromHook = (Win32Api.KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(Win32Api.KeyboardHookStruct));
            Keys keyData = (Keys)KeyDataFromHook.vkCode;
            //如果该消息被丢弃（nCode<0）或者没有事件绑定处理程序则不会触发事件
            if ((nCode >= 0) && (OnKeyDownEvent != null || OnKeyUpEvent != null || OnKeyPressEvent != null)) {
                //Win32Api.KeyboardHookStruct KeyDataFromHook = (Win32Api.KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(Win32Api.KeyboardHookStruct));
                //Keys keyData = (Keys)KeyDataFromHook.vkCode;
                //按下控制键
                if ((OnKeyDownEvent != null || OnKeyPressEvent != null) && (wParam == Win32Api.WM_KEYDOWN || wParam == Win32Api.WM_SYSKEYDOWN)) {
                    if (IsCtrlAltShiftKeys(keyData) && preKeysList.IndexOf(keyData) == -1) {
                        preKeysList.Add(keyData); System.Console.WriteLine("ADD " + keyData);
                    }
                }
                //WM_KEYDOWN和WM_SYSKEYDOWN消息，将会引发OnKeyDownEvent事件
                if (OnKeyDownEvent != null && (wParam == Win32Api.WM_KEYDOWN || wParam == Win32Api.WM_SYSKEYDOWN)) {
                    
                    KeyEventArgs e = new KeyEventArgs(GetDownKeys(keyData));
                    //System.Console.WriteLine(GetDownKeys(keyData));
                    OnKeyDownEvent(this, e);
                }
                //WM_KEYDOWN消息将引发OnKeyPressEvent 
                if (OnKeyPressEvent != null && wParam == Win32Api.WM_KEYDOWN) {
                    byte[] keyState = new byte[256];
                    Win32Api.GetKeyboardState(keyState);
                    byte[] inBuffer = new byte[2];
                    if (Win32Api.ToAscii(KeyDataFromHook.vkCode, KeyDataFromHook.scanCode, keyState, inBuffer, KeyDataFromHook.flags) == 1) {
                        KeyPressEventArgs e = new KeyPressEventArgs((char)inBuffer[0]);
                        OnKeyPressEvent(this, e);
                    }
                }
                //松开控制键
                if ((OnKeyDownEvent != null || OnKeyPressEvent != null) && (wParam == Win32Api.WM_KEYUP || wParam == Win32Api.WM_SYSKEYUP)) {
                    if (IsCtrlAltShiftKeys(keyData)) {
                        for (int i = preKeysList.Count - 1; i >= 0; i--) {
                            if (preKeysList[i] == keyData) { preKeysList.RemoveAt(i); System.Console.WriteLine("REMOVE " + keyData); }
                        }
                    }
                }
                //WM_KEYUP和WM_SYSKEYUP消息，将引发OnKeyUpEvent事件 
                if (OnKeyUpEvent != null && (wParam == Win32Api.WM_KEYUP || wParam == Win32Api.WM_SYSKEYUP)) {
                    KeyEventArgs e = new KeyEventArgs(GetDownKeys(keyData));
                    OnKeyUpEvent(this, e);
                }
            }

            if((wParam == Win32Api.WM_KEYDOWN || wParam == Win32Api.WM_SYSKEYDOWN) && !isAllow(keyData))
                return 1;
            else
                return Win32Api.CallNextHookEx(hHook, nCode, wParam, lParam);
        }

        private bool isAllow(Keys keyData) {

            return (keyData >= Keys.D0 && keyData <= Keys.D9) || (keyData >= Keys.NumPad0 && keyData <= Keys.NumPad9)
                || (keyData >= Keys.A && keyData <= Keys.Q)
                || (keyData == Keys.R && (preKeysList.IndexOf(Keys.LControlKey) == -1 && preKeysList.IndexOf(Keys.RControlKey) == -1))//R允许，CTRL+R不允许
                || (keyData >= Keys.S && keyData <= Keys.Z)
                || (keyData >= Keys.Left && keyData <= Keys.Down)
                || keyData == (Keys)231//packet
                || keyData == Keys.Oemcomma || keyData == Keys.OemPeriod
                || keyData == Keys.Tab || keyData == Keys.Back || keyData == Keys.Delete || keyData == Keys.RControlKey || keyData == Keys.LControlKey;
        }

        //根据已经按下的控制键生成key
        private Keys GetDownKeys(Keys key) {
            Keys rtnKey = Keys.None;
            foreach (Keys i in preKeysList) {
                if (i == Keys.LControlKey || i == Keys.RControlKey) { rtnKey = rtnKey | Keys.Control; }
                if (i == Keys.LMenu || i == Keys.RMenu) { rtnKey = rtnKey | Keys.Alt; }
                if (i == Keys.LShiftKey || i == Keys.RShiftKey) { rtnKey = rtnKey | Keys.Shift; }
            }
            return rtnKey | key;
        }

        private Boolean IsCtrlAltShiftKeys(Keys key) {
            if (key == Keys.LControlKey || key == Keys.RControlKey || key == Keys.LMenu || key == Keys.RMenu || key == Keys.LShiftKey || key == Keys.RShiftKey) { return true; }
            return false;
        }
    }
}

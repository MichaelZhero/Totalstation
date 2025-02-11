using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mear.Control
{
    class Serial
    {
        public SerialPort _serial = new SerialPort();
        private bool _flag = false;
        public Serial(string com , int BaudRate)
        {
            _serial.PortName = com;
            _serial.BaudRate = BaudRate;
            _serial.Parity = Parity.None;
            if (_serial.IsOpen)
                _flag = true;
            else
                _flag = false;
        }
        public Serial()
        {
            string[] Name = new string[10];
            Name = System.IO.Ports.SerialPort.GetPortNames();
            if (Name.Count() > 0)
            {
                _serial.PortName = Name[0];
                _serial.BaudRate = 115200;
                _flag = true;
            }
            else
            {
                _serial = null;
                _flag = false; 
            }

        }
        public bool set_port(string com , int rate)
        {
            _serial.PortName = com;
            _serial.BaudRate = rate;
            return true;
        }
        public string[] get_port()
        {
            string[] Name = new string[10];
            Name = System.IO.Ports.SerialPort.GetPortNames();
            return Name;
        }

        public bool open()
        {
            if(!_flag)
                return false;

            if (_serial.IsOpen)
                return true;
            else
                _serial.Open();
                return _serial.IsOpen;
        }

        public bool close()
        {
            try
            {
                _serial.Close();
                if (_serial.IsOpen)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }
        public void Dispose()
        {
            _serial.Dispose();
            GC.Collect();
        }

        public bool send(string text)
        {
            if (!_flag)
                return false;
            if (_serial.IsOpen)
                _serial.Write(text);
            else
                return false;
            return true;
        }
        int t = 4;
        public string actest(int i , int q)
        {
            return (i + t).ToString();
        }
        public string read()
        {
            if (!_flag)
                return null;
            if (_serial.IsOpen)
            {
                int count = _serial.BytesToRead;
                if (count <= 0)
                    return null;
                string recv = _serial.ReadExisting();
                recv = recv.Substring(0, recv.Length - 2);
                return recv;
            }
            else
                return null;
        }
    }
}

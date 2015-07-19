using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TestClient
{
    public class TextBoxTraceListener : TraceListener
    {
        private TextBox target;
        private StringSendDelegate invoke;

        public TextBoxTraceListener(TextBox target)
        {
            this.target = target;
            invoke = new StringSendDelegate(StringDelegate);
        }

        public override void Write(string text)
        {
            target.Dispatcher.Invoke(invoke, new object[] { text });
        }

        public override void WriteLine(string text)
        {
            target.Dispatcher.Invoke(invoke, new object[] { text + Environment.NewLine });
        }

        private delegate void StringSendDelegate(string text);
        private void StringDelegate(string text)
        {
            target.Text += text;
        }
    }
}

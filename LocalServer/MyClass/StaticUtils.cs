using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LocalServer.MyClass
{
    public class StaticUtils
    {
        public static readonly int DefaultPort = 8989;

        private static ListView mListView;

        public static void SetListView(ListView listView)
        {
            mListView = listView;
        }

        public static void WriteLine(String msg)
        {
            if (mListView != null)
            {
                mListView.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (mListView != null)
                    {
                        mListView.Items.Insert(0, DateTime.Now.ToString("[dd-MM-yyyy hh:mm:ss] ") + msg);
                    }
                }));
            }
        }

    }
}

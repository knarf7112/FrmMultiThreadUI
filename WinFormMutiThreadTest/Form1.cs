using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace WinFormMutiThreadTest
{
    public partial class Form1 : Form
    {
        private int fileCount = 0;
        private long totalSize = 0;
        private bool cancel = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtPath.Text = Path.GetTempPath();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //每隔50ms顯示最新時間於狀態列
            toolStripStatusLabel1.Text = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            ////重設統計數據
            //fileCount = 0;
            //totalSize = 0;
            ////統計檔案數及大小
            //explore(txtPath.Text);
            ////顯示結果
            //lbResult.Text = string.Format("{0:#,##0} files, {1:#,##0} bytes", fileCount, totalSize);
            //--------------------------------------------上面是舊的(同步方式)------------------------------------------------------
            if (btnOK.Text == "Go")
            {
                //將統計作業排入ThreadPool Queue
                //windows會另開Thread處理
                //UI Thread可以繼續處理UI畫面並與使用者互動
                ThreadPool.QueueUserWorkItem(new WaitCallback(startJob), txtPath.Text);//不給跨其他執行緒跑
                //在 Windows Form 的世界裡，有一條鐵律:
                //"Thou shall not access UI controls from a thread other than the one that created the UI control in the first place"
                //“除了建立 UI Control 的那條執行緒外，你不可以用其他執行緒去存取該 UI Control
                btnOK.Text = "Cancel";// Go ==變成==> Cancel
                cancel = false;
            }
            else
            {
                cancel = true;
                btnOK.Enabled = false;
            }
        }

        delegate void UpdateLabelHandler(string text);//委派改寫狀態時間
        //透過Invoke，printReulst會以UI Thread執行
        //故可隨意存取UI Control
        public void PrintResult(string text)
        {
            lbResult.Text = text;
            //作業完成後canel按鈕轉回Go   Cancel ==轉成==> Go
            btnOK.Text = "Go";
            btnOK.Enabled = true;
            txtPath.Text = Path.GetTempPath();
        }
        private void startJob(object path)
        {
            //重設統計數據
            fileCount = 0;
            totalSize = 0;
            //統計檔案數及大小
            explore(path.ToString());
            //顯示結果
            //lbResult.Text = string.Format("{0:#,##0} files, {1:#,##0} bytes", fileCount, totalSize);

            //所以透過Invoke,強制UI Thread執行
            string text = (!cancel) ? string.Format("{0:#,##0} files, {1:#,##0} bytes", fileCount, totalSize):"Canceled!!!";
            this.Invoke(
                new UpdateLabelHandler(PrintResult),//使用委派調用PrintResult方法來顯示遞迴完畢的資料
                new object[] { text }
                );
        }

        private void explore(string path)
        {
            //使用遞迴搜尋所有目錄
            foreach (string dir in Directory.GetDirectories(path))
            {
                if (cancel) return;//如果按取消則跳離
                explore(dir);
            }
            foreach (string file in Directory.GetFiles(path))
            {
                if (cancel) return;//if canceled leave this loop
                Interlocked.Increment(ref fileCount);//Interlocked.Increment增加fileCounter值的做法//防止多個Thread同時更動資料時發生衝突//fileCount++;
                //每100個檔案回報一次
                if (fileCount % 100 == 0)
                {
                    this.Invoke(
                        new UpdateLabelHandler(UpdateProcess),
                        string.Format("{0:#,##0} files processed ...",fileCount)
                        );
                    Thread.Sleep(2000);
                }
                //加總檔案大小
                FileInfo fi = new FileInfo(file);
                totalSize += fi.Length;
            }
        }
        private void UpdateProcess(string text)
        {
            txtPath.Text = text;
        }
    }
}

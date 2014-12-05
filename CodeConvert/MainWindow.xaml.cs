using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using System.IO;
using System.Diagnostics;
using System.Windows.Threading;
using System.Data.SqlClient;

namespace CodeConvert
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Convert convert = new Convert();    //创建一个Convert类实例
        DispatcherTimer timer = new DispatcherTimer();
        DirectoryInfo dirInfo;

        public MainWindow()
        {
            InitializeComponent();


            #region 其它
            int a = 255;            //转换成十六进制显示
            Debug.WriteLine(a.ToString("X"));

            //btn_StartTimer.Visibility = Visibility.Visible;
            timer.Tick += new EventHandler(timer_Tick);             //类似于WinForm中的Timer控件
            timer.Interval = TimeSpan.FromMilliseconds(100); ;
            #endregion
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            ProgressBar.Value += 1;
        }

        private void btn_SelectDir_Click(object sender, RoutedEventArgs e)
        {
            List<string> listFile = new List<string>();  //创建一个List泛型类用于储存文件扩展名
            System.Windows.Forms.FolderBrowserDialog fbd_SelectDir = new System.Windows.Forms.FolderBrowserDialog();//创建一个文件夹选择器

            if (fbd_SelectDir.ShowDialog() == System.Windows.Forms.DialogResult.OK)//如果已选择文件
            {
                Working.Content = fbd_SelectDir.SelectedPath;
                Debug.WriteLine("打开文件夹：" + fbd_SelectDir.SelectedPath);  //调试输出
                dirInfo = new DirectoryInfo(fbd_SelectDir.SelectedPath);
                FileInfo[] fileList = dirInfo.GetFiles();//获取文件

                foreach (FileInfo file in fileList)     //向listFile中添加文件
                {
                    string extension = file.Extension;
                    listFile.Add(extension);
                }

                //调用Distinct方法 通过使用默认的相等比较器对值进行比较返回序列中的非重复元素 
                foreach (var file in listFile.Distinct<string>().ToList())
                {
                    Debug.Write(file + "\t");
                }

                //System.Windows.Forms.BindingSource bs = new System.Windows.Forms.BindingSource();   //数据绑定到ListBox
                //bs.DataSource = listFile;
                cb_FileList.ItemsSource = listFile.Distinct<string>().ToList();

                cb_FileList.IsEnabled = true;
            }

        }

        private void btn_Convert_Click(object sender, RoutedEventArgs e)
        {
            List<FileInfo> filterList = dirInfo.GetFiles("*"+cb_FileList.SelectedValue.ToString()).ToList<FileInfo>();
            Debug.WriteLine(cb_FileList.SelectedItem);
            foreach (var item in filterList)
            {
                Debug.WriteLine(item.FullName);
                convert.File = item;
                Status.Content = "转换中";
                convert.CheckCode();
                convert.ConvertCode();
                convert.CreateFile(false);//true:backup
                Status.Content = "已完成";
            }

            
            cb_FileList.IsEnabled = false;
            btn_Convert.IsEnabled = false;
        }

        private void btn_SelectFile_Click(object sender, RoutedEventArgs e)
        {
            string filePath = null;
            OpenFileDialog ofdSelectFile = new OpenFileDialog();

            /* 操作多个文件 
             * string[] files;   
             * ofdSelectFile.Multiselect = true;//可以多选
             */

            if (ofdSelectFile.ShowDialog() == true)
            {
                filePath = ofdSelectFile.FileName;
                Debug.WriteLine("打开文件：" + ofdSelectFile.FileName);
                FileInfo file = new System.IO.FileInfo(filePath);           //创建一个FileInfo实例
                //files = ofdSelectFile.FileNames;          //操作多文件
                Working.Content = file;

                convert.File = file;
                Status.Content = "转换中";
                convert.CheckCode();
                convert.ConvertCode();

                convert.CreateFile(false);//true:backup
                Status.Content = "已完成";

            }
        }

        private void cb_FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_FileList.SelectedIndex != -1)
            {
                btn_Convert.IsEnabled = true;
            }
            else
                btn_Convert.IsEnabled = false;
        }

        private void btn_StartTimer_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
        }

        private void RB1_Checked(object sender, RoutedEventArgs e)
        {
            convert.NewCode = Encoding.Default;
        }

        private void RB2_Checked(object sender, RoutedEventArgs e)
        {
            convert.NewCode = Encoding.Unicode;
        }

        private void RB3_Checked(object sender, RoutedEventArgs e)
        {
            convert.NewCode = Encoding.UTF8;
        }

        private void menu_History_Click(object sender, RoutedEventArgs e)
        {
            menu_History.Items.Clear();         //清理Items
            //Convert.ConnectSQL();
            string conStr = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=""C:\Users\Chen\Documents\Visual Studio 2013\Projects\CodeConvert\CodeConvert\History.mdf"";Integrated Security=True";
            SqlConnection conn = new SqlConnection(conStr);
            conn.Open();
            Debug.WriteLine("数据库建立连接！" + conn.ConnectionString.ToString());

            SqlCommand cmd = new SqlCommand("select * from History", conn);//读历史记录
            SqlDataReader sdr = cmd.ExecuteReader();
            while (sdr.Read())
            {
                Debug.WriteLine(sdr["FileName"].ToString());

                MenuItem menuItem = new MenuItem();
                menuItem.Header = sdr["FileName"].ToString();
                menu_History.Items.Add(menuItem);               //History菜单添加新项
            }
            /*使用SqlDataAdapter DataSet为DataGrid空间绑定数据
            SqlDataAdapter sda = new SqlDataAdapter("select * from History", conn);
            System.Data.DataSet ds = new System.Data.DataSet();
            sda.Fill(ds);
            dataGrid.ItemsSource = ds.Tables[0].DefaultView;
            */
            sdr.Close();
            conn.Close();
        }

        private void menu_About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("11101113 田祖宸", "关于");
        }

        private void menu_Clear_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(Directory.GetCurrentDirectory());
            string conStr = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=""C:\Users\Chen\Documents\Visual Studio 2013\Projects\CodeConvert\CodeConvert\History.mdf"";Integrated Security=True";
            SqlConnection conn = new SqlConnection(conStr);
            conn.Open();
            try
            {
                SqlCommand deleteCmd = new SqlCommand("delete from History", conn);//读历史记录
                MessageBox.Show("已删除" + deleteCmd.ExecuteNonQuery() + "条记录！\n下次运行时生效！");
            }
            catch (Exception e2)
            {
                MessageBox.Show(e2.Message, "Error");
            }
            finally
            {
                conn.Close();
            }
        }

        private void menu_History_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            menu_History.Items.Clear();         //清理Items
            //Convert.ConnectSQL();
            string conStr = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=""C:\Users\Chen\Documents\Visual Studio 2013\Projects\CodeConvert\CodeConvert\History.mdf"";Integrated Security=True";
            SqlConnection conn = new SqlConnection(conStr);
            conn.Open();
            Debug.WriteLine("数据库建立连接！" + conn.ConnectionString.ToString());

            SqlCommand cmd = new SqlCommand("select * from History", conn);//读历史记录
            SqlDataReader sdr = cmd.ExecuteReader();
            while (sdr.Read())
            {
                Debug.WriteLine(sdr["FileName"].ToString());

                MenuItem menuItem = new MenuItem();
                menuItem.Header = sdr["FileName"].ToString();
                menu_History.Items.Add(menuItem);               //History菜单添加新项
            }
            /*使用SqlDataAdapter DataSet为DataGrid空间绑定数据
            SqlDataAdapter sda = new SqlDataAdapter("select * from History", conn);
            System.Data.DataSet ds = new System.Data.DataSet();
            sda.Fill(ds);
            dataGrid.ItemsSource = ds.Tables[0].DefaultView;
            */
            sdr.Close();
            conn.Close();
        }

    }
}

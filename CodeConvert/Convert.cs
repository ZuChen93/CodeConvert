using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Windows;
namespace CodeConvert
{
    class Convert
    {
        public FileInfo File { get; set; }//待处理文件
        public Encoding OriginalCode { get; set; }//文件原始编码
        public Encoding NewCode { get; set; }//文件新编码
        private char[] charData;
        public Convert()
        {

        }

        public Convert(FileInfo file)
        {
            File = file;
        }

        private Convert(Encoding e)
        {
            NewCode = e;
        }

        /// <summary>
        /// 检验文件编码
        /// </summary>
        public Encoding CheckCode()
        {
            /* 检测源文件编码格式
             * ANSI：                无格式定义
             * Unicode：             前两个字节为  FFFE
             * Unicode big endian：  前两字节为    FEFF
             * UTF-8：               前两字节为    EFBB
             */
            using (BinaryReader br = new BinaryReader(File.OpenRead()))
            {
                Byte[] buffer = br.ReadBytes(2);
                //Debug.Write(buffer[0].ToString("X") + " " + buffer[1].ToString("X"));//调试输出
                if (buffer[0] == 0xEF && buffer[1] == 0xBB)
                {
                    OriginalCode = Encoding.UTF8;
                    return System.Text.Encoding.UTF8;
                }
                else if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                {
                    OriginalCode = Encoding.BigEndianUnicode;
                    return System.Text.Encoding.BigEndianUnicode;
                }
                else if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                {
                    OriginalCode = Encoding.Unicode;
                    return System.Text.Encoding.Unicode;
                }
                else
                {
                    OriginalCode = Encoding.Default;
                    return System.Text.Encoding.Default;
                }
            }
        }

        /// <summary>
        /// 编码转换
        /// </summary>
        public void ConvertCode()
        {
            byte[] byteData = new byte[File.Length];
            char[] charData = new char[File.Length];
            try
            {
                CheckCode();
                using (FileStream fs = File.OpenRead())     //转码
                {
                    fs.Read(byteData, 0, (int)File.Length);
                    Decoder d = OriginalCode.GetDecoder();
                    d.GetChars(byteData, 0, byteData.Length, charData, 0, true);

                    foreach (var item in charData)//调试输出
                    {
                        Debug.Write(item);
                    }
                    this.charData = charData;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error!");

            }
        }

        /// <summary>
        /// 创建转换编码后的新文件
        /// </summary>
        public void CreateFile(bool isBackup)
        {
            byte[] data = new byte[File.Length * 2];
            using (FileStream fs = File.OpenWrite())
            {
                if (NewCode != null)
                {
                    if (isBackup == true)
                        FileBackup();

                    Encoder e = NewCode.GetEncoder();
                    e.GetBytes(charData, 0, charData.Length, data, 0, true);
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Write(data, 0, data.Length);
                    //fs.Close();

                    CreateSQL();//创建一条历史记录
                }
                else
                    MessageBox.Show("请先选择要转换的编码格式！", "Error");
            }
        }

        /// <summary>
        /// 备份文件
        /// </summary>
        public void FileBackup()
        {
            File.MoveTo(File + ".bak");
        }

        /// <summary>
        /// 向数据库中添加一条记录
        /// </summary>
        public void CreateSQL()
        {
            string conStr = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=""C:\Users\Chen\Documents\Visual Studio 2013\Projects\CodeConvert\CodeConvert\History.mdf"";Integrated Security=True";
            try
            {
                using (SqlConnection conn = new SqlConnection(conStr))
                {
                    conn.Open();
                    Debug.WriteLine("数据库建立连接！" + conn.ConnectionString.ToString());
                    //SqlCommand insert = new SqlCommand("insert into History(FileName) values('" + File.ToString() + "')", conn);
                    SqlCommand insert = new SqlCommand("insert into History(FileName) values(@FileName)", conn);
                    insert.Parameters.AddWithValue("@FileName", File.ToString());
                    insert.ExecuteNonQuery();//执行SQL查询命令
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
            }
        }
    }
}

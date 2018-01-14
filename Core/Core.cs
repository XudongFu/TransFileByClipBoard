using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Concurrent;

namespace Core
{
    #region 版本二
    public class Core2
    {
        public FileStream CopyToStream;

        public static int Interval = 150;

        string FileSafeName;

        public Thread CopyThread;

        public Core2(string fileName, FileStream stream)
        {
            this.CopyToStream = stream;
            FileSafeName = fileName;
        }

        public event EventHandler<ProgressInfo> CopyProgressGo;
        public event EventHandler<ProgressInfo> ReadProgressGo;
        public event EventHandler<PartFileLost> FileLost;


        public class ProgressInfo:EventArgs
        {
            public long value;
            public long max;
            public string json;
        }

        public class PartFileLost : EventArgs
        {
            public long Index;
            public long length;
        }
        

        public void CopyToBoard()
        {
            CopyThread = new Thread(new ThreadStart(delegate () {
                while (CopyToStream.Position < CopyToStream.Length - 1)
                {
                    PartFileInfo xx = new PartFileInfo()
                    {
                        fileName = FileSafeName,
                        Index = CopyToStream.Position,
                        FileLength = CopyToStream.Length
                    };
                    xx.PartFileLength = CopyToStream.Read(xx.data, 0, PartFileInfo.BufferSize);
                    PartFileInfo.CopyToClipBoard(xx);
                    CopyProgressGo?.Invoke(this, new ProgressInfo() { value = xx.Index, max = xx.FileLength });
                    Thread.Sleep(Interval);
                }
                CopyProgressGo?.Invoke(this, new ProgressInfo() { value = 1, max = 1 });
            }));
            CopyThread.SetApartmentState(ApartmentState.STA);
            //thread.IsBackground = true;
            CopyThread.Start();
        }

        
/// //////////////////////////////////////////////////////


        private Stream WriteToStream;
        private const int bufferlength=50;
        string Path = string.Empty;
        public List<PartFileInfo> FileBufferList = new List<PartFileInfo>(bufferlength);
        bool first = true;
        public Thread ReadThread;

        PartFileInfo last;

        ConcurrentQueue<string> fromBoard = new ConcurrentQueue<string>();
        string lastString = string.Empty;

        public Core2(string Path)
        {
            this.Path = Path; 
        }

        public void WriteToFile()
        {

            Thread GetFromBoard = new Thread( new ThreadStart(delegate()
            {
                string str= Clipboard.GetText();
                while (true)
                {
                    if (str != lastString)
                    {
                        fromBoard.Enqueue(str);
                        lastString = str;
                    }
                    str = Clipboard.GetText();
                }
            }) );
            GetFromBoard.SetApartmentState( ApartmentState.STA);
            GetFromBoard.Start();
            ReadThread = new Thread(new ThreadStart(delegate ()
            {
                Clipboard.SetText("123");
                //string str= Clipboard.GetText();
                string str;
                fromBoard.TryDequeue(out str);
                var partFile = PartFileInfo.Prase(str);
                while (true)
                {
                    if (partFile.Check())
                    {
                        if (first)
                        {
                            first = false;
                            WriteToStream = File.Create(Path + "//" + partFile.fileName);
                        }
                       
                        if (FileBufferList.Count == bufferlength)
                        {
                            FileBufferList.ForEach(p =>
                            {
                                WriteToStream.Write(p.data, 0, (int)p.PartFileLength);
                            });
                            FileBufferList.Clear();
                        }

                        if (FileBufferList.Count == 0 && partFile.Index == 0)
                        {
                            FileBufferList.Add(partFile);
                            last = partFile;
                            ReadProgressGo?.Invoke(this, new ProgressInfo()
                            {
                                value = 0,
                                max = partFile.FileLength
                            });
                        }
                        else
                        {
                            if (last.Index + last.PartFileLength == partFile.Index)
                            {
                                FileBufferList.Add(partFile);
                                last = partFile;
                                ReadProgressGo?.Invoke(this, new ProgressInfo()
                                {
                                    value = partFile.Index,
                                    max = partFile.FileLength
                                });

                                if (partFile.Index + partFile.PartFileLength == partFile.FileLength)
                                {
                                    FileBufferList.ForEach(p =>
                                    {
                                        WriteToStream.Write(p.data, 0, (int)p.PartFileLength);
                                    });
                                    FileBufferList.Clear();
                                    break;
                                }
                            }
                            else
                            {
                                if (last.Index + last.PartFileLength < partFile.Index)
                                {
                                    FileLost?.Invoke(this, new PartFileLost()
                                    {
                                        Index = last.Index + last.PartFileLength,
                                        length = partFile.Index - (last.Index + last.PartFileLength)
                                    });
                                }
                                //else
                                //    throw new Exception("文件传输错误，建议重新传输");
                            }
                        }
                        
                    }
                    fromBoard.TryDequeue(out str);
                    partFile = PartFileInfo.Prase(str);
                }
                WriteToStream.Flush();
                WriteToStream.Close();
                if (GetFromBoard!=null && GetFromBoard.IsAlive)
                {
                    GetFromBoard.Abort();
                }
                //文件传输完毕
                ReadProgressGo?.Invoke(this, new ProgressInfo()
                {
                    value = 1,
                    max = 1
                });

            }));
            ReadThread.IsBackground = true;
            ReadThread.SetApartmentState(ApartmentState.STA);
            ReadThread.Start();
        }

    }
    
    [DataContract]
    public class PartFileInfo
    {
        public static int BufferSize = 1024 * 120;//80kb;

        [DataMember]
        public string fileName;

        [DataMember]
        public long FileLength;

        [DataMember]
        public long Index;

        [DataMember]
        public long PartFileLength;

        [DataMember]
        public byte[] data;


        public PartFileInfo()
        {
            data = new byte[BufferSize];
        }

        public bool Check()
        {
            if (  data!=null && data.Length==PartFileLength )
            {
                return true;
            }
            return false;
        }

        public static void CopyToClipBoard(PartFileInfo pile)
        {
            string temp = pile.ToString();
            Clipboard.SetText(temp);
        }

        public override string ToString()
        {
            if ( PartFileLength <data.Length )
            {
                data = data.Take((int)PartFileLength).ToArray();
            }

            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(PartFileInfo));
            MemoryStream msObj = new MemoryStream();
            //将序列化之后的Json格式数据写入流中
            js.WriteObject(msObj, this);
            msObj.Position = 0;
            //从0这个位置开始读取流中的数据
            StreamReader sr = new StreamReader(msObj, Encoding.UTF8);
            string json = sr.ReadToEnd();
            sr.Close();
            msObj.Close();
            return json;
        }

        public static PartFileInfo  Prase(string str)
        {
            try
            {
                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(PartFileInfo));
                MemoryStream msObj = new MemoryStream(UTF8Encoding.UTF8.GetBytes(str));
                //将序列化之后的Json格式数据写入流中
                var filePile = (PartFileInfo)js.ReadObject(msObj);
                msObj.Close();
                return filePile;
            }
            catch
            {
                return new PartFileInfo();
            }
        }


    }
    #endregion

}

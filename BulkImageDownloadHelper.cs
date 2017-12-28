using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

/*
 * 用例
 *              //地址头
                var Host = replaceString;
                //物理地址
                var ToFile = IEnumerableHelper.GetAppSettingsValue("Imagefile");
                //虚拟存放地址
                var VirtualDirectory = $@"/PFImage/Product/{ DateTime.Now.Year}/{ DateTime.Now.Month}/{ DateTime.Now.Day}";
                using (var bulk = new BulkDownloadHelper())
                {
                    var downurl = bulk.BulkDownLoad(Host).BulkDownLoadUrls(images.Select(s => s.Src).ToArray());
                    if (downurl.Count > 0)
                        bulk.CopyToFolder(ToFile + VirtualDirectory);
                    foreach (var file in downurl)
                    {
                        var size = file.bytes.byteToImage();
                        var itemimage = images.Where(s => s.Src == file.DownLoadingPath).FirstOrDefault();
                        if (itemimage != null)
                        {
                            itemimage.Path = file.DownloadingLocalPath.Replace(ToFile, string.Empty);
                            itemimage.Md5 = file.Md5;
                            itemimage.Height = size.Height;
                            itemimage.Width = size.Width;
                        }
                    }
                }
 */

/// <summary>
/// 类名称：BulkImageDownloadHelper
/// 命名空间：Tidebuy.Platform.Utility.Tools
/// 类功能：批量下载图片
/// 外部资源：System.IO.Compression.FileSystem.dll
/// </summary>
/// 创建者：万浩
/// 创建日期：2017/09/30 09:14
/// 修改者：
/// 修改时间：
/// ----------------------------------------------------------------------------------------
public class BulkDownloadHelper : IDisposable
{
    /// <summary>
    /// 下载之后所有文件夹路径  KEY 一个文件夹  Value  Key 下载地址    Value 下载完成地址
    /// </summary>
    private readonly List<ListDownloadFile> _items = new List<ListDownloadFile>();
    //单独下载
    List<DownloadFileUrl> _urls = new List<DownloadFileUrl>();
    /// <summary>
    /// 所有需要下载的图片
    /// </summary>
    private DownDownFile _DownDownFile = new DownDownFile();
    /// <summary>
    /// 下载对象
    /// </summary>
    private readonly HttpClient _client = new HttpClient();
    /// <summary>
    /// 物理完整目录
    /// </summary>
    public static string PhysicalDirectory = System.AppDomain.CurrentDomain.BaseDirectory; //HttpRuntime.AppDomainAppPath;//Directory.GetCurrentDirectory();
    /// <summary>
    /// 临时文件夹
    /// </summary>
    readonly string _filePath = PhysicalDirectory + "\\TempFolder\\" + Guid.NewGuid() + "\\";
    /// <summary>
    /// 虚拟下载目录
    /// </summary>
    static string _VirtualfilePathDown = "\\TempFolder\\Down\\";
    /// <summary>
    /// 下载文件夹
    /// </summary>
    readonly string _filePathDown = PhysicalDirectory + _VirtualfilePathDown;
    /// <summary>
    /// 字段_zipFileName
    /// </summary>
    /// 创建者：万浩
    /// 创建日期：2017/09/30 14:06
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    private string _zipFileName;
    /// <summary>
    /// 压缩过后的文件夹路径
    /// </summary>
    public string ZipFileName => _zipFileName;
    /// <summary>
    /// 压缩文件Byte
    /// </summary>
    /// 创建者：万浩
    /// 创建日期：2017/10/28 14:20
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    private byte[] _ZipByte;
    /// <summary>
    /// 调用 ZipToByte 获得压缩文件byte 二进制
    /// </summary>
    /// 创建者：万浩
    /// 创建日期：2017/10/28 14:20
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    public byte[] ZipByte => _ZipByte;
    /// <summary>
    /// 创建文件夹
    /// </summary>
    /// 创建者：万浩
    /// 创建日期：2017/09/30 09:17
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    private void Createfile(string name)
    {
        if (!Directory.Exists(_filePathDown))
            Directory.CreateDirectory(_filePathDown);
        if (!Directory.Exists(_filePath + name))
            Directory.CreateDirectory(_filePath + name);
    }
    /// <summary>
    /// 删除文件夹
    /// </summary>
    /// 创建者：万浩
    /// 创建日期：2017/09/30 09:48
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    private void Deletefile()
    {
        if (!Directory.Exists(_filePath)) return;
        var di = new DirectoryInfo(_filePath);
        foreach (var file in di.GetFiles())
            file.Delete();
        foreach (var dir in di.GetDirectories())
            dir.Delete(true);
        di.Delete(true);
    }
    /// <summary>
    /// 批量下载图片
    /// 这个只是临时下载文件，完成后会自动删除文件
    /// 需要永久保存则继续调用（CopyToFolder）方法即可立即永久保存
    /// 会自动清理图片和文件夹需要的话请立即转存文件
    /// </summary>
    /// <param name="file">图片</param>
    /// <returns>
    /// 返回值：BulkImageDownloadHelper
    /// </returns>
    /// 创建者：万浩
    /// 创建日期：2017/09/30 10:16
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    public BulkDownloadHelper BulkDownLoad(DownDownFile file)
    {
        if (file == null && file.FileList == null)
            throw new ArgumentNullException("file 不能为空！");
        if (string.IsNullOrEmpty(file.HostName))
            throw new ArgumentNullException("HostName 不能为空！");
        _DownDownFile = file;
        return this;
    }
    /// <summary>
    /// http://127.0.0.1 或完整物理路径 D:\文件\
    /// </summary>
    /// <param name="hostName"></param>
    /// <returns></returns>
    public BulkDownloadHelper BulkDownLoad(string hostName)
    {
        if (string.IsNullOrEmpty(hostName))
            throw new ArgumentNullException("HostName 不能为空！");
        _DownDownFile = new DownDownFile
        {
            HostName = hostName,
            FileList = new List<BulkDownFile>()
        };
        return this;
    }
    /// <summary>
    /// 执行批量下载图片 序号_文件名称.图片类型
    /// </summary>
    /// <returns>
    /// 返回值：List&lt;ListDownloadFile&gt;
    /// </returns>
    /// 创建者：万浩
    /// 创建日期：2017/12/06 10:09
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    /// <exception cref="ArgumentNullException">
    /// 请先执行【BulkDownLoad】方法！
    /// or
    /// FileName：不能为空！
    /// or
    /// HostName：不能为空！
    /// </exception>
    public List<ListDownloadFile> BulkDownLoadSerialNumber()
    {
        if (_DownDownFile == null)
            throw new ArgumentNullException("请先执行【BulkDownLoad】方法！");
        foreach (var item in _DownDownFile.FileList)
        {
            var i = 0;
            if (string.IsNullOrEmpty(item.FileName))
                throw new ArgumentNullException("FileName：不能为空！");
            if (string.IsNullOrEmpty(_DownDownFile.HostName))
                throw new ArgumentNullException("HostName：不能为空！");
            var listDownloadFile = new ListDownloadFile
            {
                Folder = item.FileName
            };
            Createfile(item.FileName);
            foreach (var url in item.Urls)
            {
                var downloadFileUrl = new DownloadFileUrl
                {
                    DownloadingLocalPath = string.Empty,
                    DownLoadingPath = url,
                    FullFileName = Path.GetFileName(url)
                };
                var fileName = string.Format("{0}{1}/{2}_{3}.{4}", _filePath, item.FileName, i, downloadFileUrl.FileName, downloadFileUrl.FileSuffixes);
                downloadFileUrl.DownloadingLocalPath = fileName;
                downloadFileUrl.DownloadingLocalVirtualDirectoryPath = fileName.Replace(_filePath, string.Empty);
                var file = Down(_DownDownFile.HostName + url, fileName);
                if (file.Item3)
                {
                    downloadFileUrl.Md5 = file.Item1;
                    downloadFileUrl.bytes = file.Item2;
                }
                i++;
                listDownloadFile.DownloadFileUrl.Add(downloadFileUrl);
            }
            _items.Add(listDownloadFile);
        }
        return _items;
    }
    /// <summary>
    /// 批量下载图片 文件名称.图片类型
    /// </summary>
    /// <returns>
    /// The List&lt;ListDownloadFile&gt;
    /// </returns>
    /// 创建者：万浩
    /// 创建日期：2017/12/06 10:08
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    /// <exception cref="ArgumentNullException">
    /// FileName：不能为空！
    /// or
    /// HostName：不能为空！
    /// </exception>
    public List<ListDownloadFile> BulkDownLoadSourceName()
    {
        foreach (var item in _DownDownFile.FileList)
        {
            if (string.IsNullOrEmpty(item.FileName))
                throw new ArgumentNullException("FileName：不能为空！");
            if (string.IsNullOrEmpty(_DownDownFile.HostName))
                throw new ArgumentNullException("HostName：不能为空！");
            var listDownloadFile = new ListDownloadFile
            {
                Folder = item.FileName
            };
            Createfile(item.FileName);
            foreach (var url in item.Urls)
            {
                var downloadFileUrl = new DownloadFileUrl
                {
                    DownloadingLocalPath = string.Empty,
                    DownLoadingPath = url,
                    FullFileName = Path.GetFileName(url)
                };
                var fileName = string.Format("{0}{1}/{2}.{3}", _filePath, item.FileName, downloadFileUrl.FileName, downloadFileUrl.FileSuffixes);
                downloadFileUrl.DownloadingLocalPath = fileName;
                downloadFileUrl.DownloadingLocalVirtualDirectoryPath = fileName.Replace(_filePath, string.Empty);
                var file = Down(_DownDownFile.HostName + url, fileName);
                if (file.Item3)
                {
                    downloadFileUrl.Md5 = file.Item1;
                    downloadFileUrl.bytes = file.Item2;
                }
                listDownloadFile.DownloadFileUrl.Add(downloadFileUrl);
            }
            _items.Add(listDownloadFile);
        }
        return _items;
    }
    /// <summary>
    /// URL普通下载
    /// </summary>
    /// <returns></returns>
    public List<DownloadFileUrl> BulkDownLoadUrls(params string[] urls)
    {
        if (urls == null && urls.Count() <= 0)
            throw new ArgumentNullException("Urls 不能为空！");
        Createfile(string.Empty);
        for (int i = 0; i < urls.Length; i++)
        {
            var downloadFileUrl = new DownloadFileUrl
            {
                DownloadingLocalPath = string.Empty,
                DownLoadingPath = urls[i],
                FullFileName = Path.GetFileName(urls[i])
            };
            var fileName = string.Format("{0}/{1}.{2}", _filePath, downloadFileUrl.FileName, downloadFileUrl.FileSuffixes);
            downloadFileUrl.DownloadingLocalPath = fileName;
            downloadFileUrl.DownloadingLocalVirtualDirectoryPath = fileName.Replace(_filePath, string.Empty);
            var file = Down(_DownDownFile.HostName + urls[i], fileName);
            if (file.Item3)
            {
                downloadFileUrl.Md5 = file.Item1;
                downloadFileUrl.bytes = file.Item2;
                _urls.Add(downloadFileUrl);
            }
        }
        return _urls;
    }


    /// <summary>
    /// 压缩文件 KeyValuePair&lt;虚拟地址, 物理地址&gt;
    /// </summary>
    /// <returns>
    /// 返回值：KeyValuePair&lt;虚拟地址, 物理地址&gt;
    /// </returns>
    /// 创建者：万浩
    /// 创建日期：2017/12/06 15:27
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    /// <exception cref="ArgumentNullException">下载文件夹请设置初始值！</exception>
    public KeyValuePair<string, string> ZipFileFolder()
    {
        if (string.IsNullOrEmpty(_filePathDown))
            throw new ArgumentNullException("下载文件夹请设置初始值！");
        _zipFileName = $"{_filePathDown}{DateTime.Now:yyyyMMddHHmmssfffff}.zip";
        ZipFile.CreateFromDirectory(_filePath, _zipFileName);
        return new KeyValuePair<string, string>(_zipFileName.Replace(PhysicalDirectory, string.Empty), _zipFileName);
    }
    /// <summary>
    /// 将ZIP转换为Byte数据
    /// </summary>
    /// <returns></returns>
    public BulkDownloadHelper ZipToByte()
    {
        if (string.IsNullOrEmpty(ZipFileName))
            _ZipByte = File.ReadAllBytes(ZipFileName);
        return this;
    }
    /// <summary>
    /// 删除ZIP文件
    /// </summary>
    /// 创建者：万浩
    /// 创建日期：2017/10/28 14:22
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    public void DelZip() => File.Delete(ZipFileName);
    /// <summary>
    /// 移动到指定文件夹
    /// </summary>
    /// <returns>
    /// 返回值：BulkImageDownloadHelper
    /// </returns>
    /// 创建者：万浩
    /// 创建日期：2017/09/30 14:42
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    public BulkDownloadHelper CopyToFolder(string url)
    {
        if (_items.Count <= 0 && _urls.Count <= 0)
            throw new ArgumentNullException("请先下载文件！");
        if (string.IsNullOrEmpty(url))
            throw new ArgumentNullException("url 不能为为空！");
        CopyTo(_filePath, url);
        if (_items.Count > 0)
        {
            _items.ForEach(s =>
            {
                s.DownloadFileUrl.ForEach(_u =>
                {
                    _u.DownloadingLocalPath = _u.DownloadingLocalPath.Replace(_filePath, url);
                    _u.DownloadingLocalVirtualDirectoryPath = _u.DownloadingLocalPath.Replace(url, string.Empty);
                });
            });
        }
        if (_urls.Count > 0)
        {
            _urls.ForEach(s =>
            {
                s.DownloadingLocalPath = s.DownloadingLocalPath.Replace(_filePath, url);
                s.DownloadingLocalVirtualDirectoryPath = s.DownloadingLocalPath.Replace(url, string.Empty);
            });
        }
        return this;
    }
    /// <summary>
    /// 移动文件
    /// </summary>
    /// <param name="fromDir"></param>
    /// <param name="toDir"></param>
    private void CopyTo(string fromDir = "", string toDir = "")
    {
        if (!Directory.Exists(fromDir))
            throw new ArgumentNullException("fromDir 不存在！");
        if (!Directory.Exists(toDir))
            Directory.CreateDirectory(toDir);

        var files = Directory.GetFiles(fromDir);
        foreach (var formFileName in files)
        {
            var fileName = Path.GetFileName(formFileName);
            var toFileName = Path.Combine(toDir, fileName);
            if (File.Exists(toFileName))
                File.Delete(toFileName);
            File.Copy(formFileName, toFileName);
        }
        var fromDirs = Directory.GetDirectories(fromDir);
        foreach (var fromDirName in fromDirs)
        {
            var dirName = Path.GetFileName(fromDirName);
            if (dirName == null) continue;
            var toDirName = Path.Combine(toDir, dirName);
            CopyTo(fromDirName, toDirName);
        }
    }

    static readonly object lockDown = new object();

    /// <summary>
    /// 下载图片
    /// </summary>
    /// <param name="url">The url</param>
    /// <param name="fileName">文件路径+文件名称  完整路径</param>
    /// <returns>
    /// 返回值：String MD5  byte[] 二进制文件，是否下载成功
    /// </returns>
    public Tuple<string, byte[], bool> Down(string url, string fileName)
    {
        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
        {
            var httpResponse = _client.SendAsync(request).Result.Content;
            var content_Type = httpResponse.Headers.Where(s => s.Key.ToLower().Equals("Content-Type".ToLower())).ToList().FirstOrDefault();
            if (content_Type.Key != null || content_Type.Value != null)
            {
                if (content_Type.Value.Any(s => s.ToLower().Contains("image".ToLower())))
                {
                    var contentStream = httpResponse.ReadAsStreamAsync().Result;
                    var stream = new FileStream(fileName,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.ReadWrite);
                    contentStream.CopyTo(stream);
                    contentStream.Close();
                    contentStream.Dispose();
                    stream.Close();
                    stream.Dispose();
                    using (FileStream fStream = File.OpenRead(fileName))
                    {
                        byte[] bytes = File.ReadAllBytes(fileName);
                        return Tuple.Create(GetHash<MD5>(fStream), bytes, true);
                    }
                }
            }
            return Tuple.Create(string.Empty, new byte[0], false);
        }
    }
    /// <summary>
    /// Gets the hash.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream">The stream</param>
    /// <returns>
    /// 返回值：String
    /// </returns>
    /// 创建者：万浩
    /// 创建日期：2017/12/06 11:41
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    public static string GetHash<T>(Stream stream) where T : HashAlgorithm
    {
        StringBuilder sb = new StringBuilder();
        MethodInfo create = typeof(T).GetMethod("Create", new Type[] { });
        using (T crypt = (T)create.Invoke(null, null))
        {
            byte[] hashBytes = crypt.ComputeHash(stream);
            foreach (byte bt in hashBytes)
            {
                sb.Append(bt.ToString("x2"));
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// 执行与释放或重置非托管资源相关的应用程序定义的任务。
    /// </summary>
    /// 创建者：万浩
    /// 创建日期：2017/09/30 09:46
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    public void Dispose()
    {
        //释放请求
        _client.Dispose();
        //删除文件夹
        Deletefile();
        //清空项
        _items.Clear();
    }
}

/// <summary>
/// 类名称：DownImage
/// 命名空间：Tidebuy.Platform.Utility.Tools
/// 类功能：下载图片类
/// </summary>
/// 创建者：万浩
/// 创建日期：2017/09/30 10:01
/// 修改者：
/// 修改时间：
/// ----------------------------------------------------------------------------------------
public class DownDownFile
{
    /// <summary>
    /// 初始化一个新的实例DownDownFile
    /// </summary>
    /// 创建者：万浩
    /// 创建日期：2017/12/06 15:01
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    public DownDownFile()
    {
        FileList = new List<BulkDownFile>();
    }
    /// <summary>
    /// 外部资源需要加 http://127.0.0.1
    /// 最终URL ： hostName + url 成为下载地址 
    /// </summary>   
    public string HostName { get; set; }
    /// <summary>
    /// 所有图片
    /// </summary>
    public List<BulkDownFile> FileList { get; set; }
}
/// <summary>
/// 类名称：BulkImage
/// 命名空间：Tidebuy.Platform.Utility.Tools
/// 类功能：文件夹名称
/// </summary>
/// 创建者：万浩
/// 创建日期：2017/09/30 09:52
/// 修改者：
/// 修改时间：
/// ----------------------------------------------------------------------------------------
public class BulkDownFile
{
    /// <summary>
    /// 文件夹名称
    /// </summary>
    public string FileName { get; set; }
    /// <summary>
    /// 外部资源地址
    /// </summary>
    public string[] Urls { get; set; }
}

/// <summary>
/// 类名称：下载列表
/// 命名空间：
/// 类功能：
/// </summary>
public class ListDownloadFile
{
    /// <summary>
    /// 初始化一个新的实例ListDownload
    /// </summary>
    /// 创建者：万浩
    /// 创建日期：2017/12/05 19:07
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    public ListDownloadFile()
    {
        DownloadFileUrl = new List<DownloadFileUrl>();
    }
    /// <summary>
    /// 文件夹
    /// </summary>
    public string Folder { get; set; }
    /// <summary>
    /// 下载文件地址
    /// </summary>
    public List<DownloadFileUrl> DownloadFileUrl { get; set; }
}


/// <summary>
/// 类名称：下载文件地址
/// 命名空间：
/// 类功能：
/// </summary>
public class DownloadFileUrl
{
    /// <summary>
    /// 文件名称
    /// </summary>
    /// <value>
    /// The name of the file.
    /// </value>
    public string FileName
    {
        get
        {
            if (string.IsNullOrEmpty(FullFileName) && !FullFileName.Contains("."))
                return string.Empty;
            return FullFileName.Split('.')[0];
        }
    }
    /// <summary>
    /// 文件后缀
    /// </summary>
    public string FileSuffixes
    {
        get
        {
            if (string.IsNullOrEmpty(FullFileName) && !FullFileName.Contains("."))
                return string.Empty;
            return FullFileName.Split('.')[1];
        }
    }
    /// <summary>
    /// 完整文件名加后缀
    /// </summary>
    public string FullFileName { get; set; }
    /// <summary>
    /// 下载URL
    /// </summary>
    public string DownLoadingPath { get; set; }
    /// <summary>
    /// 下载本地URL
    /// </summary>
    public string DownloadingLocalPath { get; set; }
    /// <summary>
    /// 下载本地虚拟URL
    /// </summary>
    public string DownloadingLocalVirtualDirectoryPath { get; set; }
    /// <summary>
    /// 文件MD5
    /// </summary>
    /// 创建者：万浩
    /// 创建日期：2017/12/06 11:08
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    public string Md5 { get; set; }
    /// <summary>
    /// 文件流
    /// </summary>
    /// 创建者：万浩
    /// 创建日期：2017/12/06 13:19
    /// 修改者：
    /// 修改时间：
    /// ----------------------------------------------------------------------------------------
    public byte[] bytes { get; set; }
}

using Project.Admin.Filter;
using Project.Admin.Helper;
using Project.BLL.File;
using Project.BLL.Infrastructure;
using Project.DAL.ModelView;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using static System.String;
using File = Project.DAL.File;

namespace Project.Admin.Controllers
{
    public class FileController : Controller
    {
        private IFile _fileRepository;
        private string _root;
        private string _siteId;
        private string _mapPath;
        private Guid _userId;
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            _fileRepository = new FileRepository();
            if (Session["Site"] == null || Session["User"] == null) return;
            _siteId = Session["Site"].ToString();
            _mapPath = Server.MapPath("~/Data/");
            _root = _mapPath + Session["Site"];
            _userId = ((LogedUser) Session["User"]).Id;
            ViewBag.CDN = ConfigurationManager.AppSettings["CDN"];
        }

        public string RemoveAccent(string txt)
        {
            var bytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(txt);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }

        public string Slugify(string phrase)
        {
            var str = RemoveAccent(phrase).ToLower();
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[^a-z0-9\s-]", ""); // Remove all non valid chars          
            str = System.Text.RegularExpressions.Regex.Replace(str, @"\s+", " ").Trim(); // convert mulTypele spaces into one space  
            str = System.Text.RegularExpressions.Regex.Replace(str, @"\s", "-"); // //Replace spaces by dashes
            return str;
        }
        [CustomAuthorize("FileManagement")]
        public ActionResult Index()
        {
            try
            {
                var dir = "";
                if (Session["Dir"] != null && Request.QueryString["dir"] == null)
                    dir = Session["Dir"].ToString();
                else
                    dir = Request.QueryString["dir"] ?? "";

                Directory.CreateDirectory(_root);

                var isRoot = IsNullOrEmpty(dir);
                ViewBag.CKEditorFuncNum = Request.QueryString["CKEditorFuncNum"];
                Session["Dir"] = dir;

                return View(new FileModelView
                {
                    Folder = GetSubDirectories(dir),
                    Files = GetFiles(dir),
                    IsRoot = isRoot,
                    Location = HttpUtility.UrlEncode(dir).Replace("%2f", "/")
                });
            }
            catch (Exception ex)
            {
                return View();
            }
        }
        [CustomAuthorize("FileManagement")]
        public List<Folder> GetSubDirectories(string dir)
        {
            var dirEncoded = HttpUtility.UrlDecode(dir);
            var directory = new DirectoryInfo(_root + @"\" + dir);
            var directories = directory.GetDirectories();
            return directories.Select(subdirectory => new Folder
            {
                FolderName = subdirectory.Name,
                Location = dir,
                FolderNameEncoded = "/" + HttpUtility.UrlEncode(subdirectory.Name)
            }).ToList();
        }
        [CustomAuthorize("FileManagement")]
        public List<Files> GetFiles(string dir)
        {
            var type = GetContentType();
            dir = HttpUtility.UrlDecode(dir);
            var d = new DirectoryInfo(_root + @"\" + dir);
            var filesInFolder = d.GetFiles();
            return filesInFolder.Select(file => new Files
            {
                FileName = file.Name,
                Extension = file.Extension,
                Location = dir,
                Id = _fileRepository.GetFileByLocation(_siteId + dir + "/" + file.Name)?.Id ?? Guid.Empty,
                CreatedTime = file.CreationTime,
                ContentType = type.ContainsKey(file.Extension) ? type[file.Extension] : "file"
            }).ToList();
        }
        private Dictionary<string, string> GetContentType()
        {
            var contentType = new Dictionary<string, string>();
            contentType.Add(".jpg", "Image");
            contentType.Add(".jpeg", "Image");
            contentType.Add(".png", "Image");
            contentType.Add(".gif", "Image");
            contentType.Add(".bmp", "Image");
            contentType.Add(".doc", "file-word");
            contentType.Add(".docx", "file-word");
            contentType.Add(".xls", "file-excel");
            contentType.Add(".xlsx", "file-excel");
            contentType.Add(".pdf", "file-pdf");
            contentType.Add(".ppt", "file-powerpoint");
            contentType.Add(".pptx", "file-powerpoint");
            contentType.Add(".rar", "file-archive");
            contentType.Add(".zip", "file-archive");
            return contentType;
        }
        [CustomAuthorize("FileManagement")]
        public JsonResult UploadFileToWebSite(string dir)
        {
            if (!ModelState.IsValid)
                return Json(
                    new { Success = false, Title = "Uyarı!", Message = "Dosya yüklenirken bir hata oluştu", Type = "warning", Id = Guid.Empty, JsonRequestBehavior.AllowGet });

            try
            {
                var file = Request.Files[0];

                if (file != null && file.ContentLength > 0)
                {
                    var path = dir.Replace("/", @"\");

                    var fullPath = _root + path;
                    var ext = Path.GetExtension(file.FileName)?.ToLower();

                    var fileName = Slugify(file.FileName.Replace(ext, "")) + ext;
                    var di = Directory.CreateDirectory(fullPath);

                    file.SaveAs(fullPath + "\\" + fileName);

                    var f = new File()
                    {
                        Id = Guid.NewGuid(),
                        Location = "~/Data/",
                        FileLocation = _siteId + path.Replace("\\", "/") + "/",
                        FileName = fileName,
                        DefaultFileName = file.FileName,
                        Extension = ext,
                        Download = 0,
                        ContentType = file.ContentType,
                        ContentLength = file.ContentLength,
                        CreatedUserId = _userId
                    };

                    _fileRepository.Insert(f);

                    return Json(new { Success = true, Title = "Başarılı!", Message = "Dosya başarı ile yüklendi", Type = "success", Id = f.Id }, JsonRequestBehavior.AllowGet);

                }
                else
                {
                    return Json(new { Success = true, Title = "Uyarı!", Message = "Seçtiğiniz dosya geçerli değil", Type = "warning", Id = Guid.Empty }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Title = "Uyarı!", Message = "Dosya yüklenirken bir hata oluştu", Type = "warning", Id = Guid.Empty }, JsonRequestBehavior.AllowGet);

            }
        }
        [CustomAuthorize("FileManagement")]
        [HttpPost]
        public JsonResult AddFolder(string FolderName, string dir)
        {
            try
            {
                var yol = dir.Replace("/", @"\") + "/" + Slugify(FolderName);
                var di = Directory.CreateDirectory(_root + yol);
                return Json(new { Success = true, Title = "Başarılı!", Message = "Klasör başarı ile eklendi.", Type = "success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Title = "Uyarı!", Message = "Klasör eklenirken bir hata oluştu." + ex.Message, Type = "warning" }, JsonRequestBehavior.AllowGet);

            }
        }
        [CustomAuthorize("FileManagement")]
        [HttpPost]
        public JsonResult RemoveFile(string dir, string fileName)
        {
            try
            {
                var path = _root + dir + "/" + fileName;

                if (!System.IO.File.Exists(Path.Combine(_root + dir, fileName)))
                    return Json(new { Success = true, Title = "Uyarı!", Message = "Dosya bulunamadı.", Type = "warning" },
                        JsonRequestBehavior.AllowGet);

                System.IO.File.Delete(Path.Combine(_root + dir, fileName));

                return Json(new { Success = true, Title = "Başarılı!", Message = "Dosya başarı ile silindi.", Type = "success" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Title = "Uyarı!", Message = "Dosya silinirken bir hata oluştu." + ex.Message, Type = "warning" }, JsonRequestBehavior.AllowGet);
            }
        }
        [CustomAuthorize("FileManagement")]
        [HttpPost]
        public JsonResult RemoveFolder(string dir)
        {
            try
            {
                var path = _root + dir;

                var directory = new DirectoryInfo(path);
                if (directory.Exists)
                {
                    directory.Delete(true);
                    return Json(new { Success = true, Title = "Başarılı!", Message = "Klasör başarı ile silindi.", Type = "success" }, JsonRequestBehavior.AllowGet);
                }
                else
                    return Json(new { Success = true, Title = "Uyarı!", Message = "Klasör bulunamadı.", Type = "warning" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Title = "Uyarı!", Message = "Klasör silinirken bir hata oluştu." + ex.Message, Type = "warning" }, JsonRequestBehavior.AllowGet);
            }
        }
        [CustomAuthorize("FileManagement")]
        public FileContentResult GetContent(string dir, string fileName)
        {
            try
            {
                var myfile = System.IO.File.ReadAllBytes(_root + dir + "/" + fileName);
                return new FileContentResult(myfile, MimeMapping.GetMimeMapping(fileName));
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        [OutputCache(Duration = 3600, VaryByParam = "*")]
        [HttpGet]
        public ActionResult GetFile(Guid id, string w = null, string h = null)
        {
            try
            {
                var file = _fileRepository.Get(id);
                if (file == null)
                    return null;

                var contentType = file.ContentType;
                var path = Server.MapPath(file.Location + file.FileLocation.Replace("/", @"\") + file.FileName);
                if (w != null && h != null)
                {
                    var img = new WebImage(path)
                        .Resize(Convert.ToInt32(w), Convert.ToInt32(h), false, true);
                    return new ImageResult(new MemoryStream(img.GetBytes()), "binary/octet-stream");
                }

                var myfile = System.IO.File.ReadAllBytes(path);
                return new FileContentResult(myfile, MimeMapping.GetMimeMapping(file.FileName));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
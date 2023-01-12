using Project.Admin.Filter;
using Project.BLL.Common;
using Project.BLL.Infrastructure;
using Project.BLL.Site;
using Project.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Project.DAL.ModelView;
using Menu = Project.DAL.Menu;

namespace Project.Admin.Controllers
{
    public class PageController : _BaseController
    {
        private IPage page = new PageRepository();

        #region Page Management

        [CustomAuthorize("PageManagement")]
        public ActionResult Add(Guid? id)
        {
            ViewBag.Target = _GetTargets();
            try
            {
                if (id == null)
                    return View();

                var d = page.GetPageById((Guid)id, siteId);
                d.Description = Server.HtmlDecode(d.Description);
                d.Url = "/" + d.PageNo + "/" + d.Url;

                return View(d);
            }
            catch (Exception ex)
            {
                TempData["Alert"] = Alert("danger", "Bir hata oluştu ve detaylar sistem yöneticisine iletildi.");
                AddLog("Exception", ex.Message);
                return View();
            }
        }
        [HttpPost]
        [CustomAuthorize("PageManagement")]
        [ValidateAntiForgeryToken]
        public ActionResult Add(Page d, HttpPostedFileBase FileThumb)
        {
            try
            {
                if (d.Id == Guid.Empty)
                {
                    d.Id = Guid.NewGuid();
                    d.SiteId = siteId;
                    d.CreatedUserId = _user.Id;
                    d.UpdatedUserId = _user.Id;
                    d.Url = Slugify(d.Title);
                    d.Description = string.IsNullOrEmpty(d.Description) ? "<p></p>" : d.Description;

                    if (FileThumb != null)
                    {
                        var r = UploadImage(FileThumb, "/Page/" + d.Id);
                        if (r.Status)
                        {
                            d.ThumbnailId = (r.Data as File)?.Id;
                        }
                        else
                        {
                            AddLog("Info", new { Action = "Insert", Data = r.Data });
                            return Swal.Warning(r.Message);
                        }
                    }

                    if (d.ThumbnailId == Guid.Empty)
                        d.ThumbnailId = null;

                    if(d.ShowInHome)
                        page.ResetShowInHome(siteId);
                    page.Insert(d);

                    AddLog("Info", new { Action = "Insert", Data = d });
                    return Swal.RecordAdded("Insert");
                }
                else
                {
                    var data = page.GetPageById(d.Id, siteId);

                    if (data == null)
                    {
                        AddLog("Warning", new { Message = "Record not found", RoleId = d.Id });
                        return Swal.RecordNotFound();
                    }

                    if (FileThumb != null)
                    {
                        var r = UploadImage(FileThumb, "/Page/" + d.Id);
                        if (r.Status)
                        {
                            d.ThumbnailId = (r.Data as File)?.Id;
                        }
                        else
                        {
                            AddLog("Info", new { Action = "Insert", Data = r.Data });
                            return Swal.Warning(r.Message);
                        }
                    }

                    if (d.ThumbnailId == Guid.Empty)
                        d.ThumbnailId = null;

                    d.UpdatedUserId = _user.Id;
                    d.Url = Slugify(d.Title);
                    d.Description = string.IsNullOrEmpty(d.Description) ? "<p></p>" : d.Description;
                    d.SiteId = siteId;

                    if (d.ShowInHome)
                        page.ResetShowInHome(siteId);

                    page.Update(d);

                    AddLog("Info", new { Action = "Update", Data = d });
                    return Swal.RecordUpdated("Update");
                }
            }
            catch (Exception ex)
            {
                AddLog("Exception", ex.Message);
                return Swal.Exception();
            }
        }
        [CustomAuthorize("PageManagement")]
        public ActionResult PageList()
        {
            return View();
        }
        [CustomAuthorize("PageManagement")]
        public ActionResult PageListJSON()
        {
            try
            {
                var draw = Request.Form.GetValues("draw").FirstOrDefault();
                var start = Request.Form.GetValues("start").FirstOrDefault();
                var length = Request.Form.GetValues("length").FirstOrDefault();
                var searchValue = Request.Form.GetValues("search[value]").FirstOrDefault();

                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;
                var recordsTotal = 0;

                var dd = page.GetAllPage(skip, pageSize, searchValue, siteId);

                recordsTotal = dd?.FirstOrDefault()?.Total ?? 0;

                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = dd });
            }
            catch (Exception ex)
            {
                AddLog("Exception", ex.Message);
                throw;
            }
        }
        [CustomAuthorize("PageManagement")]
        public ActionResult _PageRemove()
        {
            try
            {
                var id = new Guid(Request.Form["Id"]);

                var data = page.GetPageById(id, siteId);

                if (data == null)
                    return Swal.RecordNotFound();

                page.RemovePage(id, siteId);

                AddLog("Info", new { Action = "Remove", Id = id });
                return Swal.RecordRemoved();
            }
            catch (Exception ex)
            {
                AddLog("Exception", ex.Message);
                return Swal.Exception();
            }
        }

        #endregion

        #region Menu Management
        [CustomAuthorize("PageManagement")]
        [HttpGet]
        public ActionResult Menu(Guid? id)
        {
            try
            {
                if (id == null)
                    return View();

                var d = page.GetMenuById((Guid)id, siteId);

                return View(d);
            }
            catch (Exception ex)
            {
                TempData["Alert"] = Alert("danger", "Bir hata oluştu ve detaylar sistem yöneticisine iletildi.");
                AddLog("Exception", ex.Message);
                return View();
            }
        }
        [HttpPost]
        [CustomAuthorize("PageManagement")]
        [ValidateAntiForgeryToken]
        public ActionResult Menu(Menu d)
        {
            try
            {
                if (d.Id == Guid.Empty)
                {

                    d.Id = Guid.NewGuid();
                    d.SiteId = siteId;

                    page.Insert(d);

                    AddLog("Info", new { Action = "Insert", Data = d });

                    return Swal.RecordAdded("Insert");
                }
                else
                {
                    var data = page.GetMenuById(d.Id,siteId);

                    if (data == null)
                    {
                        AddLog("Warning", new { Message = "Record not found", MenuId = d.Id });
                        return Swal.RecordNotFound();
                    }

                    page.Update(d);

                    AddLog("Info", new { Action = "Update", Data = d });

                    return Swal.RecordUpdated("Update");
                }
            }
            catch (Exception ex)
            {
                AddLog("Exception", ex.Message);
                return Swal.Exception();
            }
        }
        [CustomAuthorize("PageManagement")]
        public PartialViewResult _MenuList()
        {
            return PartialView();
        }
        [CustomAuthorize("PageManagement")]
        public ActionResult MenuListJSON()
        {
            try
            {
                var draw = (Request.Form.GetValues("draw") ?? Array.Empty<string>()).FirstOrDefault();
                var start = (Request.Form.GetValues("start") ?? Array.Empty<string>()).FirstOrDefault();
                var length = (Request.Form.GetValues("length") ?? Array.Empty<string>()).FirstOrDefault();
                var searchValue = (Request.Form.GetValues("search[value]") ?? Array.Empty<string>()).FirstOrDefault();


                //Paging Size (10,20,50,100)    
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;
                var recordsTotal = 0;

                var dd = page.GetAllMenu(skip, pageSize, searchValue, siteId);

                recordsTotal = dd?.FirstOrDefault()?.Total ?? 0;

                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = dd });
            }
            catch (Exception ex)
            {
                AddLog("Exception", ex.Message);
                throw;
            }

        }
        [CustomAuthorize("PageManagement")]
        public ActionResult _MenuDelete()
        {
            try
            {
                var id = new Guid(Request.Form["Id"]);

                var data = page.GetMenuById(id, siteId);
                if (data == null)
                    return Swal.RecordNotFound();

                page.RemoveMenu(id,siteId);

                AddLog("Info", new { Action = "Remove", Id = id });
                return Swal.RecordRemoved();
            }
            catch (Exception ex)
            {
                AddLog("Exception", ex.Message);
                return Swal.Exception();
            }
        }
        #endregion

        #region Menu Page Management
        [CustomAuthorize("PageManagement")]
        public ActionResult MenuPage(Guid? id)
        {
            try
            {
                if (id == null)
                    return RedirectToAction("Menu", "Page");

                ViewBag.MenuId = id;
                var d = page.GetMenuPageList((Guid)id, siteId);
                return View(d);
            }
            catch (Exception ex)
            {
                TempData["Alert"] = Alert("danger", "Bir hata oluştu ve detaylar sistem yöneticisine iletildi.");
                AddLog("Exception", ex.Message);
                return View();
            }
        }
        [HttpPost]
        [CustomAuthorize("PageManagement")]
        [ValidateAntiForgeryToken]
        public ActionResult CreateMenu()
        {
            try
            {
                var menuId = new Guid(Request.Form["MenuId"]);
                var menuJson = Request.Form["MenuJson"];

                var js = new JavaScriptSerializer();
                var menu = js.Deserialize<List<MenuJson>>(menuJson);
                page.RemoveMenuPage(menuId);
                var ord = 0;
                RecursiveMenuSave(menu.FirstOrDefault(x => x.id == Guid.Empty), menuId, ref ord);
                page.SetHasSubmenu(menuId);

                AddLog("Info", new { Action = "Insert" });
                return Swal.RecordAdded("Insert");
            }
            catch (Exception ex)
            {
                AddLog("Exception", ex.Message);
                return Swal.Exception();
            }
        }
        private void RecursiveMenuSave(MenuJson menu, Guid menuId, ref int ord)
        {
            if (menu.children == null) return;

            var pageId = menu.id;
            foreach (var m in menu.children)
            {
                var mp = new MenuPage()
                {
                    Id = Guid.NewGuid(),
                    MenuId = menuId,
                    PageId = m.id,
                    TopPageId = pageId,
                    Ord = ord,
                    HasSubMenu = false

                };
                ord++;
                page.Insert(mp);
                RecursiveMenuSave(m, menuId, ref ord);
            }
        }
        [ChildActionOnly]
        private PartialViewResult _GetMenu(MenuForPartial subMenu)
        {
            return PartialView(subMenu);
        }
        #endregion
        //#region Menu Management

        //[CustomAuthorize("MenuCRUD")]
        //public ActionResult Menu(Guid? id)
        //{
        //    try
        //    {
        //        if (id == null)
        //            return View();

        //        var d = _pageRepository.GetMenuById((Guid)id);
        //        return View(d);
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Uyari"] = Uyari("danger", "Bir hata oluştu ve detaylar sistem yöneticisine iletildi. Endişelenmeyin.");
        //        LogIt(_user.Id, ex.Message, "Exception", "SiteManagement");
        //        return View();
        //    }
        //}
        //[HttpPost]
        //[CustomAuthorize("PortalAdmin")]
        //[ValidateAntiForgeryToken]
        //public ActionResult Menu(Menu d)
        //{
        //    try
        //    {
        //        if (d.Id == Guid.Empty)
        //        {
        //            d.Id = Guid.NewGuid();
        //            d.SiteId = _site.SiteId;
        //            d.DilId = _site.DilId;
        //            d.KullaniciId = _user.Id;

        //            _pageRepository.Insert(d);
        //            LogIt(_user.Id, "Kayıt eklendi.", "AddMenu", "SiteManagement");
        //            return Swal.KayitEklendi();
        //        }
        //        else
        //        {
        //            var data = _pageRepository.GetMenuById(d.Id);

        //            if (data == null)
        //            {
        //                LogIt(_user.Id, "Kayıt bulunamadı.", "EditMenu", "SiteManagement");
        //                return Swal.KayitBulunamadi("Duzenle");
        //            }


        //            _pageRepository.Update(d);
        //            LogIt(_user.Id, "Kayıt düzenlendi.", "EditMenu", "SiteManagement");
        //            return Swal.KayitDuzenlendi();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogIt(_user.Id, ex.Message, "Exception", "SiteManagement");
        //        return Swal.HataOlustu(ex, d.Id == Guid.Empty ? "Ekle" : "Duzenle");
        //    }
        //}
        //[CustomAuthorize("MenuCRUD")]
        //public PartialViewResult _MenuList()
        //{
        //    return PartialView();
        //}
        //[CustomAuthorize("MenuCRUD")]
        //public ActionResult MenuListJSON()
        //{
        //    try
        //    {
        //        var draw = Request.Form.GetValues("draw").FirstOrDefault();
        //        var start = Request.Form.GetValues("start").FirstOrDefault();
        //        var length = Request.Form.GetValues("length").FirstOrDefault();
        //        var searchValue = Request.Form.GetValues("search[value]").FirstOrDefault();


        //        //Paging Size (10,20,50,100)    
        //        var pageSize = length != null ? Convert.ToInt32(length) : 0;
        //        var skip = start != null ? Convert.ToInt32(start) : 0;
        //        var recordsTotal = 0;

        //        // Getting all Customer data    
        //        var dd = _pageRepository.GetMenuistWithPaging(skip, pageSize, searchValue, _site.DilId, _site.SiteId);

        //        //total number of rows count     
        //        recordsTotal = dd?.FirstOrDefault()?.ToplamKayit ?? 0;

        //        //Returning Json Data    
        //        return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = dd });
        //    }
        //    catch (Exception ex)
        //    {
        //        LogIt(_user.Id, ex.Message, "Exception", "SiteManagement");
        //        throw;
        //    }

        //}
        //[CustomAuthorize("PortalAdmin")]
        //public ActionResult _MenuRemove()
        //{
        //    try
        //    {
        //        var id = new Guid(Request.Form["Id"]);

        //        var data = _pageRepository.GetMenuById(id);

        //        if (data == null)
        //            return Swal.KayitBulunamadi("Sil");

        //        _pageRepository.RemoveMenu(id);

        //        LogIt(_user.Id, "Kayıt silindi.", "RemoveMenu", "SiteManagement");
        //        return Swal.KayitSilindi();
        //    }
        //    catch (Exception ex)
        //    {
        //        LogIt(_user.Id, ex.Message, "Exception", "SiteManagement");
        //        return Swal.HataOlustu(ex, "Sil");
        //    }
        //}
        //[CustomAuthorize("MenuCRUD")]
        //public ActionResult MenuDetail(Guid? id)
        //{
        //    try
        //    {
        //        if (id == null)
        //            return RedirectToAction("Menu", "Page", new { Area = "CMS" });

        //        ViewBag.MenuId = id;
        //        var d = _pageRepository.GetMenuSayfaList((Guid)id, _site.SiteId, _site.DilId);
        //        return View(d);
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Uyari"] = Uyari("danger", "Bir hata oluştu ve detaylar sistem yöneticisine iletildi. Endişelenmeyin.");
        //        LogIt(_user.Id, ex.Message, "Exception", "SiteManagement");
        //        return View();
        //    }
        //}
        //[HttpPost]
        //[CustomAuthorize("MenuCRUD")]
        //[ValidateAntiForgeryToken]
        //public ActionResult CreateMenu()
        //{
        //    try
        //    {
        //        var menuId = new Guid(Request.Form["MenuId"]);
        //        var menuJson = Request.Form["MenuJson"];

        //        var js = new JavaScriptSerializer();
        //        var menu = js.Deserialize<List<MenuJson>>(menuJson);
        //        _pageRepository.RemoveMenuSayfa(menuId);
        //        var sira = 0;
        //        RecursiveMenuSave(menu.FirstOrDefault(x => x.id == Guid.Empty), menuId, ref sira);
        //        _pageRepository.SetHasSubmenu(menuId);
        //        LogIt(_user.Id, "Kayıt eklendi.", "AddMenu", "SiteManagement");
        //        return Swal.KayitEklendi();
        //    }
        //    catch (Exception ex)
        //    {
        //        LogIt(_user.Id, ex.Message, "Exception", "SiteManagement");
        //        return Swal.HataOlustu(ex, "Kaydet");
        //    }
        //}
        //public void RecursiveMenuSave(MenuJson menu, Guid menuId, ref int sira)
        //{
        //    if (menu.children == null) return;

        //    var sayfaId = menu.id;
        //    foreach (var m in menu.children)
        //    {
        //        var ms = new MenuSayfa()
        //        {
        //            Id = Guid.NewGuid(),
        //            MenuId = menuId,
        //            SayfaId = m.id,
        //            UstSayfaId = sayfaId,
        //            Sira = sira,
        //            HasSubmenu = false

        //        };
        //        sira++;
        //        _pageRepository.Insert(ms);
        //        RecursiveMenuSave(m, menuId, ref sira);
        //    }
        //}

        //#endregion
    }
}
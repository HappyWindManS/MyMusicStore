using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MusicStoreEntity;
using MusicStoreEntity.UserAndRole;
using MusicStore.ViewModels;

namespace MusicStore.Controllers
{
    public class StoreController : Controller
    {
        private static readonly EntityDbContext _context = new EntityDbContext();
        /// <summary>
        /// 显示专辑的明细
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult Detail(Guid id)
        {
            var detail = _context.Albums.Find(id);
            var cmt = _context.Replies.Where(x => x.Album.ID == id && x.ParentReply == null)
                .OrderByDescending(x => x.CreateDateTime).ToList();
            string htmlString = _GetHtml(cmt);
            ViewBag.Cmt = htmlString;
            return View(detail);
        }

        /// <summary>
        /// 点赞
        /// </summary>
        /// <param name="pid">回复id</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Like(Guid pid)
        {
            //初始化存储对象
            var status = new LikeStatus()
            {
                Status = "",
                Html = "",
            };
            //1判断用户是否登录
            if (Session["LoginUserSessionModel"] == null)
            {
                status = new LikeStatus()
                {
                    Status = "noLogin",
                    Html = "",
                };
                return Json(status);
            }
            //2.判断用户是否对这条回复点过赞或踩
            var person = _context.Persons.Find((Session["LoginUserSessionModel"] as LoginUserSessionModel).Person.ID);
            var reply = _context.Replies.Find(pid);//当前评论
            //是否有踩赞记录
            var IsLike = _context.LikeReplies.SingleOrDefault(x => x.Reply.ID == reply.ID && x.Person.ID == person.ID);


            //3.保存  reply实体中like+1或hate+1  LikeReply添加一条记录
            if (IsLike == null || IsLike.Person.ID != person.ID)
            {

                reply.Like += 1;//添加赞数
                var ok = new LikeReply()
                {
                    Reply = reply,
                    IsNotLike = true,
                    Person = person
                };
                _context.LikeReplies.Add(ok);
                _context.SaveChanges();
            }
            //点赞失败，已经点过了
            else
            {
                //赞或踩过了 判断
                if (IsLike.IsNotLike == false)
                {
                    status.Status = "false";
                    status.Html = "";
                    return Json(status);
                }
                reply.Like -= 1;
                _context.LikeReplies.Remove(IsLike);
                _context.SaveChanges();
            }

            //根据是否有上级回复 刷新detail 或 modal
            if (reply.ParentReply == null)
            {
                //显示评论、排序
                var albumSay = _context.Replies.Where(x => x.Album.ID == reply.Album.ID && x.ParentReply == null)
                    .OrderByDescending(x => x.CreateDateTime).ToList();

                status.Status = "Parent";
                status.Html = _GetHtml(albumSay);
            }
            else
            {
                //查询子回复
                var pcmt = _context.Replies.Find(pid).ParentReply;//主
                var cmts = _context.Replies.Where(x => x.ParentReply.ID == pcmt.ID).OrderByDescending(x => x.CreateDateTime).ToList();


                status.Status = "Son";
                status.Html = _GetHtmlModal(pcmt, cmts);
            }
            //生成html 注入视图
            return Json(status);
        }

        /// <summary>
        /// 踩
        /// </summary>
        /// <param name="pid">回复id</param>
        /// <returns></returns>
        public ActionResult Hate(Guid pid)
        {
            //初始化存储对象
            var status = new LikeStatus()
            {
                Status = "",
                Html = "",
            };
            //1判断用户是否登录
            if (Session["LoginUserSessionModel"] == null)
            {
                status = new LikeStatus()
                {
                    Status = "noLogin",
                    Html = "",
                };
                return Json(status);
            }
            //2.判断用户是否对这条回复点过赞或踩
            var person = _context.Persons.Find((Session["LoginUserSessionModel"] as LoginUserSessionModel).Person.ID);
            var reply = _context.Replies.Find(pid);//当前评论
            //是否有踩赞记录
            var IsLike = _context.LikeReplies.SingleOrDefault(x => x.Reply.ID == reply.ID && x.Person.ID == person.ID);


            //3.保存  reply实体中like+1或hate+1  LikeReply添加一条记录
            if (IsLike == null || IsLike.Person.ID != person.ID)
            {

                reply.Hate += 1;//添加赞数
                var ok = new LikeReply()
                {
                    Reply = reply,
                    IsNotLike = false,
                    Person = person
                };
                _context.LikeReplies.Add(ok);
                _context.SaveChanges();
            }
            //点赞失败，已经点过了
            else
            {
                //赞或踩过了 判断
                if (IsLike.IsNotLike == true)
                {
                    status.Status = "false";
                    status.Html = "";
                    return Json(status);
                }
                reply.Hate -= 1;
                _context.LikeReplies.Remove(IsLike);
                _context.SaveChanges();
            }

            //刷新detail 或 modal
            if (reply.ParentReply == null)
            {
                //显示评论、排序
                var albumSay = _context.Replies.Where(x => x.Album.ID == reply.Album.ID && x.ParentReply == null)
                    .OrderByDescending(x => x.CreateDateTime).ToList();

                status.Status = "Parent";
                status.Html = _GetHtml(albumSay);
            }
            else
            {
                //查询子回复
                var pcmt = _context.Replies.Find(pid).ParentReply;//主
                var cmts = _context.Replies.Where(x => x.ParentReply.ID == pcmt.ID).OrderByDescending(x => x.CreateDateTime == x.CreateDateTime).ToList();


                status.Status = "Son";
                status.Html = _GetHtmlModal(pcmt, cmts);
            }
            //生成html 注入视图
            return Json(status);
        }

        /// <summary>
        /// 生成子回复html文本 模态框
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public string _GetHtmlModal(Reply pcmt, List<Reply> cmts)
        {
            var htmlString = "";

            htmlString += "<div class=\"modal-header\">";
            htmlString += "<button type=\"button\" class=\"close\" data-dismiss=\"modal\" aria-hidden=\"true\">×</button>";
            htmlString += "<h4 class=\"modal-title\" id=\"myModalLabel\">";
            htmlString += "<em>楼主&nbsp&nbsp</em>" + pcmt.Person.Name + "  发表于" + pcmt.CreateDateTime.ToString("yyyy年MM月dd日 hh点mm分ss秒") + ":<br/>" + pcmt.Content;
            htmlString += " </h4> </div>";

            htmlString += "<div class=\"modal-body\">";
            //子回复
            htmlString += "<ul class='media-list' style='margin-left:20px;'>";
        
            foreach (var item in cmts)
            {
                htmlString += "<li class='media'>";
                htmlString += "<div class='media-left'>";
                htmlString += "<img class='media-object' id='ReplyImg' src='" + item.Person.Avarda + "' alt=\"头像\">";
                htmlString += "</div>";
                htmlString += "<div class='media-body' id='Content-" + item.ID + "'>";
                htmlString += "<h5 class='media-heading'><em>" + item.Person.Name + "</em>&nbsp;&nbsp;回复  " + item.ParentReply.Person.Name + ": " + item.ParentReply.Content + " </h5> ";
                htmlString += item.Content;
                htmlString += "</div>";
                htmlString += "<h6 style =\"margin-bottom:5px; border-bottom:1px solid #c6e6e8\">" + item.CreateDateTime.ToString("yyyy年MM月dd日 hh时mm分ss秒") +
                              "<a href='#div-editor' style='margin-left:20px;' class='reply' onclick=\"javascript:GetQuote('" + item.ParentReply.ID + "','" + item.ID + "');\">回复</a>" +
                              "<a href='#' class='reply' style='margin-left:20px;'   onclick=\"javascript:Like('" + item.ID + "');\"><i class='glyphicon glyphicon-thumbs-up'></i>(" + item.Like + ")</a>" +
                              "<a href='#' class='reply' style='margin-left:20px;'   onclick=\"javascript:Hate('" + item.ID + "');\"><i class='glyphicon glyphicon-thumbs-down'></i>(" + item.Hate + ")</a></h6>";
                htmlString += "</li>";
            }
            htmlString += "</ul>";
            htmlString += "</div><div class=\"modal-footer\"></div>";

            return htmlString;
        }

        #region HTML注入
        private static string _GetHtml(List<Reply> cmt)
        {
            var htmlString = "";
            htmlString += "<ul class='media-list'>";
            foreach (var item in cmt)
            {
                htmlString += "<li class='media'>";
                htmlString += "<div class='media-left'>";
                htmlString += "<img class='media-object' src='" + item.Person.Avarda +
                              "' alt='头像' style='width:40px;border-radius:50%;'>";
                htmlString += "</div>";
                htmlString += "<div class='media-body' id='Content-" + item.ID + "'>";
                htmlString += "<h5 class='media-heading'>" + item.Person.Name + "  发表于" +
                              item.CreateDateTime.ToString("yyyy年MM月dd日 hh点mm分ss秒") + "</h5>";
                htmlString += item.Content;
                htmlString += "</div>";
                //查询当前回复的下级回复
                var sonCmt = _context.Replies.Where(x => x.ParentReply.ID == item.ID).ToList();
                htmlString += "<h6><a href='#div-editor' class='reply' onclick=\"javascript:GetQuote('" + item.ID +
                              "');\">回复</a>(<a href='#' class='reply'  onclick=\"javascript:ShowCmt('" + item.ID + "');\">" + sonCmt.Count + "</a>)条" +
                              "<a href='#' class='reply' style='margin:0 20px 0 40px'><i class='glyphicon glyphicon-thumbs-up'></i>(" +
                              item.Like + ")</a><a href='#' class='reply' style='margin:0 20px'><i class='glyphicon glyphicon-thumbs-down'></i>(" + item.Hate + ")</a></h6>";

                htmlString += "</li>";
            }
            htmlString += "</ul>";
            return htmlString;
        }
        #endregion

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddCmt(string id,string cmt,string reply)
        {
            if (Session["LoginUserSessionModel"] == null)
                return Json("nologin");

            var person = _context.Persons.Find((Session["LoginUserSessionModel"] as
                LoginUserSessionModel).Person.ID);
            var album = _context.Albums.Find(Guid.Parse(id));

            //创建回复对象
            var r = new Reply()
            {
                Album = album,
                Person = person,
                Content = cmt,
                Title = ""
            };
            //父级回复
            if (string.IsNullOrEmpty(reply))
            {
                //顶级回复, ParentReply为空
                r.ParentReply = null;
            }
            else
            {
                r.ParentReply = _context.Replies.Find(Guid.Parse(reply));
            }

            _context.Replies.Add(r);
            _context.SaveChanges();

            //局部刷新
            var replies = _context.Replies.Where(x => x.Album.ID == album.ID && x.ParentReply == null)
                .OrderByDescending(x => x.CreateDateTime).ToList();
            return Json(_GetHtml(replies));
        }

        [HttpPost]
        public ActionResult showCmts(string pid)
        {
            var htmlString = "";
            //子回复
            Guid id = Guid.Parse(pid);
            var cmts = _context.Replies.Where(x => x.ParentReply.ID == id).OrderByDescending(x => x.CreateDateTime).ToList();
            //原回复
            var pcmt = _context.Replies.Find(id);
            htmlString += "<div class=\"modal-header\">";
            htmlString += "<button type=\"button\" class=\"close\" data-dismiss=\"modal\" aria-hidden=\"true\">×</button>";
            htmlString += "<h4 class=\"modal-title\" id=\"myModalLabel\">";
            htmlString += "<em>楼主&nbsp;&nbsp;</em>" + pcmt.Person.Name + "&nbsp;&nbsp;发表于" + pcmt.CreateDateTime.ToString("yyyy年MM月dd日 hh点mm分ss秒") + ":<br/>" + pcmt.Content;
            htmlString += " </h4> </div>";

            htmlString += "<div class=\"modal-body\">";
            //子回复
            htmlString += "<ul class='media-list' style='margin-left:20px;'>";
            foreach (var item in cmts)
            {
                htmlString += "<li class='media'>";
                htmlString += "<div class='media-left'>";
                htmlString += "<img class='media-object' src='" + item.Person.Avarda +
                              "' alt='头像' style='width:40px;border-radius:50%;'>";
                htmlString += "</div>";
                htmlString += "<div class='media-body' id='Content-" + item.ID + "'>";
                htmlString += "<h5 class='media-heading'><em>" + item.Person.Name + "</em>&nbsp;&nbsp;发表于" +
                              item.CreateDateTime.ToString("yyyy年MM月dd日 hh点mm分ss秒") + "</h5>";
                htmlString += item.Content;
                htmlString += "</div>";
                htmlString += "<h6><a href='#div-editor' class='reply' onclick=\"javascript:GetQuote('" + item.ParentReply.ID + "','" + item.ID + "');\">回复</a>" +
                              "<a href='#' class='reply' style='margin:0 20px 0 40px'   onclick=\"javascript:Like('" + item.ID + "');\"><i class='glyphicon glyphicon-thumbs-up'></i>(" + item.Like + ")</a>" +
                              "<a href='#' class='reply' style='margin:0 20px'   onclick=\"javascript:Hate('" + item.ID + "');\"><i class='glyphicon glyphicon-thumbs-down'></i>(" + item.Hate + ")</a></h6>";
                htmlString += "</li>";
            }
            htmlString += "</ul>";
            htmlString += "</div><div class=\"modal-footer\"></div>";
            return Json(htmlString);
        }

        /// <summary>
        /// 按分类显示专辑
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult Browser(Guid id)
        {
            var list = _context.Albums.Where(x=>x.Genre.ID==id)
                .OrderByDescending(x=>x.PublisherDate).ToList();
            return View(list);
        }

        /// <summary>
        /// 显示所有的分类
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            var genres = _context.Genres.OrderBy(x => x.Name).ToList();
            return View(genres);
        }

        public class LikeStatus
        {
            public string Status { get; set; }
            public string Html { get; set; }
        }
    }
}
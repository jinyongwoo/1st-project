using Microsoft.AspNetCore.Mvc;
using cook.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Hosting;

namespace cook.Controllers
{
    public class PostController : Controller
    {
        private List<Post> list;
        private readonly Csharp_Post_services postService;
        private readonly IWebHostEnvironment _env;

        public PostController(IWebHostEnvironment env)
        {
            _env = env;
            string connString = "Server=" + "127.0.0.1" +
                                ";Database=" + "recipe_members" +
                                ";port=" + "3306" +
                                ";user=" + "root" +
                                ";password=" + "1009";
            postService = new Csharp_Post_services(connString);
        }

        public IActionResult Index()
        {
            list = postService.Getpost();
            return View(list);
        }

        public IActionResult Details(int id)
        {
            var post = postService.SelectPost(id);
            return View(post);
        }

        public ActionResult Update(int id)
        {
            var std = postService.SelectPost(id);
            return View(std);
        }

        public IActionResult Create()
        {
            return View();
        }

        public ActionResult Createpost(IFormCollection form)
        {
            var member_name = form["member_name"].ToString();
            var title = form["Title"].ToString();
            var content = form["Content"].ToString();
            var imageDataFile = form.Files["ImageData"];

            byte[] imageData = ReadImageAsByteArray(imageDataFile);

            var imagePath = Path.Combine(_env.WebRootPath, "images");

            var fileExtension = Path.GetExtension(imageDataFile.FileName);
            var fileName = Guid.NewGuid().ToString().Replace("-", "") + fileExtension;
            var filePath = Path.Combine(imagePath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                imageDataFile.CopyTo(stream);
            }

            int result = postService.InsertPost(member_name, title, content, filePath, imageData);

            TempData["result"] = result;
            return View();
        }

        public IActionResult Delete(int id)
        {
            return View();
        }

        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        public IActionResult Edit(int id)
        {
            Post post = postService.SelectPost(id);
            if (post == null)
            {
                return NotFound();
            }
            return View(post);
        }

        public IActionResult Editpost(int id, Post updatedPost, IFormFile imageDataFile)
        {
            if (id != updatedPost.Postid)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                using (MySqlConnection conn = new MySqlConnection(postService.ConnectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            int result = 0;
                            string imagePath = null;
                            byte[] imageData = null;

                            if (imageDataFile != null && imageDataFile.Length > 0)
                            {
                                imageData = ReadImageAsByteArray(imageDataFile);
                                imagePath = SaveImageAndGetPath(imageDataFile);
                            }

                            result = postService.UpdatePostWithImagePathOrNoImage(updatedPost.Postid, updatedPost.member_name, updatedPost.Title, updatedPost.Content, imagePath, imageData);

                            if (result == 1)
                            {
                                TempData["result"] = "수정되었습니다.";
                                transaction.Commit();
                            }
                            else
                            {
                                TempData["result"] = "수정에 실패하였습니다.";
                                transaction.Rollback();
                            }

                            return RedirectToAction("CreateResult");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("데이터베이스 작업 실패");
                            Console.WriteLine(ex.ToString());
                            transaction.Rollback();
                        }
                    }
                }
            }

            return View(updatedPost);
        }

        private string SaveImageAndGetPath(IFormFile imageDataFile)
        {
            var uploads = Path.Combine(_env.WebRootPath, "images");
            var fileName = Guid.NewGuid().ToString().Replace("-", "") + Path.GetExtension(imageDataFile.FileName);
            var imagePath = Path.Combine(uploads, fileName);

            using (var fileStream = new FileStream(imagePath, FileMode.Create))
            {
                imageDataFile.CopyTo(fileStream);
            }

            return imagePath;
        }

        private byte[] ReadImageAsByteArray(IFormFile file)
        {
            using (var stream = new MemoryStream())
            {
                file.CopyTo(stream);
                return stream.ToArray();
            }
        }
    }
}






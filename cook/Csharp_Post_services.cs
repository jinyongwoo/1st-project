using cook.Models;
using Microsoft.Extensions.Hosting;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Transactions;

namespace cook
{
    public class Csharp_Post_services
    {
        public string ConnectionString { get; set; }

        public Csharp_Post_services(string connectionString)
        {
            ConnectionString = connectionString;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public List<Post> Getpost()
        {
            List<Post> list = new List<Post>();
            string SQL = "SELECT * FROM posts";
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SQL, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Post post = new Post()
                        {
                            Postid = Convert.ToInt32(reader["Postid"]),
                            // member_id = Convert.ToInt32(reader["member_id"]),
                            member_name = reader["member_name"].ToString(),
                            Title = reader["Title"].ToString(),
                            Content = reader["Content"].ToString(),
                        };

                        if (!DBNull.Value.Equals(reader["ImagePath"])) // 이미지 경로 필드로 변경
                        {
                            post.ImagePath = reader["ImagePath"].ToString(); // ImagePath를 읽어옴
                        }


                        list.Add(post);
                    }
                }
                conn.Close();
            }
            return list;
        }
        public Post SelectPost(int Postid)
        {
            Post post = null;

            string SQL = "SELECT * FROM posts WHERE Postid = @Postid";
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SQL, conn);
                cmd.Parameters.AddWithValue("@Postid", Postid);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        post = new Post()
                        {
                            Postid = Convert.ToInt32(reader["Postid"]),
                            // member_id = Convert.ToInt32(reader["member_id"]),
                            member_name = reader["member_name"].ToString(),
                            Title = reader["Title"].ToString(),
                            Content = reader["Content"].ToString()
                        };

                        // ImagePath 필드 값을 가져옴
                        if (!reader.IsDBNull(reader.GetOrdinal("ImagePath")))
                        {
                            post.ImagePath = reader["ImagePath"].ToString();
                        }
                    }
                }
            }

            return post;
        }


        public int InsertPost(string member_name, string Title, string Content, string ImagePath, byte[] ImageData)
        {
            string SQL = "INSERT INTO posts (member_name, Title, Content, ImagePath, ImageData) VALUES (@member_name, @Title, @Content, @ImagePath, @ImageData)";
            using (MySqlConnection conn = GetConnection())
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(SQL, conn);
                    cmd.Parameters.AddWithValue("@member_name", member_name);
                    cmd.Parameters.AddWithValue("@Title", Title);
                    cmd.Parameters.AddWithValue("@Content", Content);
                    cmd.Parameters.AddWithValue("@ImagePath", ImagePath);
                    cmd.Parameters.AddWithValue("@ImageData", ImageData);

                    if (cmd.ExecuteNonQuery() == 1)
                    {
                        Console.WriteLine("삽입 성공");
                        return 1;
                    }
                    else
                    {
                        Console.WriteLine("실패");
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DB 연결 실패");
                    Console.WriteLine(ex.Message);
                }
                conn.Close();
            }
            return 0;
        }


        public int UpdatePost(int Postid, string member_name, string Title, string Content, byte[] ImageData)
        {
            string SQL = "UPDATE posts SET member_name = @member_name, Title = @Title, Content = @Content, ImageData = @ImageData WHERE Postid = @Postid";
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                    try
                    {
                        MySqlCommand cmd = new MySqlCommand(SQL, conn);
                        cmd.Transaction = transaction;
                        cmd.Parameters.AddWithValue("@Postid", Postid);
                        cmd.Parameters.AddWithValue("@member_name", member_name);
                        cmd.Parameters.AddWithValue("@Title", Title);
                        cmd.Parameters.AddWithValue("@Content", Content);
                        cmd.Parameters.AddWithValue("@ImageData", ImageData);

                        if (cmd.ExecuteNonQuery() == 1)
                        {
                            Console.WriteLine("수정 성공");
                            return 1;
                        }
                        if (ImageData != null && ImageData.Length > 0)
                        {
                            string base64Image = Convert.ToBase64String(ImageData);
                            Console.WriteLine("이미지 데이터 (Base64): " + base64Image);
                            return 1;
                        }
                        else
                        {
                            Console.WriteLine("수정 실패");
                            return 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("데이터베이스 작업 실패");
                        Console.WriteLine(ex.ToString()); // 예외 스택 트레이스 출력
                        transaction.Rollback();
                    }
                conn.Close();
            }
            return 0;
        }

        public int DeletePost(int Postid)
        {
            string SQL = "DELETE FROM post WHERE Postid = @Postid";
            using (MySqlConnection conn = GetConnection())
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(SQL, conn);
                    if (cmd.ExecuteNonQuery() == 1)
                    {
                        Console.WriteLine("삭제 성공");
                        return 1;
                    }
                    else
                    {
                        Console.WriteLine("삭제 실패");
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DB 연결 실패");
                    Console.WriteLine(ex.Message);
                }
                conn.Close();
            }
            return 0;
        }
        public Post GetpostById(int id)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string SQL = "SELECT * FROM item WHERE Postid = @id";
                MySqlCommand cmd = new MySqlCommand(SQL, conn);
                cmd.Parameters.AddWithValue("@id", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Post
                        {
                            member_id = Convert.ToInt32(reader["Postid"]),
                            member_name = reader["member_name"].ToString(),
                            Title = reader["Title"].ToString(),
                            Content = reader["Content"].ToString(),

                        };
                    }
                }
            }
            return null;
        }
        public int UpdatePostWithImagePathOrNoImage(int postId, string memberName, string title, string content, string imagePath, byte[] imageData)
        {
            string SQL;
            MySqlConnection conn = GetConnection();

            try
            {
                conn.Open();
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    if (imagePath != null)
                    {
                        // 이미지 경로와 이미지 데이터를 모두 업데이트
                        SQL = "UPDATE post SET member_name = @memberName, title = @title, content = @content, image_path = @imagePath, image_data = @imageData WHERE Postid = @postId";
                        cmd.Parameters.AddWithValue("@imagePath", imagePath);
                        cmd.Parameters.AddWithValue("@imageData", imageData);
                    }
                    else
                    {
                        // 이미지 데이터를 업데이트하지 않음
                        SQL = "UPDATE post SET member_name = @memberName, title = @title, content = @content WHERE Postid = @postId";
                    }

                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("@postId", postId);
                    cmd.Parameters.AddWithValue("@memberName", memberName);
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@content", content);

                    int result = cmd.ExecuteNonQuery();
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("데이터베이스 업데이트 실패");
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                conn.Close();
            }

            return 0;
        }

    }

}



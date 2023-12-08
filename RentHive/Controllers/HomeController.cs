using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RentHive.Models;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;

namespace RentHive.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        public IActionResult WelcomePage()
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userData))
            {
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index(string Noti_Type, string Noti_Message)
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userData))
            {
                // User is not logged in, redirect to login or handle as needed
                return RedirectToAction("Login", "UserManagement");
            }

            ViewBag.Noti_Type = Noti_Type;
            ViewBag.Noti_Messsage = Noti_Message;

            string url1 = "https://renthive.online/Admin_API/HomeViewer.php";
            string url2 = "https://renthive.online/Admin_API/CountUser.php";
            string url3 = "https://renthive.online/Admin_API/CountAdmin.php";
            try
            {
                var userObject = JsonConvert.DeserializeObject<UserDataGetter>(userData);
                using (var httpClient = new HttpClient())
                {
                    var data = new Dictionary<string, string>
                    {
                        {"userId", userObject.Acc_id.ToString()}
                    };

                    var content = new FormUrlEncodedContent(data);

                    var response1 = await httpClient.PostAsync(url1, content);

                    if (response1.IsSuccessStatusCode)
                    {
                        var responseData1 = await response1.Content.ReadAsStringAsync();

                        if (responseData1 == "Something went wrong.")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "No record found";
                        }
                        else
                        {
                            //total number of user (getter)
                            //-------------------------------start of code----------------------------------
                            using (var client = new HttpClient())
                            {
                                HttpResponseMessage response2 = await client.GetAsync(url2);

                                if (response2.IsSuccessStatusCode)
                                {
                                    var responseData2 = await response2.Content.ReadAsStringAsync();

                                    if (responseData2 == "No Users found.")
                                    {
                                        ViewBag.Noti_Type = "error";
                                        ViewBag.Noti_Message = "No user found";
                                    }
                                    else
                                    {
                                        int userCount = Convert.ToInt32(responseData2);
                                        ViewBag.UserCount = userCount;
                                    }
                                }
                                else
                                {
                                    ViewBag.Noti_Type = "info";
                                    ViewBag.Noti_Message = "API request failed";
                                }
                            }
                            //-------------------------------end of code------------------------------------

                            //total number of admin (getter)
                            //-------------------------------start of code----------------------------------
                            using (var client = new HttpClient())
                            {
                                HttpResponseMessage response3 = await client.GetAsync(url3);

                                if (response3.IsSuccessStatusCode)
                                {
                                    var responseData3 = await response3.Content.ReadAsStringAsync();

                                    if (responseData3 == "No Users found.")
                                    {
                                        ViewBag.Noti_Type = "info";
                                        ViewBag.Noti_Message = "No admin found";
                                    }
                                    else
                                    {
                                        int userCount = Convert.ToInt32(responseData3);
                                        ViewBag.AdminCount = userCount;
                                    }
                                }
                                else
                                {
                                    ViewBag.Noti_Type = "info";
                                    ViewBag.Noti_Message = "API request failed";
                                }
                            }
                            //-------------------------------end of code------------------------------------


                            var newuserObject1 = JsonConvert.DeserializeObject<UserDataGetter>(responseData1);
                            return View(newuserObject1);
                        }
                    }
                    else
                    {
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = string.Format("There was an error processing your request.");
                return RedirectToAction("ErrorMessage", "ErrorView", new { ErrorMessage = ViewBag.ErrorMessage });
            }
            return View();
        }
        //----------------------------------------start of code-----------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Reports(UserDataGetter TempData, string sortBy, string Noti_Type, string Noti_Message)
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userData))
            {
                // User is not logged in, redirect to login or handle as needed
                return RedirectToAction("Login", "UserManagement");
            }
            string url = "https://renthive.online/Admin_API/ViewReports.php";
            try
            {
                using (var client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        // Parse the response content
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData == "No Users found")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "No record found";
                        }
                        else
                        {
                            //Successfully retrieving data 
                            var newuserObject = JsonConvert.DeserializeObject<List<UserDataGetter>>(responseData);

                            // Apply sorting based on the sortBy parameter
                            switch (sortBy)
                            {
                                case "Acc_id":
                                    newuserObject = newuserObject.OrderByDescending(item => item.Acc_id).ToList();
                                    break;
                                case "Rep_id":
                                    newuserObject = newuserObject.OrderByDescending(item => item.Rep_id).ToList();
                                    break;
                                case "Rep_Date":
                                    newuserObject = newuserObject.OrderByDescending(item => item.Rep_Date).ToList();
                                    break;
                                case "Topic":
                                    newuserObject = newuserObject.OrderByDescending(item => item.Rep_Topic).ToList();
                                    break;
                                case "0":
                                    newuserObject = newuserObject.Where(item => item.Rep_Approve == null || item.Rep_Approve == "0").ToList();
                                    break;
                                case "1":
                                    newuserObject = newuserObject.Where(item => item.Rep_Approve != null && item.Rep_Approve != "0").ToList();
                                    break;
                                case "User":
                                    newuserObject = newuserObject.Where(item => item.Reported_User != null && item.Reported_User != "0").ToList();
                                    break;
                                case "Post":
                                    newuserObject = newuserObject.Where(item => item.Post_id != null).ToList();
                                    break;
                                case "Feedback":
                                    newuserObject = newuserObject.Where(item => item.Post_id == null && item.Reported_User == null).ToList();
                                    break;
                                default:
                                    // Default sorting, if sortBy is not recognized
                                    newuserObject = newuserObject.OrderBy(item => item.Acc_id).ToList();
                                    break;
                            }
                            ViewBag.Acc_Id = TempData.Acc_id;
                           
                            return View(newuserObject);
                        }
                    }
                    else
                    {
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Noti_Type = "error";
                ViewBag.Noti_Message = "Record is empty";
                ViewBag.Acc_Id = TempData.Acc_id;
                return View(new List<UserDataGetter>());
            }
            return View();
        }
        //----------------------------------------end of code-----------------------------------------------


        //----------------------------------------start of code-----------------------------------------------
        [HttpPost]
        public async Task<IActionResult> ReportChecker(UserDataGetter TempData)
        {
            try
            {
                var userData = HttpContext.Session.GetString("UserData");
                if (string.IsNullOrEmpty(userData))
                {
                    return RedirectToAction("Login", "UserManagement");
                }

                string url = "https://renthive.online/Admin_API/ReportChecker.php";

                using (var httpClient = new HttpClient())
                {
                    int AdminID = TempData.AdminID;
                    string rep_user = TempData.Reported_User;
                    string rep_post = TempData.Post_id;
                    int rep_id = TempData.Rep_id;
                    int numHolder = TempData.NumHolder;

                    string formattedCurrentDateTime = DateTime.Now.ToString("MMMM dd, yyyy hh:mm:ss") + DateTime.Now.ToString(" tt").ToUpper() ;
                    string origin = "Report Page";
                    string sysResponse= "Success";

                    //3 days ban setter
                    string formattedCurrentDate = DateTime.Now.ToString("MMMM dd, yyyy");
                    string formattedEndDate = DateTime.Now.AddDays(3).ToString("MMMM dd, yyyy");



                    var data = new Dictionary<string, string>
                    {
                        {"adminID", AdminID.ToString()},
                        {"reportedUser", rep_user},
                        {"reportedPost", rep_post},
                        {"ReportID", rep_id.ToString()},
                        {"numholder", numHolder.ToString()},
                        {"CurrentDate", formattedCurrentDateTime},
                        {"origin", origin},
                        {"sysResponse", sysResponse},
                        { "userBanDate", formattedCurrentDate },
                        { "userBanEnd", formattedEndDate }
                    };

                    var content = new FormUrlEncodedContent(data);
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData == "Something went wrong.")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "No udrt found"; ;
                        }
                        else
                        {
                            
                            ViewBag.Noti_Type = "success";
                            ViewBag.Noti_Message = "Successfully Recorded";
                            return RedirectToAction("Reports","Home", new { Acc_id = AdminID, Noti_Type = ViewBag.Noti_Type, Noti_Message = ViewBag.Noti_Message });
                        }
                    }
                    else
                    {
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = string.Format("There was an error processing your request.");
                return RedirectToAction("ErrorMessage", "ErrorView", new { ErrorMessage = ViewBag.ErrorMessage });
            }

            return View();
        }
        //----------------------------------------end of code-----------------------------------------------

        //----------------------------------------start of code-----------------------------------------------
        public async Task<IActionResult> ReportDetails (UserDataGetter TempData, string Noti_Type, string Noti_Message)
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userData))
            {
                // User is not logged in, redirect to login or handle as needed
                return RedirectToAction("Login", "UserManagement");
            }
            string url = "https://renthive.online/Admin_API/Report_Details.php";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    int repId = TempData.Rep_id;
                    var data = new Dictionary<string, string>
                    {
                        {"repId", repId.ToString()}
                    };

                    var content = new FormUrlEncodedContent(data);
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData == "No record found")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "No record found";
                        }
                        else
                        {
                            var userObject = JsonConvert.DeserializeObject<UserDataGetter>(responseData);
                            //admin info
                            
                            ViewBag.AdminID = TempData.Acc_id;
                            return View(userObject);
                        }
                    }
                    else
                    {
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = string.Format("There was an error processing your request.");
                return RedirectToAction("ErrorMessage", "ErrorView", new { ErrorMessage = ViewBag.ErrorMessage });
            }
            return View();
        }
        //----------------------------------------end of code-----------------------------------------------

        public async Task<IActionResult> UserVerification(UserDataGetter TempData,string sortBy, string Noti_Type, string Noti_Message)
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userData))
            {
                // User is not logged in, redirect to login or handle as needed
                return RedirectToAction("Login", "UserManagement");
            }
            string url = "https://renthive.online/Admin_API/VerificationList.php";
            try
            {
                using (var client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        // Parse the response content
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData == "No Users found")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "No user found";
                        }
                        else
                        { 
                            //Successfully retrieving data 
                            var newuserObject = JsonConvert.DeserializeObject<List<UserDataGetter>>(responseData);
                            switch (sortBy)
                            {
                                case "Acc_id":
                                    newuserObject = newuserObject.OrderBy(item => item.Acc_id).ToList();
                                    break;
                                case "status":
                                    newuserObject = newuserObject.OrderByDescending(item => item.Ver_Status).ToList();
                                    break;
                                case "Ver_DateSent":
                                    newuserObject = newuserObject.OrderBy(item => item.Ver_DateSent).ToList();
                                    break;
                                case "verification_id":
                                    newuserObject = newuserObject.OrderBy(item => item.Ver_id).ToList();
                                    break;
                                case "0":
                                    newuserObject = newuserObject.Where(item => item.Ver_Status == "0").ToList();
                                    break;
                                case "1":
                                    newuserObject = newuserObject.Where(item => item.Ver_Status != "0").ToList();
                                    break;
                                default:
                                    // Default sorting, if sortBy is not recognized
                                    newuserObject = newuserObject.OrderByDescending(item => item.Acc_id).ToList();
                                    break;
                            }
                            ViewBag.Acc_Id = TempData.Acc_id;
                            
                            return View(newuserObject);
                        }
                    }
                    else
                    {
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Noti_Type = "error";
                ViewBag.Noti_Message = "Record is empty";
                ViewBag.Acc_Id = TempData.Acc_id;
                return View(new List<UserDataGetter>());
            }
            return View();
        }
        //----------------------------------------end of code-----------------------------------------------

        //----------------------------------------start of code-----------------------------------------------
        public async Task<IActionResult> UserVerification_Details(UserDataGetter TempData, string Noti_Type, string Noti_Message)
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userData))
            {
                // User is not logged in, redirect to login or handle as needed
                return RedirectToAction("Login", "UserManagement");
            }
            string url = "https://renthive.online/Admin_API/UserVerification_Details.php";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    string verID = TempData.Ver_id;

                    var data = new Dictionary<string, string>
                    {
                        {"verid", verID}
                    };

                    var content = new FormUrlEncodedContent(data);
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData == "No record found")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "No user found";
                        }
                        else
                        {
                            var userObject = JsonConvert.DeserializeObject<UserDataGetter>(responseData);

                            //admin info
                            ViewBag.AdminID = TempData.AdminID;
                            return View(userObject);
                        }
                    }
                    else
                    {
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = string.Format("There was an error processing your request.");
                return RedirectToAction("ErrorMessage", "ErrorView", new { ErrorMessage = ViewBag.ErrorMessage });
            }
            return View();
        }
        //----------------------------------------end of code-----------------------------------------------

        //----------------------------------------start of code-----------------------------------------------
        [HttpPost]
        public async Task<IActionResult> VerificationChecker(UserDataGetter TempData, string Noti_Type, string Noti_Message)
        {
            try
            {
                var userData = HttpContext.Session.GetString("UserData");
                if (string.IsNullOrEmpty(userData))
                {
                    return RedirectToAction("Login", "UserManagement");
                }

                string url = "https://renthive.online/Admin_API/VerificationChecker.php";

                using (var httpClient = new HttpClient())
                {
                    int adminID = TempData.AdminID;
                    string verifyID = TempData.Ver_id;
                    int numHolder = TempData.NumHolder;

                    string formattedCurrentDateTime = DateTime.Now.ToString("MMMM dd, yyyy hh:mm:ss") + DateTime.Now.ToString(" tt").ToUpper();
                    string origin = "Verification Page";
                    string sysResponse = "Success";

                    var data = new Dictionary<string, string>
                    {
                        {"adminID", adminID.ToString()},
                        {"verifyID", verifyID},
                        {"numHolder", numHolder.ToString()},
                        {"currentDate", formattedCurrentDateTime},
                        {"origin", origin},
                        {"sysResponse", sysResponse}
                    };

                    var content = new FormUrlEncodedContent(data);
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData != "Something went wrong.")
                        {
                            ViewBag.Noti_Type = "success";
                            ViewBag.Noti_Message = "Successful recorded";
                            return RedirectToAction("UserVerification", "Home", new { Acc_id = TempData.AdminID, ErrorMessage = ViewBag.ErrorMessage });
                        }
                    }
                    else
                    {
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = string.Format("There was an error processing your request.");
                return RedirectToAction("ErrorMessage", "ErrorView", new { ErrorMessage = ViewBag.ErrorMessage });
            }
            ViewBag.ErrorMessage = "Something went wrong.";
            return RedirectToAction("UserVerification", "Home", new { Acc_id = TempData.AdminID, ErrorMessage = ViewBag.ErrorMessage });
        }

        //----------------------------------------end of code-----------------------------------------------

        //----------------------------------------start of code-----------------------------------------------
        public async Task<IActionResult> BanAccounts(UserDataGetter TempData,string sortBy, string Noti_Type, string Noti_Message)
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userData))
            {
                // User is not logged in, redirect to login or handle as needed
                return RedirectToAction("Login", "UserManagement");
            }
            string url = "https://renthive.online/Admin_API/ViewBannedAccounts.php";
            try
            {
                using (var client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        // Parse the response content
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData == "No Users found")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "No user found";
                        }
                        else
                        {
                            var newuserObject = JsonConvert.DeserializeObject<List<UserDataGetter>>(responseData);

                            switch (sortBy)
                            {
                                case "Acc_id":
                                    newuserObject = newuserObject.OrderBy(item => item.Acc_id).ToList();
                                    break;
                                case "Acc_DisplayName":
                                    newuserObject = newuserObject.OrderBy(item => item.Acc_DisplayName).ToList();
                                    break;
                                case "Acc_BanEndDate":
                                    newuserObject = newuserObject.OrderByDescending(item => item.Acc_BanEndDate).ToList();
                                    break;
                                default:
                                    // Default sorting, if sortBy is not recognized
                                    newuserObject = newuserObject.OrderBy(item => item.Acc_id).ToList();
                                    break;
                            }
                            //Successfully retrieving data 
                            ViewBag.Acc_Id = TempData.Acc_id;
                            ViewBag.Noti_Type = "success";
                            ViewBag.Noti_Message = "retrieving data successfull";
                            return View(newuserObject);
                        }
                    }
                    else
                    {
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Noti_Type = "error";
                ViewBag.Noti_Message = "Record is empty";
                return View(new List<UserDataGetter>());
            }

            return View();
        }
        public async Task<IActionResult> BanAccountInfo(int userID, UserDataGetter TempData, string Noti_Type, string Noti_Message)
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userData))
            {
                // User is not logged in, redirect to login or handle as needed
                return RedirectToAction("Login", "UserManagement");
            }
            string url = "https://renthive.online/Admin_API/ViewUserStatus.php";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    int userId = userID;
                    var data = new Dictionary<string, string>
                    {   
                        {"userId", userId.ToString()}
                    };

                    var content = new FormUrlEncodedContent(data);
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData == "error")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "No record found";
                        }
                        else
                        {
                            var userObject = JsonConvert.DeserializeObject<UserDataGetter>(responseData);
                            //admin info
                            ViewBag.Noti_Type = "success";
                            ViewBag.Noti_Message = "retrieving data successfull";
                            ViewBag.Acc_Id = TempData.Acc_id;
                            return View(userObject);
                        }
                    }
                    else
                    {
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = string.Format("There was an error processing your request.");
                return RedirectToAction("ErrorMessage", "ErrorView", new { ErrorMessage = ViewBag.ErrorMessage });
            }
            return View();
        }



        //----------------------------------------start of code-----------------------------------------------
        [HttpPost]
        public async Task<IActionResult> BanChanger(UserDataGetter tempData, string Noti_Type, string Noti_Message)
        {
            string url = "https://renthive.online/Admin_API/UnbanUser.php";

            try
            {
                using (var httpClient = new HttpClient())
                {
                    //userlog
                    int AdminId = tempData.Acc_id;
                    string formattedCurrentDateTime = DateTime.Now.ToString("MMMM dd, yyyy hh:mm:ss") + DateTime.Now.ToString(" tt").ToUpper();
                    string origin = "Deactivated Account Page";
                    string sysResponse = "Success";
                    string action = "Unban user";

                    var data = new Dictionary<string, string>
                    {
                        { "userId", tempData.userId.ToString() },
                        { "banduration", tempData.setTimeBan },
                        //userlog
                        {"adminid" , AdminId.ToString() },
                        {"CurrentDate", formattedCurrentDateTime},
                        {"origin", origin},
                        {"sysResponse", sysResponse },
                        {"action", action }
                    };

                    var content = new FormUrlEncodedContent(data);
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData == "Something went wrong.")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "No user found";
                        }
                        else
                        {
                            // Update successful.
                            return RedirectToAction("BanAccounts", new
                            {
                                // Admin info
                                Acc_id = tempData.Acc_id,
                            });
                        }
                    }
                    else
                    {
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = string.Format("There was an error processing your request.");
                return RedirectToAction("ErrorMessage", "ErrorView", new { ErrorMessage = ViewBag.ErrorMessage });
            }
            return View();
        }
        //----------------------------------------end of code-----------------------------------------------

        //----------------------------------------start of code-----------------------------------------------
        public async Task<IActionResult> DeactPosts(UserDataGetter TempData, string sortBy, string Noti_Type, string Noti_Message)
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userData))
            {
                // User is not logged in, redirect to login or handle as needed
                return RedirectToAction("Login", "UserManagement");
            }
            string url = "https://renthive.online/Admin_API/ViewBannedPosts.php";
            try
            {
                using (var client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        // Parse the response content
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData == "No Users found")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "No user found";
                        }
                        else
                        {
                            //Successfully retrieving data 
                            var newuserObject = JsonConvert.DeserializeObject<List<UserDataGetter>>(responseData);
                            switch (sortBy)
                            {
                                case "Acc_id":
                                    newuserObject = newuserObject.OrderBy(item => item.Acc_id).ToList();
                                    break;
                                case "Post_id":
                                    newuserObject = newuserObject.OrderBy(item => item.Post_id).ToList();
                                    break;
                                case "Post_Title":
                                    newuserObject = newuserObject.OrderBy(item => item.Post_Title).ToList();
                                    break;
                                default:
                                    // Default sorting, if sortBy is not recognized
                                    newuserObject = newuserObject.OrderBy(item => item.Acc_id).ToList();
                                    break;
                            }
                            ViewBag.Acc_Id = TempData.Acc_id;
                            ViewBag.Noti_Type = "success";
                            ViewBag.Noti_Message = "retrieving data successfull";
                            return View(newuserObject);
                        }
                    }
                    else
                    {
                        ViewBag.Acc_Id = TempData.Acc_id;
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Acc_Id = TempData.Acc_id;
                ViewBag.Noti_Type = "error";
                ViewBag.Noti_Message = "Record is empty";
                return View(new List<UserDataGetter>());
            }
            return View();
        }
        public async Task<IActionResult> DeactPostInfo(int userID, int postID, UserDataGetter TempData, string Noti_Type, string Noti_Message)
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userData))
            {
                // User is not logged in, redirect to login or handle as needed
                return RedirectToAction("Login", "UserManagement");
            }
            string url = "https://renthive.online/Admin_API/PostDetailsGetter.php";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var data = new Dictionary<string, string>
                        {
                            {"userId", userID.ToString() },
                            {"postId", postID.ToString() }
                        };

                    var content = new FormUrlEncodedContent(data);
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData == "Something went wrong.")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "No post found";
                        }
                        else
                        {

                            var userObject = JsonConvert.DeserializeObject<UserDataGetter>(responseData);

                            ViewBag.UserId = userID; // the selected user
                            ViewBag.postID = postID;

                            //Admin info
                            ViewBag.Acc_Id = TempData.Acc_id;
                            ViewBag.Noti_Type = "success";
                            ViewBag.Noti_Message = "retrieving data successfull";
                            return View(userObject);
                        }
                    }
                    else
                    {
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = string.Format("There was an error processing your request.");
                return RedirectToAction("ErrorMessage", "ErrorView", new { ErrorMessage = ViewBag.ErrorMessage });
            }
            return View();
        }
        //----------------------------------------start of code-----------------------------------------------
        [HttpPost]
        public async Task<IActionResult> ptBanChanger(int userID, int postID, UserDataGetter TempData)
        {
            string url = "https://renthive.online/Admin_API/StatusChecker.php";

            try
            {
                //userlog
                string formattedCurrentDateTime = DateTime.Now.ToString("MMMM dd, yyyy hh:mm:ss") + DateTime.Now.ToString(" tt").ToUpper();
                string origin = "Deactivated Posts Page";
                string sysResponse = "Success";

                using (var httpClient = new HttpClient())
                {
                    var data = new Dictionary<string, string>
                {
                    { "userId", userID.ToString()},
                    { "postId", postID.ToString()},
                    { "numHolder", TempData.NumHolder.ToString()},
                    //userlog
                    {"adminid" , TempData.Acc_id.ToString()},
                    {"CurrentDate", formattedCurrentDateTime},
                    {"origin", origin},
                    {"sysResponse", sysResponse }
                };

                    var content = new FormUrlEncodedContent(data);
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData == "Something went wrong.")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "No user found";
                        }
                        else
                        {
                            // Update successful.
                            return RedirectToAction("DeactPosts", new
                            {
                                // Admin info
                                Acc_id = TempData.Acc_id,
                            });
                        }
                    }
                    else
                    {
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = string.Format("There was an error processing your request.");
                return RedirectToAction("ErrorMessage", "ErrorView", new { ErrorMessage = ViewBag.ErrorMessage });
            }

            return View();
        }
        //----------------------------------------end of code-----------------------------------------------
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
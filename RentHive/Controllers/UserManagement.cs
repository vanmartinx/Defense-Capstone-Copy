using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RentHive.Models;


namespace RentHive.Controllers
{
    public class UserManagement : Controller
    {
        //------------------------------------------------------------------------
        [HttpGet]
        public IActionResult SignUp(int TempuserId)
        {
            ViewBag.Acc_id = TempuserId;
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> SignUp(int TempuserId, UserDataGetter TempData, string Noti_Type, string Noti_Message)
        {
            // BanAccountslace with your PHP API URL hosted on 000webhost
            string url = "https://renthive.online/Admin_API/Signup.php";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    string Acc_UserType = TempData.Acc_UserType;
                    string Firstname = TempData.Acc_FirstName;
                    string Lastname = TempData.Acc_LastName;
                    string Middlename = TempData.Acc_MiddleName;
                    string DisplayName = TempData.Acc_DisplayName;
                    string Birthdate = TempData.Acc_Birthdate;
                    string PhoneNum = TempData.Acc_PhoneNum;
                    string Address = TempData.Acc_Address;
                    string Username = TempData.Acc_Email;
                    string Password = TempData.Acc_Password;

                    // Create a dictionary with username and password
                    var data = new Dictionary<string, string>
                    {
                        {"userType",  Acc_UserType},
                        {"firstname", Firstname},
                        {"lastname", Lastname},
                        {"middlename", Middlename},
                        {"displayname", DisplayName},
                        {"birthdate", Birthdate},
                        {"phonenum", PhoneNum},
                        {"address", Address},
                        {"username", Username},
                        {"password", Password},
                    };

                    // Serialize the credentials as JSON and send them in the request body.
                    var content = new FormUrlEncodedContent(data);

                    // Make a POST request to the PHP API
                    var response = await httpClient.PostAsync(url, content);


                    // Ensure a successful response
                    if (response.IsSuccessStatusCode)
                    {
                        // Parse the response content
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData.Contains("Taken"))
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "The email has already been taken.";
                            ViewBag.Acc_Id = TempData.Acc_id;
                            return View();
                        }
                        else if (responseData.Contains("failed"))
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "User Registration failed.";
                            ViewBag.Acc_Id = TempData.Acc_id;
                        }
                        else
                        {
                            ViewBag.Noti_Type = "success";
                            ViewBag.Noti_Message = "User Registration Successful.";
                            ViewBag.Acc_Id = TempData.Acc_id;
                        }

                    }
                    else
                    {
                        ViewBag.Noti_Type = "info";
                        ViewBag.Noti_Message = "API request failed.";
                        ViewBag.Acc_Id = TempData.Acc_id;
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = string.Format("There was an error processing your request.");
                return RedirectToAction("ErrorMessage", "ErrorView", new { ErrorMessage = ViewBag.ErrorMessage });
            }
            return View(new { Noti_Type = ViewBag.Noti_Type, Noti_Message = ViewBag.Noti_Message, Acc_id = TempData.Acc_id });
        }

        //--------------------------------------------------------------------------

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Login(UserDataGetter TempData)
        {
            // Replace with your PHP API URL hosted on 000webhost
            string url = "https://renthive.online/Admin_API/Login.php";

            try
            {
                using (var httpClient = new HttpClient())
                {
                    string Username = TempData.Acc_Email;
                    string Password = TempData.Acc_Password;

                    // Create a dictionary with username and password
                    var data = new Dictionary<string, string>
                    {
                        {"username", Username},
                        {"password", Password}
                    };

                    // Serialize the credentials as JSON and send them in the request body.
                    var content = new FormUrlEncodedContent(data);

                    // Make a POST request to the PHP API
                    var response = await httpClient.PostAsync(url, content);

                    // Ensure a successful response
                    if (response.IsSuccessStatusCode)
                    {
                        // Parse the response content
                        var responseData = await response.Content.ReadAsStringAsync();

                        // Deserialize the JSON response into a dynamic object
                        var userObject = JsonConvert.DeserializeObject<UserDataGetter>(responseData);

                        // Check if login was successful
                        if (userObject.response == "Failed")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "Please enter correct Admin Credentials.";
                        }
                        else if (userObject.response == "BanOrDeact")
                        {
                            ViewBag.Noti_Type = "info";
                            ViewBag.Noti_Message = "Login Failed. Please make sure the account is not currently banned or deactivated.";
                        }
                        else
                        {
                            ViewBag.Noti_Type = "success";
                            ViewBag.Noti_Message = "Welcome, " + userObject.Acc_DisplayName;
                            // Store user information in a session variable
                            HttpContext.Session.SetString("UserData", JsonConvert.SerializeObject(userObject));
                            return RedirectToAction("Index", "Home", new { Acc_id = userObject.Acc_id, Noti_Type = ViewBag.Noti_Type, Noti_Message = ViewBag.Noti_Message });
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
        //---------------------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Profile(UserDataGetter TempData,string Noti_Type, string Noti_Message)
        {
            var userData = HttpContext.Session.GetString("UserData");

            if (string.IsNullOrEmpty(userData))
            {
                // User is not logged in, redirect to login or handle as needed
                return RedirectToAction("Login", "UserManagement");
            }

            ViewBag.Noti_Type = Noti_Type;
            ViewBag.Noti_Message = Noti_Message;

            string url = "https://renthive.online/Admin_API/UpdateGet.php";

            try
            {
                using (var httpClient = new HttpClient())
                {
                    int userId = TempData.Acc_id;
                    var data = new Dictionary<string, string> {
                        { "userId", userId.ToString() }
                    };
                    var content = new FormUrlEncodedContent(data);

                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData.Contains("No record found"))
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "No record found";
                            ViewBag.Acc_id = TempData.Acc_id;
                        }
                        else
                        {
                            var userObject = JsonConvert.DeserializeObject<UserDataGetter>(responseData);
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
            return RedirectToAction("Profile", new { Acc_id = TempData.Acc_id, Noti_Type = ViewBag.Noti_Type, Noti_Message = ViewBag.Noti_Message });
        }


        [HttpPost]
        public async Task<IActionResult> Profile(UserDataGetter TempData, [FromForm] IFormFile file, string Noti_Type, string Noti_Message)
        {

            if (TempData.NumHolder == 1)
            {
                if (file != null && file.Length > 0)
                {
                    // Assuming you have a method to convert the file to base64
                    string base64Image = ConvertFileToBase64(file);

                    //userlog
                    string formattedCurrentDateTime = DateTime.Now.ToString("MMMM dd, yyyy hh:mm:ss") + DateTime.Now.ToString(" tt").ToUpper();
                    string origin = "Profile Page";
                    string sysResponse = "Success";
                    string action = "Update Photo";

                    // You can then do something with the base64 image, like save it to the database or use it in your application

                    // For demonstration purposes, let's just return a view with the base64 image
                    /*ViewBag.Base64Image = base64Image;*/
                    string url = "https://renthive.online/Admin_API/ImageUpload.php";
                    try
                    {
                        using (var httpClient = new HttpClient())
                        {
                            var data = new Dictionary<string, string>
                            {
                                { "userId", TempData.Acc_id.ToString() },
                                { "image", base64Image },
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

                                if (responseData.Contains("Update Failed"))
                                {
                                    ViewBag.Noti_Type = "error";
                                    ViewBag.Noti_Message = "Update Failed";
                                }
                                else
                                {
                                    ViewBag.Noti_Type = "success";
                                    ViewBag.Noti_Message = "Update Successful";
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
                }
                return RedirectToAction("Profile", new { Acc_id = TempData.Acc_id, Noti_Type = ViewBag.Noti_Type, Noti_Message = ViewBag.Noti_Message });
            }

            if (TempData.NumHolder == 2)
            {
                string url = "https://renthive.online/Admin_API/UpdatePost.php";
                try
                {

                    string formattedCurrentDateTime = DateTime.Now.ToString("MMMM dd, yyyy hh:mm:ss") + DateTime.Now.ToString(" tt").ToUpper();
                    string origin = "Profile Page";
                    string sysResponse = "Success";
                    string action = "Update Credentials";

                    using (var httpClient = new HttpClient())
                    {
                        var data = new Dictionary<string, string>
                        {
                            { "userId", TempData.Acc_id.ToString() },
                            { "firstname", TempData.Acc_FirstName },
                            { "lastname", TempData.Acc_LastName },
                            { "middlename", TempData.Acc_MiddleName },
                            { "displayname", TempData.Acc_DisplayName },
                            { "birthdate", TempData.Acc_Birthdate },
                            { "phonenum", TempData.Acc_PhoneNum },
                            { "address", TempData.Acc_Address },
                            { "email", TempData.Acc_Email },
                            { "password", TempData.Acc_Password },

                                //user log
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

                            if (responseData.Contains("Update Failed"))
                            {
                                ViewBag.Noti_Type = "error";
                                ViewBag.Noti_Message = "Update Failed";
                            }
                            else
                            {
                                ViewBag.Noti_Type = "success";
                                ViewBag.Noti_Message = "Update Successful";
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
            }
            return RedirectToAction("Profile", new { Acc_id = TempData.Acc_id, Noti_Type = ViewBag.Noti_Type, Noti_Message = ViewBag.Noti_Message });
        }
        //----------------------------------------------------------------------------

        //----------------------------------------------------------------------------
        private string ConvertFileToBase64(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                byte[] bytes = memoryStream.ToArray();
                return Convert.ToBase64String(bytes);
            }
        }
        //----------------------------------------------------------------------------

        //----------------------------------------------------------------------------

        [HttpGet]
        public IActionResult DeleteAccount()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> DeleteAccount(int TempuserId, string confirmation)
        {
            if (confirmation == "yes")
            {
                string url = "https://renthive.online/Admin_API/DeleteAccount.php";
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        int _TempuserId = TempuserId;

                        // Create a dictionary with username and password
                        var data = new Dictionary<string, string>
                    {
                        {"userId", _TempuserId.ToString()}
                    };

                        // Serialize the credentials as JSON and send them in the request body.
                        var content = new FormUrlEncodedContent(data);

                        // Make a POST request to the PHP API
                        var response = await httpClient.PostAsync(url, content);

                        // Ensure a successful response
                        if (response.IsSuccessStatusCode)
                        {
                            // Parse the response content
                            var responseData = await response.Content.ReadAsStringAsync();

                            if (responseData == "User deleted successfully.")
                            {
                                ViewBag.Noti_Type = "success";
                                ViewBag.Noti_Message = "User delete successful";
                            }
                            else
                            {
                                ViewBag.Noti_Type = "error";
                                ViewBag.Noti_Message = "User delete failed";
                                return RedirectToAction("Login", "UserManagement");
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
            }
            return RedirectToAction("Login", new { Noti_Type = ViewBag.Noti_Type, Noti_Message = ViewBag.Noti_Message });
        }
        public IActionResult Logout()
        {
            // Remove the "UserData" from the session
            HttpContext.Session.Remove("UserData");

            // Redirect to the login or home page after logout
            return RedirectToAction("Login", "UserManagement"); // Replace "Login" with your actual login page
        }

        public async Task<IActionResult> AdminLog(UserDataGetter TempData, string sortBy, string Noti_Type, string Noti_Message)
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userData))
            {
                // User is not logged in, redirect to login or handle as needed
                return RedirectToAction("Login", "UserManagement");
            }
            string url = "https://renthive.online/Admin_API/ViewAdminLog.php";
            try
            {
                string formattedCurrentDate = DateTime.Now.ToString("MMMM dd, yyyy");
                string formattedTime = DateTime.Now.ToString("hh:mm:ss") + DateTime.Now.ToString(" tt").ToUpper();

                using (var httpClient = new HttpClient())
                {
                    var data = new Dictionary<string, string>
                    {
                        {"accid", TempData.Acc_id.ToString()}
                    };

                    var content = new FormUrlEncodedContent(data);
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Parse the response content
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (responseData.Contains("No Users found"))
                        {
                            ViewBag.Noti_Type = "info";
                            ViewBag.Noti_Message = "Record empty.";
                        }
                        else
                        {

                            //Successfully retrieving data 
                            var newuserObject = JsonConvert.DeserializeObject<List<UserDataGetter>>(responseData);
                            switch (sortBy)
                            {
                                case "ul_id":
                                    newuserObject = newuserObject.OrderBy(item => item.ul_id).ToList();
                                    break;
                                case "ul_Origin":
                                    newuserObject = newuserObject.OrderByDescending(item => item.ul_Origin).ToList();
                                    break;
                                case "ul_Timestamp":
                                    newuserObject = newuserObject.OrderBy(item => item.ul_Timestamp).ToList();
                                    break;
                                case "ul_Action":
                                    newuserObject = newuserObject.OrderBy(item => item.ul_Action).ToList();
                                    break;
                                case "ul_SysResponse":
                                    newuserObject = newuserObject.OrderBy(item => item.ul_SysResponse).ToList();
                                    break;
                                default:
                                    // Default sorting, if sortBy is not recognized
                                    newuserObject = newuserObject.OrderBy(item => item.Acc_id).ToList();
                                    break;
                            }
                            ViewBag.Acc_Id = TempData.Acc_id;
                            //user
                            ViewBag.CurrentDate = formattedCurrentDate;
                            ViewBag.CurrentTime = formattedTime;
                            ViewBag.Acc_FirstName = TempData.Acc_FirstName;
                            ViewBag.Acc_MiddleName = TempData.Acc_MiddleName;
                            ViewBag.Acc_LastName = TempData.Acc_LastName;
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
            return View(new List<UserDataGetter>());
        }

        public async Task<IActionResult> deletedAcc(UserDataGetter TempData, string sortBy, string Noti_Type, string Noti_Message)
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userData))
            {
                // User is not logged in, redirect to login or handle as needed
                return RedirectToAction("Login", "UserManagement");
            }
            string url = "https://renthive.online/Admin_API/ViewDeletedAccounts.php";
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
                            ViewBag.ErrorMessage = string.Format("No users found.");
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
                            return View(newuserObject);
                        }
                    }
                    else
                    {
                        ViewBag.ErrorMessage = string.Format("API request failed");
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
            return View(new { Noti_Type = ViewBag.Noti_Type, Noti_Message = ViewBag.Noti_Message, Acc_id = TempData.Acc_id });
        }

        public async Task<IActionResult> deleteAcc_Details(int userID, UserDataGetter TempData, string Noti_Type, string Noti_Message)
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userData))
            {
                // User is not logged in, redirect to login or handle as needed
                return RedirectToAction("Login", "UserManagement");
            }

            string url = "https://renthive.online/Admin_API/deletedAcc_Details.php";
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

                        if (responseData == "Something went wrong")
                        {
                            ViewBag.Noti_Type = "error";
                            ViewBag.Noti_Message = "no record found.";
                        }
                        else
                        {
                            var userObject = JsonConvert.DeserializeObject<UserDataGetter>(responseData);
                            // Admin info
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
            return View(new { Noti_Type = ViewBag.Noti_Type, Noti_Message = ViewBag.Noti_Message, Acc_id = TempData.Acc_id });
        }
        public async Task<IActionResult> userActivate(int userID, int status, UserDataGetter TempData, string Noti_Type, string Noti_Message)
        {
            string url = "https://renthive.online/Admin_API/StatusTO_1.php";

            try
            {
                using (var httpClient = new HttpClient())
                {
                    int AdminId = TempData.Acc_id;
                    string formattedCurrentDateTime = DateTime.Now.ToString("MMMM dd, yyyy hh:mm:ss") + DateTime.Now.ToString(" tt").ToUpper();
                    string origin = "Deleted Accounts Page";
                    string sysResponse = "Success";
                    string action = "Activate account";

                    var data = new Dictionary<string, string>
                    {
                        { "userId", userID.ToString() },
                        //userlog
                        {"adminId" , AdminId.ToString() },
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
                            
                            // Update successful. Redirect to the user status page.
                            return RedirectToAction("deletedAcc", new
                            {
                                Acc_id = TempData.Acc_id
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

            // Return to the view in case of an error or exception
            return View(new { Noti_Type = ViewBag.Noti_Type, Noti_Message = ViewBag.Noti_Message, Acc_id = TempData.Acc_id });
        }

    }
}
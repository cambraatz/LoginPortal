using DeliveryManager.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

// token initialization...
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverChecklistController : ControllerBase
    {
        private IConfiguration _configuration;
        private readonly string connString;
        private readonly IWebHostEnvironment _env;

        public DriverChecklistController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
            connString = _configuration.GetConnectionString("DriverChecklistTestCon");
            //connString = _configuration.GetConnectionString("DriverChecklistDBCon");
        }

        [HttpGet]
        [Route("GetUndelivered")]
        [Authorize]
        public JsonResult GetUndelivered(string POWERUNIT, string MFSTDATE)
        {
            string query = "select * from dbo.DMFSTDAT where POWERUNIT=@POWERUNIT and MFSTDATE=@MFSTDATE and STATUS=0 order by STOP";
            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;
            
            try
            {
                using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@POWERUNIT", POWERUNIT);
                        myCommand.Parameters.AddWithValue("@MFSTDATE", MFSTDATE);
                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();
                        myCon.Close();
                    }
                }
                if (table.Rows.Count > 0)
                {
                    return new JsonResult(new { success = true, table = table });
                }
                else
                {
                    return new JsonResult(new { success = true, table = table });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = "Error: " + ex.Message });
            }
        }
        
        [HttpGet]
        [Route("GetDelivered")]
        [Authorize]
        public JsonResult GetDelivered(string POWERUNIT, string MFSTDATE)
        {
            string query = "select * from dbo.DMFSTDAT where POWERUNIT=@POWERUNIT and MFSTDATE=@MFSTDATE and STATUS=1 order by STOP";
            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;
            
            try
            {
                using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@POWERUNIT", POWERUNIT);
                        myCommand.Parameters.AddWithValue("@MFSTDATE", MFSTDATE);
                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();
                        myCon.Close();
                    }
                }
                if (table.Rows.Count > 0)
                {
                    return new JsonResult(new { success = true, table = table });
                }
                else
                {
                    return new JsonResult(new { success = true, table = table });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = "Error: " + ex.Message });
            }
        }

        [HttpPut]
        [Route("UpdateManifest")]
        [Authorize]
        public async Task<JsonResult> UpdateManifest([FromForm] DeliveryForm data)
        {
            try
            {
                // define path where the image is to be saved...
                string folderPath = Path.Combine("wwwroot", "uploads");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string location_path = null;
                string loc_name = null;

                string sign_path = null;
                string sign_name = null;

                // save image locally when photo was uploaded...
                if (data.DLVDIMGFILELOCN != null)
                {
                    try
                    {
                        // generate a unique file name...
                        loc_name = Guid.NewGuid().ToString().Substring(16) + Path.GetExtension(path: data.DLVDIMGFILELOCN.FileName);
                        location_path = Path.Combine(folderPath, loc_name);

                        // save the file to the server...
                        using (var fileStream = new FileStream(location_path, FileMode.Create))
                        {
                            await data.DLVDIMGFILELOCN.CopyToAsync(fileStream);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving file: {ex.Message}");
                    }
                }
                // omit saving, point back to image already on file...
                else
                {
                    loc_name = data.location_string;
                }

                // save image locally when photo was uploaded...
                if (data.DLVDIMGFILESIGN != null)
                {
                    try
                    {
                        // generate a unique file name...
                        sign_name = Guid.NewGuid().ToString().Substring(16) + Path.GetExtension(path: data.DLVDIMGFILESIGN.FileName);
                        sign_path = Path.Combine(folderPath, sign_name);

                        // save the file to the server...
                        using (var fileStream = new FileStream(sign_path, FileMode.Create))
                        {
                            await data.DLVDIMGFILESIGN.CopyToAsync(fileStream);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving file: {ex.Message}");
                    }
                }
                // omit saving, point back to image already on file...
                else
                {
                    sign_name = data.signature_string;
                }

                string query = "update dbo.DMFSTDAT set MFSTKEY = @MFSTKEY,STATUS = @STATUS,LASTUPDATE = @LASTUPDATE,MFSTNUMBER = @MFSTNUMBER," +
                "POWERUNIT = @POWERUNIT,STOP = @STOP,MFSTDATE = @MFSTDATE,PRONUMBER = @PRONUMBER,PRODATE = @PRODATE,SHIPNAME = @SHIPNAME," +
                "CONSNAME = @CONSNAME,CONSADD1 = @CONSADD1,CONSADD2 = @CONSADD2,CONSCITY = @CONSCITY,CONSSTATE = @CONSSTATE,CONSZIP = @CONSZIP," +
                "TTLPCS = @TTLPCS,TTLYDS = @TTLYDS,TTLWGT = @TTLWGT,DLVDDATE = @DLVDDATE,DLVDTIME = @DLVDTIME,DLVDPCS = @DLVDPCS,DLVDSIGN = @DLVDSIGN," +
                "DLVDNOTE = @DLVDNOTE,DLVDIMGFILELOCN = @DLVDIMGFILELOCN,DLVDIMGFILESIGN = @DLVDIMGFILESIGN where MFSTKEY=@MFSTKEY";

                DataTable table = new DataTable();
                string sqlDatasource = connString;
                SqlDataReader myReader;

                await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@MFSTKEY", data.MFSTKEY);
                        myCommand.Parameters.AddWithValue("@STATUS", data.STATUS);
                        myCommand.Parameters.AddWithValue("@LASTUPDATE", data.LASTUPDATE);
                        myCommand.Parameters.AddWithValue("@MFSTNUMBER", data.MFSTNUMBER);
                        myCommand.Parameters.AddWithValue("@POWERUNIT", data.POWERUNIT);
                        myCommand.Parameters.AddWithValue("@STOP", data.STOP);
                        myCommand.Parameters.AddWithValue("@MFSTDATE", data.MFSTDATE);
                        myCommand.Parameters.AddWithValue("@PRONUMBER", data.PRONUMBER);
                        myCommand.Parameters.AddWithValue("@PRODATE", data.PRODATE);
                        myCommand.Parameters.AddWithValue("@SHIPNAME", data.SHIPNAME);
                        myCommand.Parameters.AddWithValue("@CONSNAME", data.CONSNAME);
                        myCommand.Parameters.AddWithValue("@CONSADD1", data.CONSADD1);
                        myCommand.Parameters.AddWithValue("@CONSADD2", data.CONSADD2 == null || data.CONSADD2 == "null" ? DBNull.Value : data.CONSADD2);
                        myCommand.Parameters.AddWithValue("@CONSCITY", data.CONSCITY);
                        myCommand.Parameters.AddWithValue("@CONSSTATE", data.CONSSTATE);
                        myCommand.Parameters.AddWithValue("@CONSZIP", data.CONSZIP);
                        myCommand.Parameters.AddWithValue("@TTLPCS", data.TTLPCS);
                        myCommand.Parameters.AddWithValue("@TTLYDS", data.TTLYDS);
                        myCommand.Parameters.AddWithValue("@TTLWGT", data.TTLWGT);
                        myCommand.Parameters.AddWithValue("@DLVDDATE", data.DLVDDATE == null || data.DLVDDATE == "null" ? DBNull.Value : data.DLVDDATE);
                        myCommand.Parameters.AddWithValue("@DLVDTIME", data.DLVDTIME == null || data.DLVDTIME == "null" ? DBNull.Value : data.DLVDTIME);
                        myCommand.Parameters.AddWithValue("@DLVDPCS", data.DLVDPCS == null || data.DLVDPCS == -1 ? DBNull.Value : data.DLVDPCS);
                        myCommand.Parameters.AddWithValue("@DLVDSIGN", data.DLVDSIGN == null || data.DLVDSIGN == "null" ? DBNull.Value : data.DLVDSIGN);
                        myCommand.Parameters.AddWithValue("@DLVDNOTE", data.DLVDNOTE == null || data.DLVDNOTE == "null" ? DBNull.Value : data.DLVDNOTE);
                        myCommand.Parameters.AddWithValue("@DLVDIMGFILELOCN", loc_name == null || loc_name == "null" ? DBNull.Value : loc_name);
                        myCommand.Parameters.AddWithValue("@DLVDIMGFILESIGN", sign_name == null || sign_name == "null" ? DBNull.Value : sign_name);

                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();
                        myCon.Close();
                    }
                    return new JsonResult(new { success = true, table = table });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = "Error updating delivery: " + ex.Message });
            }
        }

        [HttpGet]
        [Route("GetImage")]
        [Authorize]
        public IActionResult GetImage(string IMAGE)
        {
            var folderPath = Path.Combine(_env.WebRootPath, "uploads");
            var filePath = Path.Combine(folderPath, IMAGE);

            System.Diagnostics.Debug.WriteLine($"File path to {filePath}");

            if (System.IO.File.Exists(filePath))
            {
                var image = System.IO.File.OpenRead(filePath);
                return File(image, "image/jpeg");
            }
            else
            {
                return NotFound("Image not found");
            }
        }

        /*
         * Inactive Requests; retained for maintenance/flexibility, but inactive in current iteration...
         */

        [HttpGet]
        [Route("GetDriverLog")]
        [Authorize]
        public JsonResult GetDriverLog(string POWERUNIT)
        {
            string query = "select * from dbo.DMFSTDAT where POWERUNIT=@POWERUNIT order by STOP";
            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;
            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                myCon.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@POWERUNIT", POWERUNIT);
                    myReader = myCommand.ExecuteReader();
                    table.Load(myReader);
                    myReader.Close();
                    myCon.Close();
                }
            }

            return new JsonResult(table);
        }

        [HttpPost]
        [Route("AddManifest")]
        [Authorize]
        public JsonResult AddManifest(string MFSTKEY, string STATUS, string LASTUPDATE, string MFSTNUMBER,
            string POWERUNIT, int STOP, string MFSTDATE, string PRONUMBER, string PRODATE,
            string SHIPNAME, string CONSNAME, string CONSADD1, string CONSADD2, string CONSCITY,
            string CONSSTATE, string CONSZIP, int TTLPCS, int TTLYDS, int TTLWGT, string DLVDDATE,
            string DLVDTIME, int DLVDPCS, string DLVDSIGN, string DLVDNOTE, string DLVDIMGFILELOCN, string DLVDIMGFILESIGN)
        {
            string query = "insert into dbo.DMFSTDAT(MFSTKEY,STATUS,LASTUPDATE,MFSTNUMBER,POWERUNIT,STOP,MFSTDATE,PRONUMBER,PRODATE,SHIPNAME,CONSNAME,CONSADD1," +
                "CONSADD2,CONSCITY,CONSSTATE,CONSZIP,TTLPCS,TTLYDS,TTLWGT,DLVDDATE,DLVDTIME,DLVDPCS,DLVDSIGN,DLVDNOTE,DLVDIMGFILELOCN,DLVDIMGFILESIGN) values(@MFSTKEY," +
                "@STATUS,@LASTUPDATE,@MFSTNUMBER,@POWERUNIT,@STOP,@MFSTDATE,@PRONUMBER,@PRODATE,@SHIPNAME,@CONSNAME,@CONSADD1,@CONSADD2,@CONSCITY,@CONSSTATE," +
                "@CONSZIP,@TTLPCS,@TTLYDS,@TTLWGT,@DLVDDATE,@DLVDTIME,@DLVDPCS,@DLVDSIGN,@DLVDNOTE,@DLVDIMGFILELOCN,@DLVDIMGFILESIGN)";

            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;
            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                myCon.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@MFSTKEY", MFSTKEY);
                    myCommand.Parameters.AddWithValue("@STATUS", STATUS);
                    myCommand.Parameters.AddWithValue("@LASTUPDATE", LASTUPDATE);
                    myCommand.Parameters.AddWithValue("@MFSTNUMBER", MFSTNUMBER);
                    myCommand.Parameters.AddWithValue("@POWERUNIT", POWERUNIT);
                    myCommand.Parameters.AddWithValue("@STOP", STOP);
                    myCommand.Parameters.AddWithValue("@MFSTDATE", MFSTDATE);
                    myCommand.Parameters.AddWithValue("@PRONUMBER", PRONUMBER);
                    myCommand.Parameters.AddWithValue("@PRODATE", PRODATE);
                    myCommand.Parameters.AddWithValue("@SHIPNAME", SHIPNAME);
                    myCommand.Parameters.AddWithValue("@CONSNAME", CONSNAME);
                    myCommand.Parameters.AddWithValue("@CONSADD1", CONSADD1);
                    myCommand.Parameters.AddWithValue("@CONSADD2", CONSADD2);
                    myCommand.Parameters.AddWithValue("@CONSCITY", CONSCITY);
                    myCommand.Parameters.AddWithValue("@CONSSTATE", CONSSTATE);
                    myCommand.Parameters.AddWithValue("@CONSZIP", CONSZIP);
                    myCommand.Parameters.AddWithValue("@TTLPCS", TTLPCS);
                    myCommand.Parameters.AddWithValue("@TTLYDS", TTLYDS);
                    myCommand.Parameters.AddWithValue("@TTLWGT", TTLWGT);
                    myCommand.Parameters.AddWithValue("@DLVDDATE", DLVDDATE);
                    myCommand.Parameters.AddWithValue("@DLVDTIME", DLVDTIME);
                    myCommand.Parameters.AddWithValue("@DLVDPCS", DLVDPCS);
                    myCommand.Parameters.AddWithValue("@DLVDSIGN", DLVDSIGN);
                    myCommand.Parameters.AddWithValue("@DLVDNOTE", DLVDNOTE);
                    myCommand.Parameters.AddWithValue("@DLVDIMGFILELOCN", DLVDIMGFILELOCN);
                    myCommand.Parameters.AddWithValue("@DLVDIMGFILESIGN", DLVDIMGFILESIGN);

                    myReader = myCommand.ExecuteReader();
                    table.Load(myReader);
                    myReader.Close();
                    myCon.Close();
                }
            }

            return new JsonResult("Added Successfully");
        }

        [HttpDelete]
        [Route("DeleteManifest")]
        [Authorize]
        public JsonResult DeleteManifest(string MFSTKEY)
        {
            string query = "delete from dbo.DMFSTDAT where MFSTKEY=@MFSTKEY";
            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;
            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                myCon.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@MFSTKEY", MFSTKEY);
                    myReader = myCommand.ExecuteReader();
                    table.Load(myReader);
                    myReader.Close();
                    myCon.Close();
                }
            }

            return new JsonResult("Deleted Successfully");
        }
    }
}
